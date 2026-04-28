using NarrationApp.Shared.DTOs.Geofence;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Pages.Owner;

public partial class PoiDetail
{
    private sealed class PoiEditModel
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public int Priority { get; set; }
        public int? CategoryId { get; set; }
        public NarrationMode NarrationMode { get; set; }
        public string Description { get; set; } = string.Empty;
        public string TtsScript { get; set; } = string.Empty;
        public string? MapLink { get; set; }
        public string? ImageUrl { get; set; }

        public static PoiEditModel FromPoi(PoiDto poi)
        {
            return new PoiEditModel
            {
                Name = poi.Name,
                Slug = poi.Slug,
                Lat = poi.Lat,
                Lng = poi.Lng,
                Priority = poi.Priority,
                CategoryId = poi.CategoryId,
                NarrationMode = poi.NarrationMode,
                Description = poi.Description,
                TtsScript = poi.TtsScript,
                MapLink = poi.MapLink,
                ImageUrl = poi.ImageUrl
            };
        }

        public UpdatePoiRequest ToRequest(PoiDto poi)
        {
            return new UpdatePoiRequest
            {
                Name = Name,
                Slug = Slug,
                Lat = Lat,
                Lng = Lng,
                Priority = Priority,
                CategoryId = CategoryId,
                NarrationMode = NarrationMode,
                Description = Description,
                TtsScript = TtsScript,
                MapLink = MapLink,
                ImageUrl = ImageUrl,
                Status = poi.Status
            };
        }
    }

    private sealed class GeofenceEditModel
    {
        public string Name { get; set; } = "Vùng kích hoạt chính";
        public int RadiusMeters { get; set; } = 35;
        public int Priority { get; set; } = 10;
        public int DebounceSeconds { get; set; } = 10;
        public int CooldownSeconds { get; set; } = 600;
        public bool IsActive { get; set; } = true;
        public string TriggerAction { get; set; } = "auto_play";
        public bool NearestOnly { get; set; } = true;

        public static GeofenceEditModel FromPoi(PoiDto poi)
        {
            var geofence = poi.Geofences.FirstOrDefault();
            return geofence is null ? new GeofenceEditModel() : FromGeofence(geofence);
        }

        public static GeofenceEditModel FromGeofence(GeofenceDto geofence)
        {
            return new GeofenceEditModel
            {
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

        public UpdateGeofenceRequest ToRequest()
        {
            return new UpdateGeofenceRequest
            {
                Name = Name,
                RadiusMeters = RadiusMeters,
                Priority = Priority,
                DebounceSeconds = DebounceSeconds,
                CooldownSeconds = CooldownSeconds,
                IsActive = IsActive,
                TriggerAction = TriggerAction,
                NearestOnly = NearestOnly
            };
        }
    }
}
