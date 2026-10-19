using ServeArc.Archive;
using ServeArc.Utils;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace ServeArc;

public class ArchiveProcessor(ILogger<ArchiveProcessor> logger) : IDisposable
{
    private record Archive(IArchiveHandler Handler, string? Prefix, SemaphoreSlim ThreadLock);

    private readonly List<Archive> handlers = [];

    private DateTimeOffset dirLastMod = DateTimeOffset.MinValue;

    public IReadOnlyCollection<IArchiveHandler> Handlers => handlers
        .Select(m => m.Handler)
        .ToArray()
        .AsReadOnly();

    public bool DisableDirectoryBrowsing { get; set; }

    public void UpdateLastModified(DateTimeOffset arcLastMod)
    {
        dirLastMod = dirLastMod < arcLastMod ? arcLastMod : dirLastMod;
    }

    public void AddArchive(IArchiveHandler handler, string? root = null)
    {
        handlers.Add(new(handler, root, new(1)));
        logger.LogInformation("Added {Type} to archive processing list", handler.GetType());
    }

    public bool RemoveArchive(IArchiveHandler handler)
    {
        var removed = handlers.RemoveAll(m => m.Handler == handler);
        if (removed > 0)
        {
            logger.LogInformation("Removed {Type} from archive processing list", handler.GetType());
        }
        else
        {
            logger.LogWarning("Tried to remove {Type} from archive processing list but the given instance was not found", handler.GetType());
        }
        return removed > 0;
    }

