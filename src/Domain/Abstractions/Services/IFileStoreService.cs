using Shared.Models.Files;
using Shared.Models.Results;

namespace Domain.Abstractions.Services;

public interface IFileStoreService
{
    Task<Result<FileResponse>> GetFileAsync(string fileName, CancellationToken cancellationToken = default);

    Task<Result<string>> SaveFileAsync(FileRequest fileRequest, string subDirectory, CancellationToken cancellationToken = default);

    Result<bool> DeleteFile(string fileName);

    Task<Result<bool>> DeleteFilesAsync(IEnumerable<string> fileNames, CancellationToken cancellationToken = default);
}