using Domain.Abstractions.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Shared.Extensions;
using Shared.Models.Files;
using Shared.Models.Results;

namespace Infrastructure.Services;

public class FileStoreService : IFileStoreService
{
    private readonly string _uploadPath;
    private readonly ILogger<FileStoreService> _logger;

    public FileStoreService(IConfiguration configuration, ILogger<FileStoreService> logger)
    {
        _uploadPath = configuration["FileStorage:UploadPath"] ?? Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName, "Uploads");
        _logger = logger;
    }

    public async Task<Result<FileResponse>> GetFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }

            string filePath = Path.Combine(_uploadPath, fileName);

            if (!File.Exists(filePath))
                return null;

            byte[] fileContent = await File.ReadAllBytesAsync(filePath, cancellationToken);
            string contentType = MimeMapping.GetMimeMapping(fileName);

            return Result<FileResponse>.Success().WithPayload(new FileResponse(fileContent, contentType, fileName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file");
            return Result<FileResponse>.Error().WithMessage("Error retrieving file");
        }
    }

    public async Task<Result<string>> SaveFileAsync(FileRequest fileRequest, string subDirectory, CancellationToken cancellationToken = default)
    {
        try
        {
            if (fileRequest?.FileContent == null || string.IsNullOrWhiteSpace(fileRequest.FileName))
                return Result<string>.Error().WithMessage("Invalid file request");

            fileRequest.FileName = fileRequest.FileName.Replace("#", string.Empty);

            var directoryAbsolutePath = Path.Combine(_uploadPath, subDirectory);
            if (!Directory.Exists(directoryAbsolutePath))
            {
                Directory.CreateDirectory(directoryAbsolutePath);
            }

            var relativeFilePath = Path.Combine(subDirectory, $"{Guid.CreateVersion7():N}#{fileRequest.FileName}");
            string absoluteFilePath = Path.Combine(_uploadPath, relativeFilePath);

            await File.WriteAllBytesAsync(absoluteFilePath, fileRequest.FileContent, cancellationToken);

            _logger.LogInformation("File saved: {AbsoluteFilePath}", absoluteFilePath);

            return Result<string>.Success().WithPayload(relativeFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file");
            return Result<string>.Error().WithMessage("Error saving file");
        }
    }

    public Result<bool> DeleteFile(string fileName)
    {
        try
        {
            if (fileName.IsNullOrEmpty())
                return Result<bool>.Success();

            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }

            string filePath = Path.Combine(_uploadPath, fileName);

            if (!File.Exists(filePath))
                return Result<bool>.Success().WithMessage("File does not exist");

            File.Delete(filePath);
            _logger.LogInformation("File deleted: {FilePath}", filePath);

            return Result<bool>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file");
            return Result<bool>.Error().WithMessage("Error deleting file");
        }
    }

    public async Task<Result<bool>> DeleteFilesAsync(IEnumerable<string> fileNames, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }

            await Parallel.ForEachAsync(fileNames, cancellationToken, async (fileName, ct) =>
            {
                DeleteFile(fileName);
            });

            return Result<bool>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file");
            return Result<bool>.Error().WithMessage("Error deleting file");
        }
    }
}