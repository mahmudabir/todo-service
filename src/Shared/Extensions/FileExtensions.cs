using Microsoft.AspNetCore.Http;

using Shared.Models.Files;

namespace Shared.Extensions;

public static class FileExtensions
{
    public static FileRequest ToFileRequest(this IFormFile? file)
    {
        return file == null ? null : new FileRequest(file);
    }
}