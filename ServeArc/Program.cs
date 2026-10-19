using ServeArc;
using ServeArc.Archive;

Arguments a;
try
{
    a = new(args);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine("Use '/?' to get help");
    return 2;
}

if (a.Help || a.ArchiveFiles.Count == 0)
{
    Console.WriteLine(@"ServeArc [/NB] [/AS | /SR ] <file> [file ...]

Serves files via HTTP directly from the given archives.
Latter archives take precedence over previous ones on path collisions,
allowing you to patch archives (additive only).
Supported types:
- zip: Full support (files + folders)
- tar: Limited support (no support for hard links or symbolic links)
       If the file is gzip compressed, it will be decompressed into a
       temporary directory first

/NB     Disable directory browsing. Directory requests return '404 Not Found'

/AS     Auto skip the root directory if its name is the same as the archive.
        The match is case insensitive, and without the file extension

/SR     Skip the root level. Mutually exclusive with /AS.
        This argument is like /AS but without the name constraint");
    return 1;
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ArchiveProcessor>();
var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
using var proc = app.Services.GetRequiredService<ArchiveProcessor>();

//Register all archives into the http processing queue
foreach (var file in a.ArchiveFiles)
{
    var arc = ArchiveHandler.OpenFile(file);
    if (a.AutoSkipRoot || a.SkipRoot)
    {
        var root = ServeArc.Utils.Path.GetCommonRoot(arc.Paths, true);
        if (!string.IsNullOrEmpty(root) && (a.SkipRoot || root.StartsWith(Path.GetFileNameWithoutExtension(file) + "/", StringComparison.InvariantCultureIgnoreCase)))
        {
            logger.LogInformation("Registering {File} as archive, stripping root path '{Root}'", file, root);
            proc.AddArchive(arc, root);
            proc.UpdateLastModified(File.GetLastWriteTimeUtc(file));
        }
        else
        {
            throw new ArgumentException($"Cannot skip root in '{file}'. File is empty or contents do not have a shared root", nameof(args));
        }
    }
    else
    {
        logger.LogInformation("Registering {File} as archive", file);
        proc.AddArchive(arc);
        proc.UpdateLastModified(File.GetLastWriteTimeUtc(file));
    }
}
proc.DisableDirectoryBrowsing = a.NoBrowse;

app.Use(proc.ProcessRequest);
app.Run();
return 0;