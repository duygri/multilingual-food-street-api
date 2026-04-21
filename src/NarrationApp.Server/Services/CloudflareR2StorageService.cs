using Microsoft.Extensions.Options;
using NarrationApp.Server.Configuration;

namespace NarrationApp.Server.Services;

public sealed class CloudflareR2StorageService(IR2ObjectClient objectClient, IOptions<CloudflareR2Options> options) : IStorageService
{
    private readonly CloudflareR2Options _options = options.Value;

    public string ProviderName => "cloudflare-r2";

    public async Task<(string StoragePath, string Url)> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var safeFileName = Path.GetFileName(fileName);
        var objectKey = $"audio/{Guid.NewGuid():N}_{safeFileName}";
        await objectClient.SaveAsync(_options.BucketName, objectKey, content, GetContentType(safeFileName), cancellationToken);
        return (objectKey, BuildPublicUrl(objectKey));
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        return objectClient.OpenReadAsync(_options.BucketName, storagePath, cancellationToken);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        return objectClient.DeleteAsync(_options.BucketName, storagePath, cancellationToken);
    }

    private string BuildPublicUrl(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return string.Empty;
        }

        return $"{_options.PublicBaseUrl.Trim().TrimEnd('/')}/{objectKey}";
    }

    private static string GetContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            _ => "application/octet-stream"
        };
    }
}
