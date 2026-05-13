using System.Globalization;
using System.Text.Json;
using SQLite;

namespace NarrationApp.Mobile.Features.Home;

public interface IVisitorOfflineCacheStore
{
    Task SaveContentSnapshotAsync(VisitorContentSnapshot snapshot, CancellationToken cancellationToken = default);

    Task<VisitorContentSnapshot?> LoadContentSnapshotAsync(CancellationToken cancellationToken = default);

    Task<VisitorAudioCacheEntry?> FindBestAudioAsync(
        string poiId,
        string preferredLanguageCode,
        CancellationToken cancellationToken = default);

    Task<VisitorAudioCacheEntry> CacheAudioAsync(
        VisitorAudioCacheRequest request,
        Stream audioStream,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VisitorCachedAudioItem>> ListCachedAudioAsync(CancellationToken cancellationToken = default);

    Task DeleteCachedAudioAsync(string itemId, CancellationToken cancellationToken = default);

    Task ClearCachedAudioAsync(CancellationToken cancellationToken = default);
}

public sealed record VisitorAudioCacheRequest(
    string PoiId,
    string PoiName,
    string LanguageCode,
    string SourceUrl,
    string SourceLabel,
    string StatusLabel,
    int DurationSeconds);

public sealed record VisitorAudioCacheEntry(
    string Id,
    string PoiId,
    string PoiName,
    string LanguageCode,
    string LocalFilePath,
    string SourceUrl,
    string SourceLabel,
    string StatusLabel,
    int DurationSeconds,
    long SizeBytes,
    DateTimeOffset CachedAtUtc)
{
    public VisitorCachedAudioItem ToCachedAudioItem()
    {
        var sizeMb = SizeBytes <= 0
            ? 0d
            : SizeBytes / 1024d / 1024d;
        return new VisitorCachedAudioItem(
            Id,
            PoiId,
            PoiName,
            LanguageCode,
            SourceLabel,
            sizeMb,
            $"Cập nhật {CachedAtUtc.ToLocalTime():dd/MM HH:mm}");
    }
}

public sealed partial class VisitorOfflineCacheStore : IVisitorOfflineCacheStore, IDisposable
{
    private const string ContentSnapshotKey = "visitor-content";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _audioDirectory;
    private readonly SQLiteAsyncConnection _connection;
    private readonly Lazy<Task> _initializeTask;

    public VisitorOfflineCacheStore(string databasePath, string audioDirectory)
    {
        _audioDirectory = audioDirectory;
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath) ?? ".");
        Directory.CreateDirectory(_audioDirectory);

        SQLitePCL.Batteries_V2.Init();
        _connection = new SQLiteAsyncConnection(databasePath);
        _initializeTask = new Lazy<Task>(InitializeAsync);
    }

    public async Task SaveContentSnapshotAsync(VisitorContentSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureInitializedAsync();

        var entity = new OfflineContentSnapshotEntity
        {
            Key = ContentSnapshotKey,
            Json = JsonSerializer.Serialize(snapshot, JsonOptions),
            UpdatedAtUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        await _connection.InsertOrReplaceAsync(entity);
    }

    public async Task<VisitorContentSnapshot?> LoadContentSnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureInitializedAsync();

        var entity = await _connection.FindAsync<OfflineContentSnapshotEntity>(ContentSnapshotKey);
        if (entity is null || string.IsNullOrWhiteSpace(entity.Json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<VisitorContentSnapshot>(entity.Json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<VisitorAudioCacheEntry?> FindBestAudioAsync(
        string poiId,
        string preferredLanguageCode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureInitializedAsync();

        var entities = await _connection.Table<CachedAudioEntity>()
            .Where(entity => entity.PoiId == poiId)
            .ToListAsync();
        var normalizedPreferredLanguageCode = NormalizeLanguageCode(preferredLanguageCode);
        var entity = entities
            .Where(item => File.Exists(item.LocalFilePath))
            .Where(item => string.Equals(item.LanguageCode, normalizedPreferredLanguageCode, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => string.Equals(item.LanguageCode, normalizedPreferredLanguageCode, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenByDescending(item => ParseTimestamp(item.CachedAtUtc))
            .FirstOrDefault();

        return entity?.ToEntry();
    }

    public async Task<VisitorAudioCacheEntry> CacheAudioAsync(
        VisitorAudioCacheRequest request,
        Stream audioStream,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureInitializedAsync();

        var id = CreateAudioCacheId(request.PoiId, request.LanguageCode, request.SourceUrl);
        var extension = ResolveAudioExtension(request.SourceUrl);
        var localFilePath = Path.Combine(_audioDirectory, $"{id}{extension}");

        await using (var fileStream = File.Create(localFilePath))
        {
            await audioStream.CopyToAsync(fileStream, cancellationToken);
        }

        var fileInfo = new FileInfo(localFilePath);
        var entity = new CachedAudioEntity
        {
            Id = id,
            PoiId = request.PoiId,
            PoiName = request.PoiName,
            LanguageCode = request.LanguageCode,
            LocalFilePath = localFilePath,
            SourceUrl = request.SourceUrl,
            SourceLabel = request.SourceLabel,
            StatusLabel = request.StatusLabel,
            DurationSeconds = request.DurationSeconds,
            SizeBytes = fileInfo.Length,
            CachedAtUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        await _connection.InsertOrReplaceAsync(entity);
        return entity.ToEntry();
    }

    public async Task<IReadOnlyList<VisitorCachedAudioItem>> ListCachedAudioAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureInitializedAsync();

        var entities = await _connection.Table<CachedAudioEntity>().ToListAsync();
        return entities
            .Where(entity => File.Exists(entity.LocalFilePath))
            .OrderByDescending(entity => ParseTimestamp(entity.CachedAtUtc))
            .Select(entity => entity.ToEntry().ToCachedAudioItem())
            .ToArray();
    }

    public async Task DeleteCachedAudioAsync(string itemId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureInitializedAsync();

        var entity = await _connection.FindAsync<CachedAudioEntity>(itemId);
        if (entity is null)
        {
            return;
        }

        DeleteFileIfExists(entity.LocalFilePath);
        await _connection.DeleteAsync(entity);
    }

    public async Task ClearCachedAudioAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureInitializedAsync();

        var entities = await _connection.Table<CachedAudioEntity>().ToListAsync();
        foreach (var entity in entities)
        {
            DeleteFileIfExists(entity.LocalFilePath);
        }

        await _connection.DeleteAllAsync<CachedAudioEntity>();
    }

    public void Dispose()
    {
        _connection.CloseAsync().GetAwaiter().GetResult();
    }

    private Task EnsureInitializedAsync() => _initializeTask.Value;

    private async Task InitializeAsync()
    {
        await _connection.CreateTableAsync<OfflineContentSnapshotEntity>();
        await _connection.CreateTableAsync<CachedAudioEntity>();
    }
}
