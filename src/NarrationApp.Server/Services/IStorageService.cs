namespace NarrationApp.Server.Services;

public interface IStorageService
{
    string ProviderName { get; }

    Task<(string StoragePath, string Url)> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}
