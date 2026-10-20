using SharpSevenZip;

namespace ServeArc.Archive;

internal class SevenZip : IArchiveHandler
{
    private record ArchiveEntry(ArchiveFileInfo Info, string UriPath);

    private readonly SharpSevenZipExtractor arc;

    private readonly ArchiveEntry[] entries;

    public bool IsFileThreadSafe => false;

    public string[] Paths => [.. entries.Select(m => m.UriPath)];

    public SevenZip(string file)
    {
        arc = new SharpSevenZipExtractor(file);
        entries = [.. arc.ArchiveFileData.Select(m => new ArchiveEntry(m, m.FileName.Replace(Path.DirectorySeparatorChar, '/') + (m.IsDirectory ? "/" : "")))];
    }

    public DirectoryEntry[]? Browse(string dir)
    {
        var filtered = new List<ArchiveEntry>();

        if (dir == "/")
        {
            //Root directory: Everything without a slash, or only one at the end
            foreach (var item in entries)
            {
                if (Utils.Path.IsDirectChild(item.UriPath))
                {
                    filtered.Add(item);
                }
            }
        }
        else if (dir.EndsWith('/'))
        {
            //Non-root directory
            var baseDir = dir[1..];
            foreach (var item in entries.Where(m => m.UriPath.StartsWith(baseDir) && m.UriPath != baseDir))
            {
                var name = item.UriPath[baseDir.Length..];
                //Filter out items that are inside of subdirectories
                if (Utils.Path.IsDirectChild(name))
                {
                    filtered.Add(item);
                }
            }
        }
        else
        {
            return null;
        }
        return [.. filtered.Select(m => new DirectoryEntry("/" + m.UriPath, Path.GetFileName(m.UriPath), m.Info.LastWriteTime, m.UriPath.EndsWith('/') ? null : (long)m.Info.Size))];
    }

    public FileResult? GetFile(string file)
    {
        //File delivery
        var ze = entries.FirstOrDefault(m => m.UriPath == file[1..]);
        if (ze != null && !ze.Info.IsDirectory)
        {
            return new(arc.OpenFileStream(ze.Info.Index), ze.Info.LastWriteTime, ze.Info.Size, null);
        }
        return null;
    }

    public async Task StreamFile(string file, Stream target)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(target);
        var ze = entries.FirstOrDefault(m => m.UriPath == file[1..]);
        if (ze != null && !ze.Info.IsDirectory)
        {
            //Workaround for async I/O
            //using var ms = new MemoryStream();
            //await arc.ExtractFileAsync(ze.Info.Index, ms);
            //ms.Position = 0;
            //await ms.CopyToAsync(target);

            //The stream writes by this call are not actually async
            await arc.ExtractFileAsync(ze.Info.Index, target);
        }
        else
        {
            throw new ArgumentException($"Path '{file}' does not exist or it does not refer to a file", nameof(file));
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        arc.Dispose();
    }
}
