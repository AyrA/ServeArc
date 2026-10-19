using Microsoft.AspNetCore.StaticFiles;

namespace ServeArc.Utils;

public static class MimeType
{
    private static readonly FileExtensionContentTypeProvider mimeProvider = new();

    public static string GetMimeType(string path, string fallback = "application/octet-stream")
    {
        return mimeProvider.TryGetContentType(path, out var result) ? result : fallback;
    }
}
