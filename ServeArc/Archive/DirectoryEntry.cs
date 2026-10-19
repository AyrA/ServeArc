namespace ServeArc.Archive;

/// <summary>
/// Represents a directory entry
/// </summary>
/// <param name="FullPath">Full path, this is used 'as-is' for links when generating a directory listing</param>
/// <param name="Name">Display name, this is used as the link text when generating a directory listing</param>
/// <param name="LastModified">Last modified date, or null if not known</param>
/// <param name="Size">Size, or null if not known. Usually null for directories</param>
public record DirectoryEntry(string FullPath, string Name, DateTimeOffset? LastModified, long? Size);