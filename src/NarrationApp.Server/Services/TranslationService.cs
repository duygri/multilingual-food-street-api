using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Translation;

namespace NarrationApp.Server.Services;

public sealed class TranslationService(
    AppDbContext dbContext,
    IGoogleTranslationService googleTranslationService,
    IAudioGenerationScheduler? audioGenerationScheduler = null) : ITranslationService
{
    public async Task<IReadOnlyList<TranslationDto>> GetByPoiAsync(int poiId, CancellationToken cancellationToken = default)
    {
        var translations = await dbContext.PoiTranslations
            .AsNoTracking()
            .Where(translation => translation.PoiId == poiId)
            .OrderBy(translation => translation.LanguageCode)
            .ToListAsync(cancellationToken);

        return translations.Select(translation => translation.ToDto()).ToArray();
    }

    public async Task<TranslationDto> UpsertAsync(CreateTranslationRequest request, CancellationToken cancellationToken = default)
    {
        _ = await dbContext.Pois.SingleOrDefaultAsync(poi => poi.Id == request.PoiId, cancellationToken)
            ?? throw new KeyNotFoundException("POI was not found.");

        var normalizedLanguage = request.LanguageCode.Trim().ToLowerInvariant();
        var translation = await dbContext.PoiTranslations
            .SingleOrDefaultAsync(
                item => item.PoiId == request.PoiId && item.LanguageCode == normalizedLanguage,
                cancellationToken);

        if (translation is null)
        {
            translation = new PoiTranslation
            {
                PoiId = request.PoiId,
                LanguageCode = normalizedLanguage
            };

            dbContext.PoiTranslations.Add(translation);
        }

        translation.Title = request.Title;
        translation.Description = request.Description;
        translation.Story = request.Story;
        translation.Highlight = request.Highlight;
        translation.IsFallback = normalizedLanguage != AppConstants.DefaultLanguage && request.IsFallback;

        await dbContext.SaveChangesAsync(cancellationToken);

        if (!string.Equals(normalizedLanguage, AppConstants.DefaultLanguage, StringComparison.OrdinalIgnoreCase)
            && audioGenerationScheduler is not null)
        {
            await audioGenerationScheduler.QueueFromTranslationAsync(request.PoiId, normalizedLanguage, cancellationToken: cancellationToken);
        }

        return translation.ToDto();
    }

    public async Task<TranslationDto> AutoTranslateAsync(int poiId, string targetLanguage, CancellationToken cancellationToken = default)
    {
        var sourceTranslation = await dbContext.PoiTranslations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                translation => translation.PoiId == poiId && translation.LanguageCode == AppConstants.DefaultLanguage,
                cancellationToken)
            ?? throw new KeyNotFoundException("Default Vietnamese translation was not found.");

        var normalizedTargetLanguage = targetLanguage.Trim().ToLowerInvariant();

        return await UpsertAsync(
            new CreateTranslationRequest
            {
                PoiId = poiId,
                LanguageCode = normalizedTargetLanguage,
                Title = await googleTranslationService.TranslateAsync(sourceTranslation.Title, AppConstants.DefaultLanguage, normalizedTargetLanguage, cancellationToken),
                Description = await googleTranslationService.TranslateAsync(sourceTranslation.Description, AppConstants.DefaultLanguage, normalizedTargetLanguage, cancellationToken),
                Story = await googleTranslationService.TranslateAsync(sourceTranslation.Story, AppConstants.DefaultLanguage, normalizedTargetLanguage, cancellationToken),
                Highlight = await googleTranslationService.TranslateAsync(sourceTranslation.Highlight, AppConstants.DefaultLanguage, normalizedTargetLanguage, cancellationToken),
                IsFallback = true
            },
            cancellationToken);
    }

    public async Task DeleteAsync(int translationId, CancellationToken cancellationToken = default)
    {
        var translation = await dbContext.PoiTranslations
            .SingleOrDefaultAsync(item => item.Id == translationId, cancellationToken)
            ?? throw new KeyNotFoundException("Translation was not found.");

        dbContext.PoiTranslations.Remove(translation);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
