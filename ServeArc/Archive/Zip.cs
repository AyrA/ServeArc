using System.IO.Compression;

namespace ServeArc.Archive;

internal class Zip : IArchiveHandler
{
    private readonly ZipArchive arc;
    private readonly ZipArchiveEntry[] entries;

    public bool IsFileThreadSafe => false;

    public string[] Paths => [.. entries.Select(m => "/" + m.FullName)];

    public Zip(string archive)
    {
        arc = ZipFile.OpenRead(archive);
        entries = [.. arc.Entries];
    }

    public DirectoryEntry[]? Browse(string dir)
    {
        var filtered = new List<ZipArchiveEntry>();

        if (dir == "/")
        {
            //Root directory: Everything without a slash, or only one at the end
            foreach (var item in entries)
            {
                if (Utils.Path.IsDirectChild(item.FullName))
                {
                    filtered.Add(item);
                }
            }
        }
        else if (dir.EndsWith('/'))
        {
            //Non-root directory
            var baseDir = dir[1..];
            foreach (var item in entries.Where(m => m.FullName.StartsWith(baseDir) && m.FullName != baseDir))
            {
                var name = item.FullName[baseDir.Length..];
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
        return [.. filtered.Select(m => new DirectoryEntry("/" + m.FullName, m.Name, m.LastWriteTime, m.Name.EndsWith('/') ? null : m.Length))];
    }

    public FileResult? GetFile(string dir)
    {
        //File delivery
        var ze = entries.FirstOrDefault(m => m.FullName == dir[1..]);
        if (ze != null)
        {
            return new(ze.Open(), ze.LastWriteTime, ze.Length, null);
        }
        return null;
    }

    public void Dispose()
    {
        arc.Dispose();
    }
}