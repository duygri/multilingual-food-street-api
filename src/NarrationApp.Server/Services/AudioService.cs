using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed class AudioService(AppDbContext dbContext, IStorageService storageService, IGoogleTtsService googleTtsService) : IAudioService
{
    public async Task<IReadOnlyList<AudioDto>> GetByPoiAsync(int poiId, string? languageCode, CancellationToken cancellationToken = default)
    {
        var query = dbContext.AudioAssets
            .AsNoTracking()
            .Where(audio => audio.PoiId == poiId);

        if (!string.IsNullOrWhiteSpace(languageCode))
        {
            var normalizedLanguage = languageCode.Trim().ToLowerInvariant();
            query = query.Where(audio => audio.LanguageCode == normalizedLanguage);
        }

        var items = await query
            .OrderByDescending(audio => audio.GeneratedAt)
            .ToListAsync(cancellationToken);

        return items.Select(audio => audio.ToDto()).ToArray();
    }

    public async Task<AudioDto?> GetByIdAsync(int audioId, CancellationToken cancellationToken = default)
    {
        var audio = await dbContext.AudioAssets
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == audioId, cancellationToken);

        return audio?.ToDto();
    }

    public async Task<AudioDto> UploadAsync(Guid actorUserId, UserRole actorRole, UploadAudioRequest request, Stream content, CancellationToken cancellationToken = default)
    {
        var poi = await dbContext.Pois.SingleOrDefaultAsync(item => item.Id == request.PoiId, cancellationToken)
            ?? throw new KeyNotFoundException("POI was not found.");

        EnsureOwnerAccess(poi, actorUserId, actorRole);

        var (storagePath, url) = await storageService.SaveAsync(request.FileName, content, cancellationToken);
        var asset = new AudioAsset
        {
            PoiId = request.PoiId,
            LanguageCode = request.LanguageCode.Trim().ToLowerInvariant(),
            SourceType = AudioSourceType.Recorded,
            Provider = storageService.ProviderName,
            StoragePath = storagePath,
            Url = url,
            Status = AudioStatus.Ready,
            DurationSeconds = 5,
            GeneratedAt = DateTime.UtcNow
        };

        dbContext.AudioAssets.Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken);
        EnsureStreamUrl(asset);
        await dbContext.SaveChangesAsync(cancellationToken);

        return asset.ToDto();
    }

    public async Task<AudioDto> GenerateTtsAsync(Guid actorUserId, UserRole actorRole, TtsGenerateRequest request, CancellationToken cancellationToken = default)
    {
        var poi = await dbContext.Pois.SingleOrDefaultAsync(item => item.Id == request.PoiId, cancellationToken)
            ?? throw new KeyNotFoundException("POI was not found.");

        EnsureOwnerAccess(poi, actorUserId, actorRole);

        var bytes = await googleTtsService.GenerateAsync(request.Script, request.LanguageCode, request.VoiceProfile, cancellationToken);
        await using var content = new MemoryStream(bytes);
        var (storagePath, url) = await storageService.SaveAsync($"tts_{request.PoiId}_{request.LanguageCode}.mp3", content, cancellationToken);

        var asset = new AudioAsset
        {
            PoiId = request.PoiId,
            LanguageCode = request.LanguageCode.Trim().ToLowerInvariant(),
            SourceType = AudioSourceType.Tts,
            Provider = googleTtsService.ProviderName,
            StoragePath = storagePath,
            Url = url,
            Status = AudioStatus.Ready,
            DurationSeconds = 5,
            GeneratedAt = DateTime.UtcNow
        };

        dbContext.AudioAssets.Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken);
        EnsureStreamUrl(asset);
        await dbContext.SaveChangesAsync(cancellationToken);

        return asset.ToDto();
    }

    public async Task<AudioDto> GenerateFromTranslationAsync(Guid actorUserId, UserRole actorRole, GenerateAudioFromTranslationRequest request, CancellationToken cancellationToken = default)
    {
        var poi = await dbContext.Pois.SingleOrDefaultAsync(item => item.Id == request.PoiId, cancellationToken)
            ?? throw new KeyNotFoundException("POI was not found.");

        EnsureOwnerAccess(poi, actorUserId, actorRole);

        var normalizedLanguage = request.LanguageCode.Trim().ToLowerInvariant();
        if (string.Equals(normalizedLanguage, AppConstants.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Vietnamese audio should be generated from the source script flow.");
        }

        var translation = await dbContext.PoiTranslations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.PoiId == request.PoiId && item.LanguageCode == normalizedLanguage,
                cancellationToken)
            ?? throw new KeyNotFoundException("Saved translation was not found.");

        var script = ResolveScript(translation);
        var bytes = await googleTtsService.GenerateAsync(script, normalizedLanguage, request.VoiceProfile, cancellationToken);
        await using var content = new MemoryStream(bytes);
        var (storagePath, url) = await storageService.SaveAsync($"tts_{request.PoiId}_{normalizedLanguage}.mp3", content, cancellationToken);

        var existing = await dbContext.AudioAssets
            .SingleOrDefaultAsync(
                item => item.PoiId == request.PoiId
                    && item.LanguageCode == normalizedLanguage
                    && item.SourceType == AudioSourceType.Tts,
                cancellationToken);

        var asset = existing ?? new AudioAsset
        {
            PoiId = request.PoiId,
            LanguageCode = normalizedLanguage,
            SourceType = AudioSourceType.Tts
        };

        asset.Provider = googleTtsService.ProviderName;
        asset.StoragePath = storagePath;
        asset.Url = url;
        asset.Status = AudioStatus.Ready;
        asset.DurationSeconds = 5;
        asset.GeneratedAt = DateTime.UtcNow;

        if (existing is null)
        {
            dbContext.AudioAssets.Add(asset);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        EnsureStreamUrl(asset);
        await dbContext.SaveChangesAsync(cancellationToken);

        return asset.ToDto();
    }

    public async Task<AudioDto> UpdateAsync(Guid actorUserId, UserRole actorRole, int audioId, UpdateAudioRequest request, CancellationToken cancellationToken = default)
    {
        var audio = await dbContext.AudioAssets
            .Include(item => item.Poi)
            .SingleOrDefaultAsync(item => item.Id == audioId, cancellationToken)
            ?? throw new KeyNotFoundException("Audio asset was not found.");

        EnsureOwnerAccess(audio.Poi ?? throw new InvalidOperationException("POI relation is missing."), actorUserId, actorRole);

        audio.LanguageCode = request.LanguageCode.Trim().ToLowerInvariant();
        audio.Provider = request.Provider;
        audio.StoragePath = request.StoragePath;
        audio.Url = request.Url;
        audio.Status = request.Status;
        audio.DurationSeconds = request.DurationSeconds;

        await dbContext.SaveChangesAsync(cancellationToken);
        return audio.ToDto();
    }

    public async Task DeleteAsync(Guid actorUserId, UserRole actorRole, int audioId, CancellationToken cancellationToken = default)
    {
        var audio = await dbContext.AudioAssets
            .Include(item => item.Poi)
            .SingleOrDefaultAsync(item => item.Id == audioId, cancellationToken)
            ?? throw new KeyNotFoundException("Audio asset was not found.");

        EnsureOwnerAccess(audio.Poi ?? throw new InvalidOperationException("POI relation is missing."), actorUserId, actorRole);

        await storageService.DeleteAsync(audio.StoragePath, cancellationToken);
        dbContext.AudioAssets.Remove(audio);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Stream> OpenReadStreamAsync(int audioId, CancellationToken cancellationToken = default)
    {
        var audio = await dbContext.AudioAssets
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == audioId, cancellationToken)
            ?? throw new KeyNotFoundException("Audio asset was not found.");

        return await storageService.OpenReadAsync(audio.StoragePath, cancellationToken);
    }

    private static void EnsureOwnerAccess(Poi poi, Guid actorUserId, UserRole actorRole)
    {
        if (actorRole == UserRole.Admin)
        {
            return;
        }

        if (actorRole != UserRole.PoiOwner || poi.OwnerId != actorUserId)
        {
            throw new UnauthorizedAccessException("You do not have permission to modify this audio asset.");
        }
    }

    private static void EnsureStreamUrl(AudioAsset asset)
    {
        if (string.IsNullOrWhiteSpace(asset.Url))
        {
            asset.Url = $"/api/audio/{asset.Id}/stream";
        }
    }

    private static string ResolveScript(PoiTranslation translation)
    {
        if (!string.IsNullOrWhiteSpace(translation.Story))
        {
            return translation.Story.Trim();
        }

        if (!string.IsNullOrWhiteSpace(translation.Description))
        {
            return translation.Description.Trim();
        }

        if (!string.IsNullOrWhiteSpace(translation.Highlight))
        {
            return translation.Highlight.Trim();
        }

        if (!string.IsNullOrWhiteSpace(translation.Title))
        {
            return translation.Title.Trim();
        }

        throw new InvalidOperationException("Translation does not contain any text to synthesize.");
    }
}
