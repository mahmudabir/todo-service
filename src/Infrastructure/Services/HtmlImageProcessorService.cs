using System.Collections.Concurrent;
using System.Text.RegularExpressions;

using Domain.Abstractions.Services;

using Microsoft.AspNetCore.Http;

using Shared.Constants;
using Shared.Extensions;
using Shared.Models.Files;

namespace Infrastructure.Services;

public class HtmlImageProcessorService(IFileStoreService fileStoreService, IHttpContextAccessor httpContextAccessor) : IHtmlImageProcessorService
{
    private static readonly Regex ImageUrlRegex = new Regex("<img[^>]*?src=\\\"https?://[^/]+/api/files/(?<code>[^\"]+)\\\"[^>]*?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Base64ImageRegex = new Regex("<img[^>]*?src=\\\"data:image/(?<type>[^;]+);base64,(?<data>[^\"]+)\\\"[^>]*?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SrcAttributeRegex = new Regex("src=\\\"[^\"]+\\\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);


    public async Task<string> ProcessBase64ImagesAsync(string html,
                                                       string subDirectory = null,
                                                       CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(html)) return html;

        var matches = Base64ImageRegex.Matches(html);
        if (matches.Count == 0) return html;

        subDirectory = string.IsNullOrEmpty(subDirectory) ? DirectoryNames.CommonFiles : subDirectory;
        var baseUrl = $"{httpContextAccessor.HttpContext?.Request.Scheme}://{httpContextAccessor.HttpContext?.Request.Host}/api/files/";

        // Process all matches in parallel and collect results
        var replacements = new ConcurrentDictionary<string, string>();
        var tasks = matches.Select(async match =>
        {
            try
            {
                var replacement = await ProcessMatchAsync(match, subDirectory, baseUrl, ct);
                if (replacement.newTag != null)
                {
                    replacements.TryAdd(replacement.originalTag, replacement.newTag);
                }
            }
            catch (Exception ex)
            {
                // Log exception if needed
                // Continue with other replacements
            }
        });

        await Task.WhenAll(tasks);

        // Perform all replacements at once
        return replacements.Aggregate(html,
                                      (current, replacement) =>
                                          current.Replace(replacement.Key, replacement.Value));
    }

    private async Task<(string originalTag, string? newTag)> ProcessMatchAsync(Match match,
                                                                               string subDirectory,
                                                                               string baseUrl,
                                                                               CancellationToken ct)
    {
        var originalTag = match.Value;

        try
        {
            var base64 = match.Groups["data"].Value;
            var type = match.Groups["type"].Value;
            var bytes = Convert.FromBase64String(base64);

            var fileName = $"img_{Guid.CreateVersion7():N}.{type}";
            var mimeType = MimeMapping.GetMimeMapping(type);

            var fileRequest = new FileRequest(bytes, fileName, mimeType);
            var fileSaveResult = await fileStoreService.SaveFileAsync(fileRequest, subDirectory, ct);

            if (fileSaveResult.IsFailure)
            {
                return (originalTag, null);
            }

            var fileUrl = baseUrl + fileSaveResult.Payload.StringToBase64();
            var newTag = SrcAttributeRegex.Replace(originalTag, $"src=\"{fileUrl}\"");

            return (originalTag, newTag);
        }
        catch
        {
            return (originalTag, null);
        }
    }

    public List<string> ExtractImageUrls(string? html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return [];
        }

        var matches = ImageUrlRegex.Matches(html);
        if (matches.Count == 0)
        {
            return [];
        }

        // Process in parallel and merge results
        const int ParallelizationThreshold = 100;
        if (matches.Count >= ParallelizationThreshold)
        {
            return matches.AsParallel()
                          .WithDegreeOfParallelism(Environment.ProcessorCount)
                          .Select(match => match.Success ? match.Groups["code"].Value : null)
                          .Where(url => url != null)
                          .Distinct(StringComparer.Ordinal)
                          .ToList()!;
        }

        var uniqueUrls = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in matches)
        {
            if (!match.Success) continue;
            var group = match.Groups["code"];
            if (group.Success)
            {
                uniqueUrls.Add(group.Value);
            }
        }
        return [..uniqueUrls];
    }
}