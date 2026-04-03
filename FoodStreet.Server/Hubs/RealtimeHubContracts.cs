using Microsoft.AspNetCore.SignalR;

namespace FoodStreet.Server.Hubs
{
    public static class NotificationHubEvents
    {
        public const string PoiUpdated = "poiUpdated";
        public const string MenuUpdated = "menuUpdated";
        public const string TranslationUpdated = "translationUpdated";
        public const string AudioReady = "audioReady";
        public const string TourPublished = "tourPublished";
        public const string QrScanned = "qrScanned";
        public const string ModerationChanged = "moderationChanged";
    }

    public static class NotificationHubGroups
    {
        public static string User(string userId) => $"user_{userId}";
        public static string Role(string role) => $"role_{role}";
        public static string Poi(int poiId) => $"poi_{poiId}";
    }

    public class RealtimeActivityMessage
    {
        public string EventName { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string? Status { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public string? TriggeredBy { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }

    public static class NotificationHubDispatchExtensions
    {
        public static Task SendRealtimeToRoleAsync(
            this IHubContext<NotificationHub> hubContext,
            string role,
            string eventName,
            RealtimeActivityMessage payload,
            CancellationToken cancellationToken = default)
        {
            payload.EventName = eventName;
            return hubContext.Clients.Group(NotificationHubGroups.Role(role))
                .SendAsync(eventName, payload, cancellationToken);
        }

        public static Task SendRealtimeToUserAsync(
            this IHubContext<NotificationHub> hubContext,
            string userId,
            string eventName,
            RealtimeActivityMessage payload,
            CancellationToken cancellationToken = default)
        {
            payload.EventName = eventName;
            return hubContext.Clients.Group(NotificationHubGroups.User(userId))
                .SendAsync(eventName, payload, cancellationToken);
        }

        public static Task SendRealtimeToPoiAsync(
            this IHubContext<NotificationHub> hubContext,
            int poiId,
            string eventName,
            RealtimeActivityMessage payload,
            CancellationToken cancellationToken = default)
        {
            payload.EventName = eventName;
            return hubContext.Clients.Group(NotificationHubGroups.Poi(poiId))
                .SendAsync(eventName, payload, cancellationToken);
        }
    }
}
