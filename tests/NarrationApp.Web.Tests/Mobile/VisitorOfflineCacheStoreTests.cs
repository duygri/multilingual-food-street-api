using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorOfflineCacheStoreTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"visitor-cache-tests-{Guid.NewGuid():N}");
    private readonly List<VisitorOfflineCacheStore> _stores = [];

    [Fact]
    public async Task SaveContentSnapshotAsync_RoundTripsContentThroughSqlite()
    {
        var store = CreateStore();
        var snapshot = new VisitorContentSnapshot(
            [
                new VisitorPoi(
                    "poi-7",
                    "Bến Nhà Rồng",
                    "di-tich",
                    "Di tích",
                    "Quận 4",
                    "Offline",
                    "Nội dung đã cache.",
                    "Đã tải",
                    50,
                    50,
                    0,
                    "01:30",
                    "Có cache",
                    10.7609,
                    106.7054,
                    ReadyAudioLanguageCodesRaw: ["vi", "en"])
            ],
            [
                new VisitorTourCard("tour-1", "Tour cache", "1 điểm dừng", "15 phút", "Dễ", "Tour offline", ["poi-7"])
            ],
            [
                new VisitorCategory("di-tich", "Di tích", "🏛️")
            ]);

        await store.SaveContentSnapshotAsync(snapshot);

        var loaded = await store.LoadContentSnapshotAsync();

        Assert.NotNull(loaded);
        Assert.Equal("Bến Nhà Rồng", Assert.Single(loaded.Pois).Name);
        Assert.Equal(["en", "vi"], Assert.Single(loaded.Pois).ReadyAudioLanguageCodes.OrderBy(code => code));
        Assert.Equal("Tour cache", Assert.Single(loaded.Tours).Title);
        Assert.Equal("Di tích", Assert.Single(loaded.Categories!).Label);
    }

    [Fact]
    public async Task CacheAudioAsync_WritesAudioFileAndIndexesItForSettings()
    {
        var store = CreateStore();
        await using var audioBytes = new MemoryStream([1, 2, 3, 4, 5]);

        var entry = await store.CacheAudioAsync(
            new VisitorAudioCacheRequest(
                PoiId: "poi-7",
                PoiName: "Bến Nhà Rồng",
                LanguageCode: "en",
                SourceUrl: "https://example.test/audio.mp3",
                SourceLabel: "Google TTS",
                StatusLabel: "Sẵn sàng phát offline • EN",
                DurationSeconds: 88),
            audioBytes);

        Assert.True(File.Exists(entry.LocalFilePath));
        Assert.Equal(5, new FileInfo(entry.LocalFilePath).Length);

        var cachedAudio = Assert.Single(await store.ListCachedAudioAsync());
        Assert.Equal(entry.Id, cachedAudio.Id);
        Assert.Equal("Bến Nhà Rồng", cachedAudio.PoiName);

        var best = await store.FindBestAudioAsync("poi-7", "en");
        Assert.NotNull(best);
        Assert.Equal(entry.LocalFilePath, best.LocalFilePath);
    }

    [Fact]
    public async Task ClearCachedAudioAsync_RemovesDatabaseRowsAndFiles()
    {
        var store = CreateStore();
        await using var audioBytes = new MemoryStream([1, 2, 3]);
        var entry = await store.CacheAudioAsync(
            new VisitorAudioCacheRequest(
                PoiId: "poi-7",
                PoiName: "Bến Nhà Rồng",
                LanguageCode: "vi",
                SourceUrl: "https://example.test/audio.mp3",
                SourceLabel: "Recorded",
                StatusLabel: "Sẵn sàng phát offline • VI",
                DurationSeconds: 77),
            audioBytes);

        await store.ClearCachedAudioAsync();

        Assert.Empty(await store.ListCachedAudioAsync());
        Assert.False(File.Exists(entry.LocalFilePath));
    }

    public void Dispose()
    {
        foreach (var store in _stores)
        {
            store.Dispose();
        }

        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private VisitorOfflineCacheStore CreateStore()
    {
        Directory.CreateDirectory(_tempRoot);
        var store = new VisitorOfflineCacheStore(
            Path.Combine(_tempRoot, "visitor-cache.db3"),
            Path.Combine(_tempRoot, "audio"));
        _stores.Add(store);
        return store;
    }
}
