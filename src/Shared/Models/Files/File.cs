using Microsoft.AspNetCore.Http;

namespace Shared.Models.Files;

public class FileRequest
{
    public byte[] FileContent { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }

    public FileRequest(IFormFile file)
    {
        if (file is null) return;

        using var memoryStream = new MemoryStream();
        file.CopyTo(memoryStream);

        FileContent = memoryStream.ToArray();
        FileName = file.FileName;
        ContentType = file.ContentType;
    }

    public FileRequest(byte[] fileContent, string fileName, string contentType)
    {
        if (fileContent is null) return;

        FileContent = fileContent;
        FileName = fileName;
        ContentType = contentType;
    }
}

public class FileResponse
{
    public byte[] FileContent { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }

    public FileResponse(byte[] fileContent, string contentType, string fileName)
    {
        FileContent = fileContent;
        ContentType = contentType;
        FileName = fileName;
    }
}