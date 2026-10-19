namespace ServeArc.Archive;

public interface IArchiveHandler : IDisposable
{
    /// <summary>
    /// Gets if <see cref="GetFile(string)"/> is thread safe.
    /// If not, the HTTP pipeline will ensure that only one file is being processed from this archive at once.
    /// </summary>
    /// <remarks><see cref="Browse(string)"/> is always assumed to be thread safe</remarks>
    bool IsFileThreadSafe { get; }

    /// <summary>
    /// Gets all path strings contained in the archive
    /// </summary>
    string[] Paths { get; }

    /// <summary>
    /// Gets all direct child objects (files and directories) in the given directory
    /// </summary>
    /// <param name="dir">The path of the directory to browse. Always begins and ends in '/'.</param>
    /// <returns>
    /// An array of DirectoryEntry objects representing the entries in the directory,
    /// or null if the request cannot be handled and should be sent to the next handler in the queue
    /// </returns>
    DirectoryEntry[]? Browse(string dir);

    /// <summary>
    /// Returns the file stream for a file
    /// </summary>
    /// <param name="dir">Path of the file entry. Always begins with '/'</param>
    /// <returns>
    /// File result if file was found,
    /// null if the request cannot be handled and should be sent to the next handler in the queue
    /// </returns>
    /// <remarks>The stream will be disposed after the request chain ends</remarks>
    FileResult? GetFile(string dir);
}
