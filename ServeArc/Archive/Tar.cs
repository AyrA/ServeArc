using System.Collections;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace ServeArc.Archive;

internal class Tar : IArchiveHandler
{
    private string? fileDeleteName;
    private readonly TarReader tarFile;
    private readonly TarEntry[] entries;

    public string[] Paths => [.. entries.Select(FP)];

    public bool IsFileThreadSafe => false;

    public Tar(string file, bool gzip)
    {
        if (gzip)
        {
            using var fs = File.OpenRead(file);
            using var gz = new GZipStream(fs, CompressionMode.Decompress);
            using var temp = File.Create(fileDeleteName = file = Path.GetTempFileName());
            gz.CopyTo(temp);
        }
        tarFile = new TarReader(File.OpenRead(file));
        entries = TarEnumerable.GetAll(tarFile);
    }

    public DirectoryEntry[]? Browse(string dir)
    {
        var filtered = new List<TarEntry>();

        if (dir == "/")
        {
            //Root directory: Everything without a slash, or only one at the end
            foreach (var item in entries)
            {
                if (Utils.Path.IsDirectChild(item.Name))
                {
                    filtered.Add(item);
                }
            }
        }
        else if (dir.EndsWith('/'))
        {
            //Non-root directory
            var baseDir = dir[1..];
            foreach (var item in entries.Where(m => m.Name.StartsWith(baseDir) && m.Name != baseDir))
            {
                var name = item.Name[baseDir.Length..];
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
        return [.. filtered.Select(m => new DirectoryEntry("/" + m.Name, Path.GetFileName(m.Name), m.ModificationTime, m.Name.EndsWith('/') ? null : m.Length))];
    }

    public FileResult? GetFile(string dir)
    {
        //File delivery
        var ze = entries.FirstOrDefault(m => m.Name == dir[1..]);
        if (ze?.DataStream != null)
        {
            ze.DataStream.Position = 0;
            return new(new Utils.StreamWrapper(ze.DataStream), ze.ModificationTime, ze.Length, null);
        }
        return null;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        tarFile.Dispose();
        if (!string.IsNullOrEmpty(fileDeleteName))
        {
            try
            {
                File.Delete(fileDeleteName);
            }
            catch
            {
                //NOOP
            }
            fileDeleteName = null;
        }
    }

    private static string FP(TarEntry e)
    {
        var name = e.Name;
        if (!name.EndsWith('/') && e.EntryType == TarEntryType.Directory)
        {
            name += "/";
        }
        if (!name.StartsWith('/'))
        {
            name = "/" + name;
        }
        return name;
    }

    private class TarEnumerable(TarReader reader) : IEnumerable<TarEntry>
    {
        public static TarEntry[] GetAll(TarReader reader) => new TarEnumerable(reader).ToArray();

        public IEnumerator<TarEntry> GetEnumerator()
        {
            return new TarEnumerator(reader);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private class TarEnumerator(TarReader reader) : IEnumerator<TarEntry>
    {
        private TarEntry? current;
        public TarEntry Current => current ?? throw InvOp();

        object IEnumerator.Current => current ?? throw InvOp();

        public void Dispose()
        {
            current = default!;
            GC.SuppressFinalize(this);
        }

        public bool MoveNext()
        {
            current = reader.GetNextEntry()!;
            return current != null;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static InvalidOperationException InvOp()
            => new("Enumeration has not yet started, or has reached the end");
    }
}