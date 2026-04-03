using FoodStreet.Server.Constants;
using FoodStreet.Server.Extensions;
using FoodStreet.Server.Links;
using FoodStreet.Server.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Data;
using PROJECT_C_.Models;

namespace PROJECT_C_.Controllers
{
    [ApiController]
    [Route("api/content/tours")]
    [Route("api/tours")]
    [Route("api/tour")]
    public class TourController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public TourController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("resume/latest")]
        [Authorize]
        public async Task<IActionResult> GetLatestResume()
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var latestSession = await _context.TourSessions
                .AsNoTracking()
                .Where(session => session.UserId == userId && !session.IsCompleted)
                .OrderByDescending(session => session.LastActivityAt)
                .FirstOrDefaultAsync();

            if (latestSession == null)
            {
                return NoContent();
            }

            var tour = await LoadTourAsync(latestSession.TourId, asNoTracking: true);
            if (tour == null || !tour.IsActive)
            {
                return NoContent();
            }

            var orderedItems = tour.Items.OrderBy(item => item.Order).ToList();
            var activeStop = orderedItems.FirstOrDefault(item => item.Order == latestSession.CurrentStopOrder) ?? orderedItems.FirstOrDefault();
            var resolvedName = activeStop?.Location == null
                ? "Điểm dừng đang chờ"
                : PoiContentResolver.Resolve(activeStop.Location, ResolveLanguage()).Name;

            return Ok(new
            {
                TourId = tour.Id,
                TourName = tour.Name,
                TourDescription = tour.Description,
                latestSession.SessionId,
                StopOrder = latestSession.CurrentStopOrder,
                CurrentStopName = resolvedName,
                latestSession.ProgressPercent,
                latestSession.CompletedStops,
                latestSession.TotalStops,
                UpdatedAt = latestSession.LastActivityAt
            });
        }

        /// <summary>
        /// Lấy danh sách tất cả Tours
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllTours([FromQuery] bool? activeOnly = null)
        {
            var query = _context.Tours.AsQueryable();
            if (activeOnly == true)
            {
                query = query.Where(t => t.IsActive);
            }

            var tours = await query
                .AsNoTracking()
                .Include(t => t.Items)
                    .ThenInclude(i => i.Location)
                        .ThenInclude(l => l!.Translations)
                .Include(t => t.Items)
                    .ThenInclude(i => i.Location)
                        .ThenInclude(l => l!.AudioFiles)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var languageCode = ResolveLanguage();

            var result = tours.Select(t => new
            {
                t.Id,
                t.Name,
                t.Description,
                t.EstimatedDurationMinutes,
                t.EstimatedDistanceKm,
                t.IsActive,
                t.CreatedAt,
                ItemCount = t.Items.Count,
                Items = t.Items
                    .OrderBy(i => i.Order)
                    .Select(i => MapTourStop(i, languageCode, includeDeepLink: false))
                    .ToList()
            });

            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết một Tour
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTour(int id)
        {
            var languageCode = ResolveLanguage();
            var tour = await LoadTourAsync(id, asNoTracking: true);

            if (tour == null)
            {
                return NotFound(new { message = "Tour không tồn tại" });
            }

            var result = new
            {
                tour.Id,
                tour.Name,
                tour.Description,
                tour.EstimatedDurationMinutes,
                tour.EstimatedDistanceKm,
                tour.IsActive,
                tour.CreatedAt,
                tour.UpdatedAt,
                Items = tour.Items
                    .OrderBy(i => i.Order)
                    .Select(i => MapTourStop(i, languageCode, includeDeepLink: false))
                    .ToList()
            };

            return Ok(result);
        }

        [HttpGet("{id}/sessions/{sessionId}")]
        [Authorize]
        public async Task<IActionResult> GetTourSession(int id, string sessionId)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var session = await _context.TourSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.TourId == id && item.SessionId == sessionId && item.UserId == userId);

            if (session == null)
            {
                return NotFound(new { message = "Phiên tour không tồn tại." });
            }

            var tour = await LoadTourAsync(id, asNoTracking: true);
            if (tour == null || !tour.IsActive)
            {
                return NotFound(new { message = "Tour không tồn tại hoặc chưa được kích hoạt" });
            }

            return Ok(BuildTourSessionResponse(tour, session, ResolveLanguage()));
        }

        [HttpPost("{id}/resume")]
        [Authorize]
        public async Task<IActionResult> ResumeTour(int id, [FromBody] ResumeTourRequest request)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var tour = await LoadTourAsync(id);
            if (tour == null || !tour.IsActive)
            {
                return NotFound(new { message = "Tour không tồn tại hoặc chưa được kích hoạt" });
            }

            var session = await _context.TourSessions
                .FirstOrDefaultAsync(item => item.TourId == id && item.SessionId == request.SessionId && item.UserId == userId);

            if (session == null)
            {
                return NotFound(new { message = "Phiên tour không tồn tại." });
            }

            if (!session.IsCompleted && ShouldTrackResume(session))
            {
                session.ResumeCount += 1;
                session.LastResumedAt = DateTime.UtcNow;
                session.LastActivityAt = DateTime.UtcNow;
                session.DeviceType = request.DeviceType ?? session.DeviceType;
                session.LastLatitude = request.Latitude ?? session.LastLatitude;
                session.LastLongitude = request.Longitude ?? session.LastLongitude;

                await _context.SaveChangesAsync();
                await LogTourEventAsync(
                    session.CurrentLocationId,
                    session.SessionId,
                    session.DeviceType,
                    session.LastLatitude,
                    session.LastLongitude,
                    PlaySources.TourResume);
            }

            return Ok(BuildTourSessionResponse(tour, session, ResolveLanguage()));
        }

        /// <summary>
        /// Bắt đầu tour cho du khách đã đăng nhập
        /// </summary>
        [HttpPost("{id}/start")]
        [Authorize]
        public async Task<IActionResult> StartTour(int id, [FromBody] StartTourRequest? request)
        {
            var tour = await LoadTourAsync(id);
            if (tour == null || !tour.IsActive)
            {
                return NotFound(new { message = "Tour không tồn tại hoặc chưa được kích hoạt" });
            }

            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var orderedItems = tour.Items.OrderBy(i => i.Order).ToList();
            if (!orderedItems.Any())
            {
                return BadRequest(new { message = "Tour chưa có điểm dừng" });
            }

            var sessionId = string.IsNullOrWhiteSpace(request?.SessionId)
                ? Guid.NewGuid().ToString("N")
                : request!.SessionId!;

            var currentStop = orderedItems[0];
            var nextStop = orderedItems.Skip(1).FirstOrDefault();
            var languageCode = ResolveLanguage();

            await LogTourEventAsync(
                currentStop.LocationId,
                sessionId,
                request?.DeviceType,
                request?.Latitude,
                request?.Longitude,
                PlaySources.TourStart);

            var persistedSession = await UpsertTourSessionAsync(
                tour.Id,
                userId,
                sessionId,
                currentStop.LocationId,
                currentStop.Order,
                completedStops: 0,
                totalStops: orderedItems.Count,
                progressPercent: 0,
                isCompleted: false,
                request?.DeviceType,
                request?.Latitude,
                request?.Longitude);

            return Ok(BuildTourSessionResponse(tour, persistedSession, languageCode));
        }

        /// <summary>
        /// Cập nhật tiến trình tour theo điểm dừng hiện tại
        /// </summary>
        [HttpPost("{id}/progress")]
        [Authorize]
        public async Task<IActionResult> UpdateTourProgress(int id, [FromBody] TourProgressRequest request)
        {
            var tour = await LoadTourAsync(id);
            if (tour == null || !tour.IsActive)
            {
                return NotFound(new { message = "Tour không tồn tại hoặc chưa được kích hoạt" });
            }

            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var persistedSession = await _context.TourSessions
                .FirstOrDefaultAsync(item => item.TourId == id && item.SessionId == request.SessionId && item.UserId == userId);

            if (persistedSession == null)
            {
                return NotFound(new { message = "Phiên tour không tồn tại." });
            }

            var orderedItems = tour.Items.OrderBy(i => i.Order).ToList();
            var currentStop = orderedItems.FirstOrDefault(i => i.LocationId == request.CurrentLocationId);
            if (currentStop == null)
            {
                return BadRequest(new { message = "Điểm dừng hiện tại không thuộc tour này" });
            }

            var nextStop = orderedItems.FirstOrDefault(i => i.Order == currentStop.Order + 1);
            var completedStops = currentStop.Order;
            var progressPercent = (int)Math.Round((double)completedStops / orderedItems.Count * 100, MidpointRounding.AwayFromZero);
            var languageCode = ResolveLanguage();
            var eventSource = nextStop == null ? PlaySources.TourComplete : PlaySources.TourProgress;

            await LogTourEventAsync(
                currentStop.LocationId,
                request.SessionId,
                request.DeviceType,
                request.Latitude,
                request.Longitude,
                eventSource);

            var activeStop = nextStop ?? currentStop;
            persistedSession.CurrentLocationId = activeStop.LocationId;
            persistedSession.CurrentStopOrder = activeStop.Order;
            persistedSession.CompletedStops = completedStops;
            persistedSession.TotalStops = orderedItems.Count;
            persistedSession.ProgressPercent = progressPercent;
            persistedSession.IsCompleted = nextStop == null;
            persistedSession.DeviceType = request.DeviceType ?? persistedSession.DeviceType;
            persistedSession.LastLatitude = request.Latitude;
            persistedSession.LastLongitude = request.Longitude;
            persistedSession.LastActivityAt = DateTime.UtcNow;
            persistedSession.DismissedAt = null;
            persistedSession.CompletedAt = nextStop == null ? DateTime.UtcNow : null;

            await _context.SaveChangesAsync();

            return Ok(BuildTourSessionResponse(tour, persistedSession, languageCode));
        }

        [HttpDelete("sessions/{sessionId}")]
        [Authorize]
        public async Task<IActionResult> DismissTourSession(string sessionId)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var session = await _context.TourSessions
                .FirstOrDefaultAsync(item => item.SessionId == sessionId && item.UserId == userId);

            if (session == null)
            {
                return NoContent();
            }

            if (session.IsCompleted && session.CompletedAt.HasValue)
            {
                return NoContent();
            }

            if (!session.IsCompleted)
            {
                await LogTourEventAsync(
                    session.CurrentLocationId,
                    session.SessionId,
                    session.DeviceType,
                    session.LastLatitude,
                    session.LastLongitude,
                    PlaySources.TourDismiss);
            }

            session.IsCompleted = true;
            session.DismissedAt = DateTime.UtcNow;
            session.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Tạo Tour mới
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateTour([FromBody] CreateTourRequest request)
        {
            var tour = new Tour
            {
                Name = request.Name,
                Description = request.Description,
                EstimatedDurationMinutes = request.EstimatedDurationMinutes,
                EstimatedDistanceKm = request.EstimatedDistanceKm,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();

            if (request.LocationIds != null && request.LocationIds.Any())
            {
                int order = 1;
                foreach (var locationId in request.LocationIds)
                {
                    _context.TourItems.Add(new TourItem
                    {
                        TourId = tour.Id,
                        LocationId = locationId,
                        Order = order++,
                        EstimatedStopMinutes = 15
                    });
                }

                await _context.SaveChangesAsync();
            }

            return Ok(new { id = tour.Id, message = "Tạo Tour thành công" });
        }

        /// <summary>
        /// Cập nhật Tour
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateTour(int id, [FromBody] UpdateTourRequest request)
        {
            var tour = await _context.Tours
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
            {
                return NotFound(new { message = "Tour không tồn tại" });
            }

            tour.Name = request.Name;
            tour.Description = request.Description;
            tour.EstimatedDurationMinutes = request.EstimatedDurationMinutes;
            tour.EstimatedDistanceKm = request.EstimatedDistanceKm;
            tour.IsActive = request.IsActive;
            tour.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật Tour thành công" });
        }

        /// <summary>
        /// Xóa Tour
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteTour(int id)
        {
            var tour = await _context.Tours
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
            {
                return NotFound(new { message = "Tour không tồn tại" });
            }

            _context.TourItems.RemoveRange(tour.Items);
            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa Tour thành công" });
        }

        /// <summary>
        /// Thêm điểm dừng vào Tour
        /// </summary>
        [HttpPost("{id}/items")]
        [Authorize]
        public async Task<IActionResult> AddTourItem(int id, [FromBody] AddTourItemRequest request)
        {
            var tour = await _context.Tours
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
            {
                return NotFound(new { message = "Tour không tồn tại" });
            }

            var location = await _context.Locations.FindAsync(request.LocationId);
            if (location == null)
            {
                return NotFound(new { message = "Địa điểm không tồn tại" });
            }

            var maxOrder = tour.Items.Any() ? tour.Items.Max(i => i.Order) : 0;

            var item = new TourItem
            {
                TourId = id,
                LocationId = request.LocationId,
                Order = maxOrder + 1,
                Note = request.Note,
                EstimatedStopMinutes = request.EstimatedStopMinutes
            };

            _context.TourItems.Add(item);
            await _context.SaveChangesAsync();

            return Ok(new { id = item.Id, message = "Thêm điểm dừng thành công" });
        }

        /// <summary>
        /// Xóa điểm dừng khỏi Tour
        /// </summary>
        [HttpDelete("{tourId}/items/{itemId}")]
        [Authorize]
        public async Task<IActionResult> RemoveTourItem(int tourId, int itemId)
        {
            var item = await _context.TourItems
                .FirstOrDefaultAsync(i => i.Id == itemId && i.TourId == tourId);

            if (item == null)
            {
                return NotFound(new { message = "Điểm dừng không tồn tại" });
            }

            _context.TourItems.Remove(item);
            await _context.SaveChangesAsync();

            var remainingItems = await _context.TourItems
                .Where(i => i.TourId == tourId)
                .OrderBy(i => i.Order)
                .ToListAsync();

            int newOrder = 1;
            foreach (var remaining in remainingItems)
            {
                remaining.Order = newOrder++;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa điểm dừng thành công" });
        }

        /// <summary>
        /// Cập nhật thứ tự các điểm dừng trong Tour
        /// </summary>
        [HttpPut("{id}/items/reorder")]
        [Authorize]
        public async Task<IActionResult> ReorderTourItems(int id, [FromBody] ReorderItemsRequest request)
        {
            var tour = await _context.Tours
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
            {
                return NotFound(new { message = "Tour không tồn tại" });
            }

            foreach (var itemOrder in request.ItemOrders)
            {
                var item = tour.Items.FirstOrDefault(i => i.Id == itemOrder.ItemId);
                if (item != null)
                {
                    item.Order = itemOrder.Order;
                }
            }

            tour.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thứ tự thành công" });
        }

        /// <summary>
        /// Toggle trạng thái Active của Tour
        /// </summary>
        [HttpPatch("{id}/toggle")]
        [Authorize]
        public async Task<IActionResult> ToggleTourActive(int id)
        {
            var tour = await _context.Tours.FindAsync(id);

            if (tour == null)
            {
                return NotFound(new { message = "Tour không tồn tại" });
            }

            tour.IsActive = !tour.IsActive;
            tour.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { isActive = tour.IsActive, message = tour.IsActive ? "Đã kích hoạt Tour" : "Đã tạm dừng Tour" });
        }

        private string ResolveLanguage()
        {
            return PoiContentResolver.NormalizeLanguageCode(Request.Headers["Accept-Language"].ToString());
        }

        private string ResolveBaseUrl()
        {
            var configuredBaseUrl = _configuration["App:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
            {
                return configuredBaseUrl;
            }

            return $"{Request.Scheme}://{Request.Host}";
        }

        private async Task<Tour?> LoadTourAsync(int id, bool asNoTracking = false)
        {
            var query = _context.Tours.AsQueryable();
            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            return await query
                .Include(t => t.Items)
                    .ThenInclude(i => i.Location)
                        .ThenInclude(l => l!.Translations)
                .Include(t => t.Items)
                    .ThenInclude(i => i.Location)
                        .ThenInclude(l => l!.AudioFiles)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        private object MapTourStop(
            TourItem item,
            string languageCode,
            bool includeDeepLink,
            int? tourId = null,
            string? sessionId = null,
            string source = PlaySources.Manual)
        {
            if (item.Location == null)
            {
                return new
                {
                    item.Id,
                    item.LocationId,
                    LocationName = "Unknown",
                    item.Order,
                    item.Note,
                    item.EstimatedStopMinutes
                };
            }

            var resolved = PoiContentResolver.Resolve(item.Location, languageCode);
            var deepLink = includeDeepLink
                ? PoiDeepLinkBuilder.Build(ResolveBaseUrl(), item.LocationId, source, tourId, item.Order, sessionId)
                : null;

            return new
            {
                item.Id,
                item.LocationId,
                LocationName = resolved.Name,
                LocationDescription = resolved.Description,
                LocationImageUrl = item.Location.ImageUrl,
                LocationLatitude = item.Location.Latitude,
                LocationLongitude = item.Location.Longitude,
                item.Order,
                item.Note,
                item.EstimatedStopMinutes,
                resolved.HasAudio,
                resolved.AudioUrl,
                resolved.AudioStatus,
                resolved.LanguageCode,
                resolved.Tier,
                resolved.FallbackUsed,
                resolved.IsFallback,
                DeepLink = deepLink
            };
        }

        private object BuildTourSessionResponse(Tour tour, TourSession session, string languageCode)
        {
            var orderedItems = tour.Items.OrderBy(item => item.Order).ToList();
            var activeStop = orderedItems.FirstOrDefault(item => item.Order == session.CurrentStopOrder) ?? orderedItems.FirstOrDefault();
            var nextStop = activeStop == null
                ? null
                : orderedItems.FirstOrDefault(item => item.Order == activeStop.Order + 1);

            return new
            {
                TourId = tour.Id,
                session.SessionId,
                TotalStops = session.TotalStops > 0 ? session.TotalStops : orderedItems.Count,
                session.CompletedStops,
                session.ProgressPercent,
                CurrentStop = activeStop == null
                    ? null
                    : MapTourStop(
                        activeStop,
                        languageCode,
                        includeDeepLink: true,
                        tourId: tour.Id,
                        sessionId: session.SessionId,
                        source: session.CompletedStops == 0 ? PlaySources.TourStart : PlaySources.TourProgress),
                NextStop = nextStop == null
                    ? null
                    : MapTourStop(
                        nextStop,
                        languageCode,
                        includeDeepLink: true,
                        tourId: tour.Id,
                        sessionId: session.SessionId,
                        source: PlaySources.TourProgress),
                session.IsCompleted
            };
        }

        private async Task<TourSession> UpsertTourSessionAsync(
            int tourId,
            string userId,
            string sessionId,
            int currentLocationId,
            int currentStopOrder,
            int completedStops,
            int totalStops,
            int progressPercent,
            bool isCompleted,
            string? deviceType,
            double? latitude,
            double? longitude)
        {
            var session = await _context.TourSessions
                .FirstOrDefaultAsync(item => item.SessionId == sessionId && item.UserId == userId);

            if (session == null)
            {
                session = new TourSession
                {
                    SessionId = sessionId,
                    UserId = userId,
                    TourId = tourId,
                    StartedAt = DateTime.UtcNow
                };
                _context.TourSessions.Add(session);
            }

            session.TourId = tourId;
            session.CurrentLocationId = currentLocationId;
            session.CurrentStopOrder = currentStopOrder;
            session.CompletedStops = completedStops;
            session.TotalStops = totalStops;
                session.ProgressPercent = progressPercent;
                session.IsCompleted = isCompleted;
                session.ResumeCount = 0;
                session.DeviceType = deviceType ?? session.DeviceType;
                session.LastLatitude = latitude;
                session.LastLongitude = longitude;
                session.LastResumedAt = null;
                session.DismissedAt = null;
                session.CompletedAt = isCompleted ? DateTime.UtcNow : null;
                session.LastActivityAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return session;
        }

        private static bool ShouldTrackResume(TourSession session)
        {
            if (session.IsCompleted)
            {
                return false;
            }

            if (!session.LastResumedAt.HasValue)
            {
                return true;
            }

            return session.LastResumedAt.Value <= DateTime.UtcNow.AddMinutes(-1);
        }

        private async Task LogTourEventAsync(
            int locationId,
            string sessionId,
            string? deviceType,
            double? latitude,
            double? longitude,
            string source)
        {
            _context.PlayLogs.Add(new PlayLog
            {
                LocationId = locationId,
                DurationSeconds = 0,
                SessionId = sessionId,
                DeviceType = deviceType ?? "unknown",
                Language = ResolveLanguage(),
                Latitude = latitude,
                Longitude = longitude,
                Source = PlaySources.Normalize(source),
                PlayedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
    }

    public class CreateTourRequest
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public int EstimatedDurationMinutes { get; set; } = 60;
        public double EstimatedDistanceKm { get; set; } = 1.0;
        public bool IsActive { get; set; } = true;
        public List<int>? LocationIds { get; set; }
    }

    public class UpdateTourRequest
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public int EstimatedDurationMinutes { get; set; }
        public double EstimatedDistanceKm { get; set; }
        public bool IsActive { get; set; }
    }

    public class AddTourItemRequest
    {
        public int LocationId { get; set; }
        public string? Note { get; set; }
        public int EstimatedStopMinutes { get; set; } = 15;
    }

    public class ReorderItemsRequest
    {
        public List<ItemOrderDto> ItemOrders { get; set; } = new();
    }

    public class StartTourRequest
    {
        public string? SessionId { get; set; }
        public string? DeviceType { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class TourProgressRequest
    {
        public required string SessionId { get; set; }
        public int CurrentLocationId { get; set; }
        public string? DeviceType { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class ResumeTourRequest
    {
        public required string SessionId { get; set; }
        public string? DeviceType { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class ItemOrderDto
    {
        public int ItemId { get; set; }
        public int Order { get; set; }
    }
}
