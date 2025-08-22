namespace Infrastructure.Services;

public static class MimeMapping
{
    private static readonly Dictionary<string, string> MimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        {
            ".txt", "text/plain"
        },
        {
            ".html", "text/html"
        },
        {
            ".htm", "text/html"
        },
        {
            ".css", "text/css"
        },
        {
            ".js", "application/javascript"
        },
        {
            ".json", "application/json"
        },
        {
            ".xml", "application/xml"
        },
        {
            ".csv", "text/csv"
        },
        {
            ".jpg", "image/jpeg"
        },
        {
            ".jpeg", "image/jpeg"
        },
        {
            ".png", "image/png"
        },
        {
            ".gif", "image/gif"
        },
        {
            ".bmp", "image/bmp"
        },
        {
            ".svg", "image/svg+xml"
        },
        {
            ".ico", "image/x-icon"
        },
        {
            ".pdf", "application/pdf"
        },
        {
            ".zip", "application/zip"
        },
        {
            ".rar", "application/x-rar-compressed"
        },
        {
            ".tar", "application/x-tar"
        },
        {
            ".mp3", "audio/mpeg"
        },
        {
            ".wav", "audio/wav"
        },
        {
            ".mp4", "video/mp4"
        },
        {
            ".avi", "video/x-msvideo"
        },
        {
            ".mov", "video/quicktime"
        },
        {
            ".flv", "video/x-flv"
        },
        {
            ".wmv", "video/x-ms-wmv"
        },
        {
            ".doc", "application/msword"
        },
        {
            ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        },
        {
            ".xls", "application/vnd.ms-excel"
        },
        {
            ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        },
        {
            ".ppt", "application/vnd.ms-powerpoint"
        },
        {
            ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"
        }
    };

    public static string GetMimeMapping(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "application/octet-stream"; // Default unknown type

        string extension = Path.GetExtension(fileName)?.ToLower();

        return extension != null && MimeTypes.TryGetValue(extension, out string mimeType)
            ? mimeType
            : "application/octet-stream"; // Default unknown type
    }
}