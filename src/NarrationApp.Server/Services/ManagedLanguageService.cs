using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Languages;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed class ManagedLanguageService(AppDbContext dbContext) : IManagedLanguageService
{
    public async Task<IReadOnlyList<ManagedLanguageDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        var totalPoiCount = await dbContext.Pois.CountAsync(cancellationToken);
        var languages = await dbContext.ManagedLanguages
            .AsNoTracking()
            .OrderBy(language => language.Role)
            .ThenBy(language => language.Code)
            .ToListAsync(cancellationToken);

        var translationCounts = await dbContext.PoiTranslations
            .AsNoTracking()
            .GroupBy(item => item.LanguageCode)
            .Select(group => new { group.Key, Count = group.Select(item => item.PoiId).Distinct().Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);

        var audioCounts = await dbContext.AudioAssets
            .AsNoTracking()
            .GroupBy(item => item.LanguageCode)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);

        return languages.Select(language => new ManagedLanguageDto
        {
            Code = language.Code,
            DisplayName = language.DisplayName,
            NativeName = language.NativeName,
            FlagCode = language.FlagCode,
            Role = language.Role,
            IsActive = language.IsActive,
            TranslationCoverageCount = translationCounts.TryGetValue(language.Code, out var translationCount) ? translationCount : 0,
            TranslationCoverageTotal = totalPoiCount,
            AudioCount = audioCounts.TryGetValue(language.Code, out var audioCount) ? audioCount : 0
        }).ToArray();
    }

    public async Task<ManagedLanguageDto> CreateAsync(CreateManagedLanguageRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedCode = request.Code.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            throw new InvalidOperationException("Language code is required.");
        }

        var existing = await dbContext.ManagedLanguages
            .SingleOrDefaultAsync(item => item.Code == normalizedCode, cancellationToken);

        if (existing is not null)
        {
            existing.DisplayName = request.DisplayName.Trim();
            existing.NativeName = request.NativeName.Trim();
            existing.FlagCode = request.FlagCode.Trim().ToUpperInvariant();
            existing.IsActive = true;
        }
        else
        {
            existing = new ManagedLanguage
            {
                Code = normalizedCode,
                DisplayName = request.DisplayName.Trim(),
                NativeName = request.NativeName.Trim(),
                FlagCode = request.FlagCode.Trim().ToUpperInvariant(),
                Role = string.Equals(normalizedCode, AppConstants.DefaultLanguage, StringComparison.OrdinalIgnoreCase)
                    ? ManagedLanguageRole.Source
                    : ManagedLanguageRole.TranslationAudio,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            dbContext.ManagedLanguages.Add(existing);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetAsync(cancellationToken))
            .Single(item => item.Code == normalizedCode);
    }

    public async Task DeleteAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToLowerInvariant();
        if (string.Equals(normalizedCode, AppConstants.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Vietnamese source language cannot be removed.");
        }

        var language = await dbContext.ManagedLanguages
            .SingleOrDefaultAsync(item => item.Code == normalizedCode, cancellationToken)
            ?? throw new KeyNotFoundException("Managed language was not found.");

        dbContext.ManagedLanguages.Remove(language);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
