using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Category;
using NarrationApp.Shared.DTOs.Geofence;
using NarrationApp.Shared.DTOs.Notification;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.DTOs.QR;
using NarrationApp.Shared.DTOs.Translation;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

internal static class ServiceMappingExtensions
{
    public static PoiDto ToDto(this Poi poi)
    {
        return new PoiDto
        {
            Id = poi.Id,
            Name = poi.Name,
            Slug = poi.Slug,
            OwnerId = poi.OwnerId,
            Lat = poi.Lat,
            Lng = poi.Lng,
            Priority = poi.Priority,
            CategoryId = poi.CategoryId,
            CategoryName = poi.Category?.Name,
            NarrationMode = poi.NarrationMode,
            Description = poi.Description,
            TtsScript = poi.TtsScript,
            MapLink = poi.MapLink,
            ImageUrl = poi.ImageUrl,
            Status = poi.Status,
            CreatedAtUtc = poi.CreatedAt,
            Translations = poi.Translations.Select(translation => translation.ToDto()).ToArray(),
            Geofences = poi.Geofences.Select(geofence => geofence.ToDto()).ToArray()
        };
    }

    public static CategoryDto ToDto(this Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            Icon = category.Icon,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive
        };
    }

    public static TranslationDto ToDto(this PoiTranslation translation)
    {
        return new TranslationDto
        {
            Id = translation.Id,
            PoiId = translation.PoiId,
            LanguageCode = translation.LanguageCode,
            Title = translation.Title,
            Description = translation.Description,
            Story = translation.Story,
            Highlight = translation.Highlight,
            IsFallback = translation.IsFallback,
            WorkflowStatus = translation.GetWorkflowStatus()
        };
    }

    public static GeofenceDto ToDto(this Geofence geofence)
    {
        return new GeofenceDto
        {
            Id = geofence.Id,
            PoiId = geofence.PoiId,
            Name = geofence.Name,
            RadiusMeters = geofence.RadiusMeters,
            Priority = geofence.Priority,
            DebounceSeconds = geofence.DebounceSeconds,
            CooldownSeconds = geofence.CooldownSeconds,
            IsActive = geofence.IsActive,
            TriggerAction = geofence.TriggerAction,
            NearestOnly = geofence.NearestOnly
        };
    }

    public static AudioDto ToDto(this AudioAsset audioAsset)
    {
        return new AudioDto
        {
            Id = audioAsset.Id,
            PoiId = audioAsset.PoiId,
            LanguageCode = audioAsset.LanguageCode,
            SourceType = audioAsset.SourceType,
            Provider = audioAsset.Provider,
            StoragePath = audioAsset.StoragePath,
            Url = audioAsset.Url,
            Status = audioAsset.Status,
            DurationSeconds = audioAsset.DurationSeconds,
            GeneratedAtUtc = audioAsset.GeneratedAt
        };
    }

    public static QrCodeDto ToDto(this QrCode qrCode)
    {
        return new QrCodeDto
        {
            Id = qrCode.Id,
            Code = qrCode.Code,
            TargetType = qrCode.TargetType,
            TargetId = qrCode.TargetId,
            LocationHint = qrCode.LocationHint,
            ExpiresAtUtc = qrCode.ExpiresAt
        };
    }

    public static NotificationDto ToDto(this Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Type = notification.Type,
            Title = notification.Title,
            Message = notification.Message,
            IsRead = notification.IsRead,
            CreatedAtUtc = notification.CreatedAt
        };
    }

    private static TranslationWorkflowStatus GetWorkflowStatus(this PoiTranslation translation)
    {
        if (string.Equals(translation.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase))
        {
            return TranslationWorkflowStatus.Source;
        }

        return translation.IsFallback
            ? TranslationWorkflowStatus.AutoTranslated
            : TranslationWorkflowStatus.Reviewed;
    }
}