    public async Task ProcessRequest(HttpContext ctx, Func<Task> next)
    {
        var url = ctx.Request.Path.Value ?? "/";
        var isDir = url.EndsWith('/');
        bool hasLmod = DateTimeOffset.TryParseExact(ctx.Request.Headers.IfModifiedSince.FirstOrDefault(), "R", CultureInfo.InvariantCulture, DateTimeStyles.None, out var lmod);
        DirectoryEntry[]? dirEntries = null;
        //For directory last modified handling, do before enumerating entries
        if (isDir && dirLastMod != DateTimeOffset.MinValue)
        {
            ctx.Response.Headers.LastModified = dirLastMod.ToString("R");
            if (hasLmod && lmod <= dirLastMod)
            {
                logger.LogInformation("Using cache for directory listings");
                ctx.Response.StatusCode = 304;
                ctx.Response.ContentType = "text/html";
                await ctx.Response.StartAsync();
                await ctx.Response.CompleteAsync();
                return;
            }
        }
        foreach (var (index, archive) in handlers.Index().Reverse())
        {
            string fullTypeName = archive.Handler.GetType().FullName ?? archive.Handler.GetType().Name;
            var path = string.IsNullOrEmpty(archive.Prefix)
                ? url :
                archive.Prefix + url[1..];
            try
            {
                if (isDir)
                {
                    //Directory browsing is done on a cumulative basis
                    if (!DisableDirectoryBrowsing)
                    {
                        var result = StripRoot(archive.Handler.Browse(path), archive.Prefix);
                        if (result != null)
                        {
                            logger.LogInformation("Request '{Path}' handled by '{Handler}' at position {Index}", path, fullTypeName, index + 1);
                            dirEntries = [.. result.Concat(dirEntries ?? []).DistinctBy(m => m.FullPath)];
                        }
                        else
                        {
                            logger.LogInformation("Request '{Path}' skipped by '{Handler}' at position {Index}", path, fullTypeName, index + 1);
                        }
                    }
                    else
                    {
                        logger.LogInformation("Request '{Path}' skipped because directory browsing is disabled", path);
                    }
                }
                else
                {
                    var ts = archive.Handler.IsFileThreadSafe;
                    if (!ts)
                    {
                        await archive.ThreadLock.WaitAsync();
                    }
                    try
                    {
                        var f = archive.Handler.GetFile(path);
                        if (f != null)
                        {
                            logger.LogInformation("Request '{Path}' handled by '{Handler}' at position {Index}", path, fullTypeName, index + 1);

                            await SendFile(ctx, f, hasLmod && f.LastModified.HasValue && lmod <= f.LastModified);
                            return;
                        }
                        else
                        {
                            logger.LogInformation("Request '{Path}' skipped by '{Handler}' at position {Index}", path, fullTypeName, index + 1);
                        }
                    }
                    finally
                    {
                        if (!ts)
                        {
                            archive.ThreadLock.Release();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Archive handler '{Handler}' at position {Index} failed to handle the request", fullTypeName, index + 1);
            }
        }
        if (isDir && dirEntries != null)
        {
            await SendDirectory(ctx, dirEntries);
        }
        else
        {
            //No handler matched
            logger.LogInformation("No handler handled the request at '{Path}'. Deferring to default handler", ctx.Request.Path);
            await next();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        lock (handlers)
        {
            handlers.ForEach(h => h.Handler.Dispose());
            handlers.Clear();
        }
    }

    [return: NotNullIfNotNull(nameof(directoryEntries))]
    private static DirectoryEntry[]? StripRoot(DirectoryEntry[]? directoryEntries, string? prefix)
    {
        if (directoryEntries == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(prefix) || prefix == "/")
        {
            return directoryEntries;
        }

        return [.. directoryEntries.Select(m => m with
        {
            FullPath = m.FullPath[(prefix.Length - 1)..]
        })];
    }

    private async Task SendFile(HttpContext ctx, FileResult file, bool cacheOnly)
    {
        ctx.Response.RegisterForDispose(file.File);

        var mediaType = file.MimeType ?? MimeType.GetMimeType(ctx.Request.Path);
        //Set headers we want regardless of the response type
        if (file.LastModified.HasValue)
        {
            ctx.Response.Headers.AcceptRanges = "bytes";
            ctx.Response.Headers.LastModified = file.LastModified.Value.ToString("R");
        }

        //Minimal reply on cache hit
        if (cacheOnly)
        {
            ctx.Response.StatusCode = 304;
            ctx.Response.ContentType = mediaType;
        }
        else
        {
            //If no range header is present, return the full file data
            if (!RangeHeaderValue.TryParse(ctx.Request.Headers.Range.FirstOrDefault(), out var range))
            {
                if (file.Size.HasValue)
                {
                    ctx.Response.ContentLength = file.Size.Value;
                }
                ctx.Response.ContentType = mediaType;
                await ctx.Response.StartAsync();
                await file.File.CopyToAsync(ctx.Response.Body);
            }
            else
            {
                if (!MediaTypeHeaderValue.TryParse(mediaType, out var mimeType))
                {
                    mimeType = MediaTypeHeaderValue.Parse("application/octet-stream");
                }
                //Process range request
                ByteRangeStreamContent content;
                try
                {
                    content = new ByteRangeStreamContent(file.File, range, mimeType);
                }
                catch (InvalidByteRangeException ex)
                {
                    logger.LogInformation("Range not satisfiable. {Details}", ex.Message);
                    var msg = Encoding.UTF8.GetBytes(Html.Body("416 - Range not satisfiable", $"<h1>416 - Range not satisfiable</h1><p>{Html.Encode(ex.Message)}</p>"));
                    ctx.Response.StatusCode = 416;
                    ctx.Response.ContentType = "text/plain";
                    ctx.Response.ContentLength = msg.Length;
                    ctx.Response.Headers.ContentRange = new Microsoft.Extensions.Primitives.StringValues(ex.ContentRange.ToString());
                    await ctx.Response.StartAsync();
                    await ctx.Response.Body.WriteAsync(msg);
                    await ctx.Response.CompleteAsync();
                    return;
                }
                ctx.Response.RegisterForDispose(content);
                //Copy required headers
                foreach (var kv in content.Headers)
                {
                    ctx.Response.Headers[kv.Key] = new Microsoft.Extensions.Primitives.StringValues([.. kv.Value]);
                }
                await ctx.Response.StartAsync();
                await content.CopyToAsync(ctx.Response.Body);
            }
        }
        await ctx.Response.CompleteAsync();
    }

    private static async Task SendDirectory(HttpContext ctx, DirectoryEntry[] result)
    {
        //Show dir listing
        var dir = $"<h1>Directory listing of {Html.Encode(ctx.Request.Path.Value ?? "/")}</h1>" +
            "<table><thead><tr><th>Name</th><th>Last modified</th><th>Size</th></tr></thead><tbody>" +
            string.Concat(result.OrderBy(m => m.FullPath, StringComparer.InvariantCultureIgnoreCase).Select(TableRow)) +
            "</tbody></table>";
        dir = Html.Body($"Directory listing of {Html.Encode(ctx.Request.Path.Value ?? "/")}", dir);
        ctx.Response.ContentType = "text/html; charset=utf-8";
        ctx.Response.ContentLength = Encoding.UTF8.GetByteCount(dir);
        await ctx.Response.StartAsync();
        await ctx.Response.WriteAsync(dir);
        await ctx.Response.CompleteAsync();
    }

    private static string TableRow(DirectoryEntry entry)
    {
        var absUrl = Html.Encode(entry.FullPath);
        var name = Html.Encode(string.IsNullOrEmpty(entry.Name) ? entry.FullPath.TrimEnd('/').Split('/').Last() + "/" : entry.Name);
        var ret = $"<td><a href=\"{absUrl}\">{name}</a></td>";
        if (entry.LastModified.HasValue)
        {
            ret += $"<td><time datetime=\"{entry.LastModified.Value:s}Z\">{entry.LastModified:R}</time></td>";
        }
        else
        {
            ret += "<td></td>";
        }
        if (entry.Size.HasValue)
        {
            ret += $"<td title=\"{entry.Size.Value} bytes\">{FormatValue(entry.Size.Value)}</td>";
        }
        else
        {
            ret += "<td></td>";
        }
        return "<tr>" + ret + "</tr>";
    }

    private static string FormatValue(long value)
    {
        const string suffixes = "BKMGTPE";
        int index = 0;
        double ret = value;
        while (Math.Abs(ret) >= 1024.0 && index < suffixes.Length - 1)
        {
            ++index;
            ret /= 1024.0;
        }
        return ret.ToString(index == 0 ? "0" : "0.00") + " " + suffixes[index];
    }
}
