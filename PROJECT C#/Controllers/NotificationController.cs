using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Data;
using PROJECT_C_.Models;

namespace PROJECT_C_.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(AppDbContext context, ILogger<NotificationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Đăng ký Push Subscription
        /// </summary>
        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
        {
            // Check if already subscribed
            var existing = await _context.PushSubscriptions
                .FirstOrDefaultAsync(p => p.Endpoint == request.Endpoint);

            if (existing != null)
            {
                // Update existing subscription
                existing.P256dh = request.Keys.P256dh;
                existing.Auth = request.Keys.Auth;
                existing.IsActive = true;
                existing.SessionId = request.SessionId;
            }
            else
            {
                // Create new subscription
                var subscription = new PushSubscription
                {
                    SessionId = request.SessionId,
                    Endpoint = request.Endpoint,
                    P256dh = request.Keys.P256dh,
                    Auth = request.Keys.Auth,
                    PreferredLanguage = request.Language ?? "vi"
                };
                _context.PushSubscriptions.Add(subscription);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Push subscription registered for session: {SessionId}", request.SessionId);

            return Ok(new { success = true, message = "Đã đăng ký nhận thông báo" });
        }

        /// <summary>
        /// Hủy đăng ký Push Subscription
        /// </summary>
        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request)
        {
            var subscription = await _context.PushSubscriptions
                .FirstOrDefaultAsync(p => p.Endpoint == request.Endpoint);

            if (subscription != null)
            {
                subscription.IsActive = false;
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true, message = "Đã hủy đăng ký thông báo" });
        }

        /// <summary>
        /// Gửi thông báo test (Admin only)
        /// </summary>
        [HttpPost("send-test")]
        public async Task<IActionResult> SendTestNotification([FromBody] SendNotificationRequest request)
        {
            var subscriptions = await _context.PushSubscriptions
                .Where(p => p.IsActive)
                .ToListAsync();

            // Note: Trong production, sẽ dùng WebPush library để gửi thật
            // Ở đây chỉ trả về thông tin cho client tự xử lý
            
            _logger.LogInformation("Test notification sent to {Count} subscribers", subscriptions.Count);

            return Ok(new 
            { 
                success = true, 
                subscriberCount = subscriptions.Count,
                notification = new
                {
                    title = request.Title,
                    body = request.Body,
                    icon = request.Icon ?? "/favicon.png"
                }
            });
        }

        /// <summary>
        /// Lấy số lượng subscribers (Admin)
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var total = await _context.PushSubscriptions.CountAsync();
            var active = await _context.PushSubscriptions.CountAsync(p => p.IsActive);

            return Ok(new
            {
                totalSubscriptions = total,
                activeSubscriptions = active
            });
        }

        /// <summary>
        /// Trigger notification khi vào geofence (internal use)
        /// </summary>
        [HttpPost("geofence-trigger")]
        public async Task<IActionResult> GeofenceTrigger([FromBody] GeofenceTriggerRequest request)
        {
            var subscription = await _context.PushSubscriptions
                .FirstOrDefaultAsync(p => p.SessionId == request.SessionId && p.IsActive);

            if (subscription == null)
            {
                return Ok(new { triggered = false, reason = "No active subscription" });
            }

            // Return notification data for client to display
            return Ok(new
            {
                triggered = true,
                notification = new
                {
                    title = $"📍 {request.PoiName}",
                    body = request.PoiDescription ?? "Bạn đang ở gần địa điểm này!",
                    icon = request.PoiImageUrl ?? "/favicon.png",
                    tag = $"geofence-{request.PoiId}",
                    data = new
                    {
                        poiId = request.PoiId,
                        latitude = request.Latitude,
                        longitude = request.Longitude
                    }
                }
            });
        }
    }

    // Request DTOs
    public class SubscribeRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public PushKeys Keys { get; set; } = new();
        public string? Language { get; set; }
    }

    public class PushKeys
    {
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
    }

    public class UnsubscribeRequest
    {
        public string Endpoint { get; set; } = string.Empty;
    }

    public class SendNotificationRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? Icon { get; set; }
    }

    public class GeofenceTriggerRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public int PoiId { get; set; }
        public string PoiName { get; set; } = string.Empty;
        public string? PoiDescription { get; set; }
        public string? PoiImageUrl { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
