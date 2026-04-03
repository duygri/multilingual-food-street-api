using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodStreet.Server.Constants;
using PROJECT_C_.Data;
using PROJECT_C_.DTOs;
using PROJECT_C_.Models;

namespace PROJECT_C_.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    [ApiExplorerSettings(IgnoreApi = true)] // TEMPORARILY HIDDEN FROM SWAGGER FOR DEBUG
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(AppDbContext context, IWebHostEnvironment environment, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
        }

        [HttpPost("audio/upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAudio([FromForm] IFormFile file, [FromForm] int? locationId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            // Validate file type
            if (!file.ContentType.StartsWith("audio/"))
                return BadRequest("Only audio files are allowed.");

            // Prepare directory
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "audio");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Save metadata
            var audioFile = new AudioFile
            {
                FileName = uniqueFileName,
                OriginalName = file.FileName,
                ContentType = file.ContentType,
                Size = file.Length,
                DurationSeconds = 0, // Placeholder, usually requires FFmpeg or lib to parse
                LocationId = locationId
            };

            _context.AudioFiles.Add(audioFile);
            await _context.SaveChangesAsync();

            return Ok(new { audioFile.Id, audioFile.FileName });
        }

        [HttpPost("image/upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            // Validate file type
            if (!file.ContentType.StartsWith("image/"))
                return BadRequest("Only image files are allowed.");

            // Prepare directory
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"/images/{uniqueFileName}";
            return Ok(new { url = fileUrl });
        }

        [HttpGet("stats")]
        [AllowAnonymous] // Allow public access for now - dashboard needs this
        public async Task<IActionResult> GetStats()
        {
            var stats = new
            {
                TotalLocations = await _context.Locations.CountAsync(),
                TotalAudios = await _context.AudioFiles.CountAsync(),
                TotalUsers = await _context.Users.CountAsync()
            };
            return Ok(stats);
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<AdminDashboardDto>> GetDashboard()
        {
            if (!User.IsInRole(AppRoles.Admin))
            {
                return Forbid();
            }

            var now = DateTime.UtcNow;
            var since30Days = now.AddDays(-30);
            var adminUserId = User.Claims.FirstOrDefault(c =>
                c.Type == "sub" ||
                c.Type == System.Security.Claims.ClaimTypes.NameIdentifier ||
                c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            var locations = await _context.Locations
                .AsNoTracking()
                .Include(location => location.AudioFiles)
                .Include(location => location.Translations)
                .OrderByDescending(location => location.Id)
                .ToListAsync();

            var tours = await _context.Tours
                .AsNoTracking()
                .ToListAsync();

            var menuItems = await _context.PoiMenuItems
                .AsNoTracking()
                .Include(item => item.Translations)
                .ToListAsync();

            var playLogs = await _context.PlayLogs
                .AsNoTracking()
                .Where(log => log.PlayedAt >= since30Days)
                .OrderByDescending(log => log.PlayedAt)
                .ToListAsync();
            var totalTourStarts = CountDistinctSessions(playLogs, PlaySources.TourStart);
            var totalTourResumes = CountDistinctSessions(playLogs, PlaySources.TourResume);
            var totalTourCompletions = CountDistinctSessions(playLogs, PlaySources.TourComplete);
            var totalTourDismissals = CountDistinctSessions(playLogs, PlaySources.TourDismiss);
            var activeTourSessions = await _context.TourSessions
                .AsNoTracking()
                .CountAsync(session => !session.IsCompleted);

            var totalMenuItems = menuItems.Count;
            var availableMenuItems = menuItems.Count(item => item.IsAvailable);
            var totalPoiTranslations = locations.Sum(location =>
                LocalizationCoverageMetrics.CountAvailableLanguages(location.Translations.Select(translation => translation.LanguageCode)));
            var fullyLocalizedPois = locations.Count(location =>
                LocalizationCoverageMetrics.HasFullCoverage(location.Translations.Select(translation => translation.LanguageCode)));
            var totalMenuTranslations = menuItems.Sum(item =>
                LocalizationCoverageMetrics.CountAvailableLanguages(item.Translations.Select(translation => translation.LanguageCode)));
            var fullyLocalizedMenuItems = menuItems.Count(item =>
                LocalizationCoverageMetrics.HasFullCoverage(item.Translations.Select(translation => translation.LanguageCode)));

            var recentNotificationsQuery = _context.Notifications
                .AsNoTracking()
                .Where(notification => notification.TargetRole == AppRoles.Admin || notification.UserId == adminUserId)
                .OrderByDescending(notification => notification.CreatedAt);

            var recentNotifications = await recentNotificationsQuery
                .Take(6)
                .Select(notification => new AdminDashboardNotificationDto
                {
                    Id = notification.Id,
                    Title = notification.Title,
                    Message = notification.Message,
                    Type = notification.Type.ToString(),
                    CreatedAt = notification.CreatedAt,
                    SenderName = notification.SenderName,
                    RelatedId = notification.RelatedId
                })
                .ToListAsync();

            var unreadAdminNotifications = await recentNotificationsQuery
                .CountAsync(notification => !notification.IsRead);

            var poiOwnerRoleId = await _context.Roles
                .Where(role => role.Name == AppRoles.PoiOwner)
                .Select(role => role.Id)
                .FirstOrDefaultAsync();

            List<IdentityUser> poiOwners = [];
            if (!string.IsNullOrWhiteSpace(poiOwnerRoleId))
            {
                var poiOwnerIds = await _context.UserRoles
                    .Where(userRole => userRole.RoleId == poiOwnerRoleId)
                    .Select(userRole => userRole.UserId)
                    .ToListAsync();

                poiOwners = await _context.Users
                    .AsNoTracking()
                    .Where(user => poiOwnerIds.Contains(user.Id))
                    .ToListAsync();
            }

            var ownerLookup = poiOwners.ToDictionary(owner => owner.Id, owner => owner.Email ?? owner.UserName ?? "POI Owner");

            AdminDashboardPoiItemDto MapPoi(Location location) => new()
            {
                Id = location.Id,
                Name = location.Name,
                Address = location.Address,
                OwnerEmail = !string.IsNullOrWhiteSpace(location.OwnerId) && ownerLookup.TryGetValue(location.OwnerId, out var ownerEmail)
                    ? ownerEmail
                    : null,
                IsApproved = location.IsApproved,
                HasAudio = location.AudioFiles.Any() || PoiAudioStatuses.Normalize(location.AudioStatus, location.AudioFiles.Any()) == PoiAudioStatuses.Ready,
                AudioStatus = PoiAudioStatuses.Normalize(location.AudioStatus, location.AudioFiles.Any()),
                ApprovedAt = location.ApprovedAt
            };

            var dashboard = new AdminDashboardDto
            {
                TotalPois = locations.Count,
                ApprovedPois = locations.Count(location => location.IsApproved),
                PendingPois = locations.Count(location => !location.IsApproved),
                PoisWithAudio = locations.Count(location => location.AudioFiles.Any() || PoiAudioStatuses.Normalize(location.AudioStatus, location.AudioFiles.Any()) == PoiAudioStatuses.Ready),
                TotalPoiTranslations = totalPoiTranslations,
                FullyLocalizedPois = fullyLocalizedPois,
                PoiTranslationCoveragePercent = LocalizationCoverageMetrics.CalculateCoveragePercent(totalPoiTranslations, locations.Count),
                TotalMenuItems = totalMenuItems,
                AvailableMenuItems = availableMenuItems,
                TotalMenuTranslations = totalMenuTranslations,
                FullyLocalizedMenuItems = fullyLocalizedMenuItems,
                MenuTranslationCoveragePercent = LocalizationCoverageMetrics.CalculateCoveragePercent(totalMenuTranslations, totalMenuItems),
                TotalAudios = await _context.AudioFiles.CountAsync(),
                TotalTours = tours.Count,
                ActiveTours = tours.Count(tour => tour.IsActive),
                TotalUsers = await _context.Users.CountAsync(),
                ActivePoiOwners = poiOwners.Count(owner => owner.LockoutEnd == null || owner.LockoutEnd <= now),
                PendingPoiOwners = poiOwners.Count(owner => owner.LockoutEnd != null && owner.LockoutEnd > now),
                TotalPlays30Days = playLogs.Count,
                TodayPlays = playLogs.Count(log => log.PlayedAt >= now.Date),
                QrScans30Days = playLogs.Count(log => log.Source == PlaySources.QrScan),
                TourStarts30Days = totalTourStarts,
                TourResumes30Days = totalTourResumes,
                TourCompletions30Days = totalTourCompletions,
                TourDismissals30Days = totalTourDismissals,
                ActiveTourSessions = activeTourSessions,
                TourCompletionRate30Days = totalTourStarts > 0 ? Math.Round(totalTourCompletions * 100d / totalTourStarts, 1) : 0,
                TourDismissRate30Days = totalTourStarts > 0 ? Math.Round(totalTourDismissals * 100d / totalTourStarts, 1) : 0,
                UnreadAdminNotifications = unreadAdminNotifications,
                LastActivityAt = playLogs.Select(log => (DateTime?)log.PlayedAt).FirstOrDefault() ??
                                 recentNotifications.Select(notification => (DateTime?)notification.CreatedAt).FirstOrDefault(),
                PendingReviewPois = locations.Where(location => !location.IsApproved).Take(5).Select(MapPoi).ToList(),
                RecentPois = locations.Take(6).Select(MapPoi).ToList(),
                SourceBreakdown = playLogs
                    .GroupBy(log => string.IsNullOrWhiteSpace(log.Source) ? PlaySources.Manual : log.Source)
                    .OrderByDescending(group => group.Count())
                    .Select(group => new AdminDashboardSourceDto
                    {
                        Source = group.Key,
                        Count = group.Count()
                    })
                    .ToList(),
                RecentNotifications = recentNotifications
            };

            return Ok(dashboard);
        }

        private static int CountDistinctSessions(IEnumerable<PlayLog> logs, string source)
        {
            return logs
                .Where(log => log.Source == source && !string.IsNullOrWhiteSpace(log.SessionId))
                .Select(log => log.SessionId!)
                .Distinct()
                .Count();
        }
    }
}
