using Microsoft.AspNetCore.Http.Features;
using ServeArc.Archive;
using ServeArc.Utils;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace ServeArc;

public class ArchiveProcessor(ILogger<ArchiveProcessor> logger) : IDisposable
{
    private record Archive(IArchiveHandler Handler, string? Prefix, SemaphoreSlim ThreadLock);

    private readonly List<Archive> handlers = [];

    private string etag = Convert.ToBase64String(Encoding.UTF8.GetBytes(nameof(ArchiveProcessor)));

    /// <summary>
    /// Truncated header value for ETag. About 124 bits of entropy
    /// </summary>
    private string EtagHeaderValue => etag[..20];

    public IReadOnlyCollection<IArchiveHandler> Handlers => handlers
        .Select(m => m.Handler)
        .ToArray()
        .AsReadOnly();

    public bool DisableDirectoryBrowsing { get; set; }

    public void UpdateCacheString(string archive)
    {
        IncrementalHash(archive);
        IncrementalHash(File.GetLastWriteTimeUtc(archive).ToString("R"));
        logger.LogInformation("Adding archive '{Archive}' to the cache string updated it to {ETag}", archive, etag);
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
        string? reqEtag = ctx.Request.Headers.IfNoneMatch.FirstOrDefault()?.Trim('"');

        ctx.Response.Headers.ETag = '"' + EtagHeaderValue + '"';
        //Abort early on cache match
        if (EtagHeaderValue.Equals(reqEtag))
        {
            logger.LogInformation("Cached response for {Url}", ctx.Request.Path);
            ctx.Response.StatusCode = 304;
            await ctx.Response.StartAsync();
            await ctx.Response.CompleteAsync();
            return;
        }

        DirectoryEntry[]? dirEntries = null;
        //For directory last modified handling, do before enumerating entries
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
                            if (f.File == null)
                            {
                                await StreamFile(ctx, f, path, archive.Handler);
                            }
                            else
                            {
                                //Make stream seekable in order to support range requests
                                if (!f.File.CanSeek)
                                {
                                    f = f with { File = new ReadSeekableStream(f.File, 100_000_000, (long?)f.Size) };
                                }
                                await SendFile(ctx, f);
                            }
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

    private async Task StreamFile(HttpContext ctx, FileResult file, string filePath, IArchiveHandler archive)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(archive);
        logger.LogInformation("Streaming file without range support");

        //Need to allow sync writes for this to work with all archive implementations
        var syncIOFeature = ctx.Features.Get<IHttpBodyControlFeature>();
        syncIOFeature?.AllowSynchronousIO = true;

        SetHeaders(ctx, file, false);

        //This type of streaming does not support range requests
        await ctx.Response.StartAsync();
        await archive.StreamFile(filePath, ctx.Response.Body);
        await ctx.Response.CompleteAsync();
    }

    private async Task SendFile(HttpContext ctx, FileResult file)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(file.File);
        logger.LogInformation("Sending file with range support");
        ctx.Response.RegisterForDispose(file.File);

        SetHeaders(ctx, file, true);

        var mediaType = file.MimeType ?? MimeType.GetMimeType(ctx.Request.Path);

        //If no range header is present, return the full file data
        if (!RangeHeaderValue.TryParse(ctx.Request.Headers.Range.FirstOrDefault(), out var range))
        {
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

    private static void SetHeaders(HttpContext ctx, FileResult file, bool supportRanges)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(file);

        var mediaType = file.MimeType ?? MimeType.GetMimeType(ctx.Request.Path);
        //Set headers we want regardless of the response type
        if (supportRanges)
        {
            ctx.Response.Headers.AcceptRanges = "bytes";
        }
        if (file.LastModified.HasValue)
        {
            ctx.Response.Headers.LastModified = file.LastModified.Value.ToString("R");
        }

        //If a range header is present, content length and type will be set by the range processor
        if (!RangeHeaderValue.TryParse(ctx.Request.Headers.Range.FirstOrDefault(), out _))
        {
            if (file.Size.HasValue)
            {
                ctx.Response.ContentLength = (long?)file.Size.Value;
            }
            ctx.Response.ContentType = mediaType;
        }
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

    private void IncrementalHash(string value)
    {
        var hash = HMACSHA256.HashData(Convert.FromBase64String(etag), Encoding.UTF8.GetBytes(value));
        etag = Convert.ToBase64String(hash);
    }
}
