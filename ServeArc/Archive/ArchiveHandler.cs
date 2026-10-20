namespace ServeArc.Archive;

public static class ArchiveHandler
{
    private const StringComparison CI = StringComparison.InvariantCultureIgnoreCase;

    public static IArchiveHandler OpenFile(string file)
    {
        //Use the internal decompressors for known types because they support HTTP range requests without creating intermeidate files
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
        try
        {
            return new SevenZip(file);
        }
        catch (Exception e)
        {
            throw new ArgumentException("Unable to detect supported archive type from given file name", nameof(file), e);
        }
    }
}
