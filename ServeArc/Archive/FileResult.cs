namespace ServeArc.Archive;

/// <summary>
/// Result of requesting a file
/// </summary>
/// <param name="File">Stream containing file contents. This will be sent to the client as-is</param>
/// <param name="LastModified">Date the file was last written to</param>
/// <param name="Size">File size to report, null if not known. If specified, the value must be exact, or the request will fail</param>
/// <param name="MimeType">File mime type to use, or null in order to autodetect from the file name</param>
public record FileResult(Stream File, DateTimeOffset? LastModified, long? Size, string? MimeType);
