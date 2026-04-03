namespace FoodStreet.Client.DTOs
{
    public static class RealtimeEventNames
    {
        public const string PoiUpdated = "poiUpdated";
        public const string MenuUpdated = "menuUpdated";
        public const string TranslationUpdated = "translationUpdated";
        public const string AudioReady = "audioReady";
        public const string TourPublished = "tourPublished";
        public const string QrScanned = "qrScanned";
        public const string ModerationChanged = "moderationChanged";
    }

    public class RealtimeActivityDto
    {
        public string EventName { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string? Status { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public string? TriggeredBy { get; set; }
        public DateTime OccurredAt { get; set; }
    }
}
