namespace ServeArc.Archive;

public static class ArchiveHandler
{
    private const StringComparison CI = StringComparison.InvariantCultureIgnoreCase;
    public static IArchiveHandler OpenFile(string file)
    {
        if (file.EndsWith(".zip", CI))
        {
            return new Zip(file);
        }
        if (file.EndsWith(".tar", CI))
        {
            return new Tar(file, false);
        }
        if (file.EndsWith(".tar.gz", CI) || file.EndsWith(".tgz", CI))
        {
            return new Tar(file, true);
        }
        throw new ArgumentException("Unable to detect supported archive type from given file name", nameof(file));
    }
}
