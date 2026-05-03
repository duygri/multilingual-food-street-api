using System.Globalization;
using System.Security.Cryptography;
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

public sealed class VisitorOfflineCacheStore : IVisitorOfflineCacheStore, IDisposable
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
        var entity = entities
            .Where(item => File.Exists(item.LocalFilePath))
            .OrderBy(item => string.Equals(item.LanguageCode, preferredLanguageCode, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(item => string.Equals(item.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
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

    private static string CreateAudioCacheId(string poiId, string languageCode, string sourceUrl)
    {
        var input = $"{poiId}|{languageCode}|{sourceUrl}";
        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input)))[..16].ToLowerInvariant();
        return $"cache-{SanitizeFileName(poiId)}-{languageCode.ToLowerInvariant()}-{hash}";
    }

    private static string ResolveAudioExtension(string sourceUrl)
    {
        if (Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
        {
            var extension = Path.GetExtension(uri.AbsolutePath);
            return string.IsNullOrWhiteSpace(extension) ? ".mp3" : extension;
        }

        return ".mp3";
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(character => invalidChars.Contains(character) ? '-' : character));
    }

    private static DateTimeOffset ParseTimestamp(string value)
    {
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp)
            ? timestamp
            : DateTimeOffset.MinValue;
    }

    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private sealed class OfflineContentSnapshotEntity
    {
        [PrimaryKey]
        public string Key { get; set; } = string.Empty;

        public string Json { get; set; } = string.Empty;

        public string UpdatedAtUtc { get; set; } = string.Empty;
    }

    private sealed class CachedAudioEntity
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        [Indexed]
        public string PoiId { get; set; } = string.Empty;

        public string PoiName { get; set; } = string.Empty;

        [Indexed]
        public string LanguageCode { get; set; } = string.Empty;

        public string LocalFilePath { get; set; } = string.Empty;

        public string SourceUrl { get; set; } = string.Empty;

        public string SourceLabel { get; set; } = string.Empty;

        public string StatusLabel { get; set; } = string.Empty;

        public int DurationSeconds { get; set; }

        public long SizeBytes { get; set; }

        public string CachedAtUtc { get; set; } = string.Empty;

        public VisitorAudioCacheEntry ToEntry()
        {
            return new VisitorAudioCacheEntry(
                Id,
                PoiId,
                PoiName,
                LanguageCode,
                LocalFilePath,
                SourceUrl,
                SourceLabel,
                StatusLabel,
                DurationSeconds,
                SizeBytes,
                ParseTimestamp(CachedAtUtc));
        }
    }
}
