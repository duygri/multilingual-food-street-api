namespace NarrationApp.Server.Services;

public sealed class MockStorageService(string rootPath) : IStorageService
{
    public string ProviderName => "mock-storage";

    public async Task<(string StoragePath, string Url)> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(rootPath);

        var safeFileName = Path.GetFileName(fileName);
        var storedFileName = $"{Guid.NewGuid():N}_{safeFileName}";
        var fullPath = Path.Combine(rootPath, storedFileName);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return (fullPath, $"/audio/{storedFileName}");
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        Stream stream = File.OpenRead(storagePath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (File.Exists(storagePath))
        {
            File.Delete(storagePath);
        }

        return Task.CompletedTask;
    }
}
