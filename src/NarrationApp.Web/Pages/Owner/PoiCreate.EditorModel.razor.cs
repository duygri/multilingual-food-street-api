using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Pages.Owner;

public partial class PoiCreate
{
    private sealed class PoiCreateModel
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public int Priority { get; set; } = 10;
        public NarrationMode NarrationMode { get; set; } = NarrationMode.TtsOnly;
        public string Description { get; set; } = string.Empty;
        public string TtsScript { get; set; } = string.Empty;
        public string? MapLink { get; set; }

        public static PoiCreateModel CreateDefault()
        {
            return new PoiCreateModel
            {
                Lat = 10.758d,
                Lng = 106.701d
            };
        }

        public CreatePoiRequest ToRequest(int? categoryId)
        {
            return new CreatePoiRequest
            {
                Name = Name,
                Slug = Slug,
                Lat = Lat,
                Lng = Lng,
                Priority = Priority,
                CategoryId = categoryId,
                NarrationMode = NarrationMode,
                Description = Description,
                TtsScript = TtsScript,
                MapLink = MapLink
            };
        }
    }
}
