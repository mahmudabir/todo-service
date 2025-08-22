namespace Domain.Abstractions.Services;

public interface IHtmlImageProcessorService
{
    Task<string> ProcessBase64ImagesAsync(string html, string subDirectory = null, CancellationToken ct = default);

    List<string> ExtractImageUrls(string html);
}