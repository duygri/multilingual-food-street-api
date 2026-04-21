namespace NarrationApp.Server.Services;

public interface IR2ObjectClient
{
    Task SaveAsync(string bucketName, string objectKey, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);
}
