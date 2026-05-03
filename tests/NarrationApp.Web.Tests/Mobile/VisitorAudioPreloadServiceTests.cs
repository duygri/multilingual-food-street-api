using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorAudioPreloadServiceTests
{
    [Fact]
    public async Task PreloadAsync_DownloadsOnlyPoisWithSelectedLanguageThatAreNotAlreadyCached()
    {
        var cacheStore = new FakeVisitorOfflineCacheStore();
        cacheStore.AudioEntries.Add(new VisitorAudioCacheEntry(
            Id: "cache-poi-1-en",
            PoiId: "poi-1",
            PoiName: "Cầu Khánh Hội",
            LanguageCode: "en",
            LocalFilePath: @"D:\cache\poi-1-en.mp3",
            SourceUrl: "https://example.test/audio/1",
            SourceLabel: "Google TTS",
            StatusLabel: "Sẵn sàng phát offline • EN",
            DurationSeconds: 90,
            SizeBytes: 2048,
            CachedAtUtc: DateTimeOffset.UtcNow));
        var audioCatalog = new FakeVisitorAudioCatalogService();
        var service = new VisitorAudioPreloadService(audioCatalog, cacheStore);

        var result = await service.PreloadAsync(
            [
                CreatePoi("poi-1", "Cầu Khánh Hội", ["en"]),
                CreatePoi("poi-2", "Bến Nhà Rồng", ["en", "vi"]),
                CreatePoi("poi-3", "Phố đêm Xóm Chiếu", ["vi"])
            ],
            "en");

        Assert.Equal(2, result.Total);
        Assert.Equal(1, result.Downloaded);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(0, result.Failed);

        var request = Assert.Single(audioCatalog.Requests);
        Assert.Equal("poi-2", request.PoiId);
        Assert.Equal("en", request.LanguageCode);
        Assert.Equal("Bến Nhà Rồng", request.PoiName);
    }

    [Fact]
    public async Task PreloadAsync_ReportsProgressForEachDownloadCandidate()
    {
        var cacheStore = new FakeVisitorOfflineCacheStore();
        var audioCatalog = new FakeVisitorAudioCatalogService();
        var service = new VisitorAudioPreloadService(audioCatalog, cacheStore);
        var progress = new CapturingProgress();

        await service.PreloadAsync(
            [
                CreatePoi("poi-1", "Cầu Khánh Hội", ["en"]),
                CreatePoi("poi-2", "Bến Nhà Rồng", ["en"])
            ],
            "en",
            progress);

        var progressEvents = progress.Events;
        Assert.Contains(progressEvents, progress => progress.Completed == 0 && progress.Total == 2);
        Assert.Contains(progressEvents, progress => progress.Completed == 1 && progress.Total == 2);
        Assert.Contains(progressEvents, progress => progress.Completed == 2 && progress.Total == 2);
    }

    [Fact]
    public async Task PreloadAsync_CountsUnavailableAudioAsFailed()
    {
        var cacheStore = new FakeVisitorOfflineCacheStore();
        var audioCatalog = new FakeVisitorAudioCatalogService();
        audioCatalog.UnavailablePoiIds.Add("poi-2");
        var service = new VisitorAudioPreloadService(audioCatalog, cacheStore);

        var result = await service.PreloadAsync(
            [
                CreatePoi("poi-1", "Cầu Khánh Hội", ["en"]),
                CreatePoi("poi-2", "Bến Nhà Rồng", ["en"])
            ],
            "en");

        Assert.Equal(2, result.Total);
        Assert.Equal(1, result.Downloaded);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(1, result.Failed);
    }

    private static VisitorPoi CreatePoi(string id, string name, IReadOnlyList<string> readyLanguages)
    {
        return new VisitorPoi(
            id,
            name,
            "history",
            "Di tích",
            "Quận 4",
            "Live API",
            "Mô tả POI",
            "Điểm nổi bật",
            32,
            48,
            120,
            "2:00",
            "Sẵn sàng",
            10.7609,
            106.7054,
            ReadyAudioLanguageCodesRaw: readyLanguages);
    }

    private sealed class FakeVisitorAudioCatalogService : IVisitorAudioCatalogService
    {
        public List<(string PoiId, string LanguageCode, string? PoiName)> Requests { get; } = [];

        public HashSet<string> UnavailablePoiIds { get; } = [];

        public Task<VisitorAudioCue> LoadBestForPoiAsync(
            string poiId,
            string preferredLanguageCode,
            string? poiName = null,
            CancellationToken cancellationToken = default)
        {
            Requests.Add((poiId, preferredLanguageCode, poiName));
            if (UnavailablePoiIds.Contains(poiId))
            {
                return Task.FromResult(VisitorAudioCue.Unavailable(poiId, "Chưa có audio."));
            }

            return Task.FromResult(new VisitorAudioCue(
                PoiId: poiId,
                LanguageCode: preferredLanguageCode,
                StreamUrl: $@"file:///D:/cache/{poiId}-{preferredLanguageCode}.mp3",
                DurationSeconds: 90,
                IsAvailable: true,
                StatusLabel: $"Sẵn sàng phát offline • {preferredLanguageCode.ToUpperInvariant()}",
                IsPreferredLanguage: true));
        }
    }

    private sealed class CapturingProgress : IProgress<VisitorAudioPreloadProgress>
    {
        public List<VisitorAudioPreloadProgress> Events { get; } = [];

        public void Report(VisitorAudioPreloadProgress value)
        {
            Events.Add(value);
        }
    }
}
