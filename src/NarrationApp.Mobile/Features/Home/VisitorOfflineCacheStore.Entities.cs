using System.Globalization;
using System.Security.Cryptography;
using SQLite;

namespace NarrationApp.Mobile.Features.Home;

public sealed partial class VisitorOfflineCacheStore
{
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

    private static string NormalizeLanguageCode(string languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? "vi"
            : languageCode.Trim().ToLowerInvariant();
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
