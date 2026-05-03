using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

internal sealed class FakeVisitorOfflineCacheStore : IVisitorOfflineCacheStore
{
    public VisitorContentSnapshot? ContentSnapshot { get; set; }

    public List<VisitorAudioCacheEntry> AudioEntries { get; } = [];

    public List<VisitorAudioCacheRequest> CacheRequests { get; } = [];

    public byte[] LastCachedAudioBytes { get; private set; } = [];

    public Task SaveContentSnapshotAsync(VisitorContentSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ContentSnapshot = snapshot;
        return Task.CompletedTask;
    }

    public Task<VisitorContentSnapshot?> LoadContentSnapshotAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ContentSnapshot);
    }

    public Task<VisitorAudioCacheEntry?> FindBestAudioAsync(
        string poiId,
        string preferredLanguageCode,
        CancellationToken cancellationToken = default)
    {
        var entry = AudioEntries
            .Where(item => string.Equals(item.PoiId, poiId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => string.Equals(item.LanguageCode, preferredLanguageCode, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(item => string.Equals(item.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .FirstOrDefault();

        return Task.FromResult(entry);
    }

    public async Task<VisitorAudioCacheEntry> CacheAudioAsync(
        VisitorAudioCacheRequest request,
        Stream audioStream,
        CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await audioStream.CopyToAsync(buffer, cancellationToken);
        LastCachedAudioBytes = buffer.ToArray();
        CacheRequests.Add(request);

        var entry = new VisitorAudioCacheEntry(
            Id: $"cache-{request.PoiId}-{request.LanguageCode}",
            PoiId: request.PoiId,
            PoiName: request.PoiName,
            LanguageCode: request.LanguageCode,
            LocalFilePath: $@"D:\cache\{request.PoiId}-{request.LanguageCode}.mp3",
            SourceUrl: request.SourceUrl,
            SourceLabel: request.SourceLabel,
            StatusLabel: request.StatusLabel,
            DurationSeconds: request.DurationSeconds,
            SizeBytes: LastCachedAudioBytes.Length,
            CachedAtUtc: DateTimeOffset.UtcNow);

        AudioEntries.Add(entry);
        return entry;
    }

    public Task<IReadOnlyList<VisitorCachedAudioItem>> ListCachedAudioAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<VisitorCachedAudioItem> items = AudioEntries
            .Select(entry => entry.ToCachedAudioItem())
            .ToArray();

        return Task.FromResult(items);
    }

    public Task DeleteCachedAudioAsync(string itemId, CancellationToken cancellationToken = default)
    {
        AudioEntries.RemoveAll(entry => string.Equals(entry.Id, itemId, StringComparison.OrdinalIgnoreCase));
        return Task.CompletedTask;
    }

    public Task ClearCachedAudioAsync(CancellationToken cancellationToken = default)
    {
        AudioEntries.Clear();
        return Task.CompletedTask;
    }
}
