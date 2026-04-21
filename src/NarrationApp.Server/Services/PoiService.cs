using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed class PoiService(AppDbContext dbContext) : IPoiService
{
    public async Task<IReadOnlyList<PoiDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var pois = await QueryPois()
            .Where(poi => poi.Status == PoiStatus.Published)
            .OrderByDescending(poi => poi.Priority)
            .ThenBy(poi => poi.Name)
            .ToListAsync(cancellationToken);

        return pois.Select(poi => poi.ToDto()).ToArray();
    }

    public async Task<PoiDto?> GetByIdAsync(int poiId, CancellationToken cancellationToken = default)
    {
        var poi = await QueryPois()
            .SingleOrDefaultAsync(item => item.Id == poiId && item.Status == PoiStatus.Published, cancellationToken);

        return poi?.ToDto();
    }

    public async Task<IReadOnlyList<PoiDto>> GetNearbyAsync(PoiNearRequest request, CancellationToken cancellationToken = default)
    {
        var pois = await QueryPois()
            .Where(poi => poi.Status == PoiStatus.Published)
            .ToListAsync(cancellationToken);

        return pois
            .Select(poi => new
            {
                Poi = poi,
                Distance = CalculateDistanceInMeters(request.Lat, request.Lng, poi.Lat, poi.Lng)
            })
            .Where(item => item.Distance <= request.RadiusMeters)
            .OrderBy(item => item.Distance)
            .ThenByDescending(item => item.Poi.Priority)
            .Select(item => item.Poi.ToDto())
            .ToArray();
    }

    public async Task<PoiDto> CreateAsync(Guid ownerId, CreatePoiRequest request, CancellationToken cancellationToken = default)
    {
        var poi = new Poi
        {
            Name = request.Name.Trim(),
            Slug = request.Slug.Trim().ToLowerInvariant(),
            OwnerId = ownerId,
            Lat = request.Lat,
            Lng = request.Lng,
            Priority = request.Priority,
            CategoryId = request.CategoryId,
            NarrationMode = request.NarrationMode,
            Description = request.Description.Trim(),
            TtsScript = request.TtsScript.Trim(),
            MapLink = request.MapLink,
            ImageUrl = request.ImageUrl,
            Status = PoiStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Pois.Add(poi);
        UpsertBaseTranslation(poi);
        await dbContext.SaveChangesAsync(cancellationToken);

        var createdPoi = await QueryPois().SingleAsync(item => item.Id == poi.Id, cancellationToken);
        return createdPoi.ToDto();
    }

    public async Task<PoiDto> UpdateAsync(Guid actorUserId, UserRole actorRole, int poiId, UpdatePoiRequest request, CancellationToken cancellationToken = default)
    {
        var poi = await dbContext.Pois
            .Include(item => item.Translations)
            .Include(item => item.Geofences)
            .Include(item => item.Category)
            .SingleOrDefaultAsync(item => item.Id == poiId, cancellationToken)
            ?? throw new KeyNotFoundException("POI was not found.");

        EnsureWriteAccess(poi, actorUserId, actorRole);

        poi.Name = request.Name.Trim();
        poi.Slug = request.Slug.Trim().ToLowerInvariant();
        poi.Lat = request.Lat;
        poi.Lng = request.Lng;
        poi.Priority = request.Priority;
        poi.CategoryId = request.CategoryId;
        poi.NarrationMode = request.NarrationMode;
        poi.Description = request.Description.Trim();
        poi.TtsScript = request.TtsScript.Trim();
        poi.MapLink = request.MapLink;
        poi.ImageUrl = request.ImageUrl;
        poi.Status = actorRole == UserRole.Admin ? request.Status : poi.Status == PoiStatus.Published ? PoiStatus.Updated : PoiStatus.Draft;

        UpsertBaseTranslation(poi);
        await dbContext.SaveChangesAsync(cancellationToken);

        return poi.ToDto();
    }

    public async Task DeleteAsync(Guid actorUserId, UserRole actorRole, int poiId, CancellationToken cancellationToken = default)
    {
        var poi = await dbContext.Pois.SingleOrDefaultAsync(item => item.Id == poiId, cancellationToken)
            ?? throw new KeyNotFoundException("POI was not found.");

        EnsureWriteAccess(poi, actorUserId, actorRole);

        dbContext.Pois.Remove(poi);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Poi> QueryPois()
    {
        return dbContext.Pois
            .AsNoTracking()
            .Include(poi => poi.Translations)
            .Include(poi => poi.Geofences)
            .Include(poi => poi.Category);
    }

    private static void EnsureWriteAccess(Poi poi, Guid actorUserId, UserRole actorRole)
    {
        if (actorRole == UserRole.Admin)
        {
            return;
        }

        if (actorRole != UserRole.PoiOwner || poi.OwnerId != actorUserId)
        {
            throw new UnauthorizedAccessException("You do not have permission to modify this POI.");
        }
    }

    private static double CalculateDistanceInMeters(double sourceLat, double sourceLng, double targetLat, double targetLng)
    {
        const double EarthRadiusMeters = 6_371_000;

        var dLat = DegreesToRadians(targetLat - sourceLat);
        var dLng = DegreesToRadians(targetLng - sourceLng);
        var a =
            Math.Pow(Math.Sin(dLat / 2), 2) +
            Math.Cos(DegreesToRadians(sourceLat)) *
            Math.Cos(DegreesToRadians(targetLat)) *
            Math.Pow(Math.Sin(dLng / 2), 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;

    private void UpsertBaseTranslation(Poi poi)
    {
        var translation = poi.Translations.SingleOrDefault(item => item.LanguageCode == AppConstants.DefaultLanguage);
        if (translation is null)
        {
            translation = new PoiTranslation
            {
                Poi = poi,
                LanguageCode = AppConstants.DefaultLanguage
            };

            poi.Translations.Add(translation);
        }

        translation.Title = poi.Name;
        translation.Description = poi.Description;
        translation.Story = string.IsNullOrWhiteSpace(poi.TtsScript) ? poi.Description : poi.TtsScript;
        translation.Highlight = BuildHighlight(poi.Description);
        translation.IsFallback = false;
    }

    private static string BuildHighlight(string description)
    {
        var trimmed = description.Trim();
        return trimmed.Length <= 160 ? trimmed : $"{trimmed[..157]}...";
    }
}
