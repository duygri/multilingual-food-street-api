using FoodStreet.Server.Constants;
using FoodStreet.Server.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Data;
using PROJECT_C_.DTOs;
using PROJECT_C_.Models;
using System.Text;

namespace PROJECT_C_.Controllers
{
    /// <summary>
    /// Owner portal — POI owners manage their own profile, dashboard, and analytics.
    /// Route prefix: api/owner
    /// </summary>
    [ApiController]
    [Route("api/owner")]
    [Authorize(Roles = AppRoles.AdminOrPoiOwner)]
    public class OwnerController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OwnerController(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>Dashboard summary for the logged-in POI owner.</summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<OwnerDashboardDto>> GetDashboard()
        {
            var ownerContext = await GetCurrentOwnerAsync();
            if (ownerContext == null)
            {
                return Unauthorized();
            }

            var (ownerId, user) = ownerContext.Value;
            var locations = await GetOwnerLocationsAsync(ownerId);
            var menuSummary = GetOwnerMenuSummary(locations);
            var translationSummary = GetOwnerTranslationSummary(locations);
            var playLogs = await GetOwnerPlayLogsAsync(locations, 30);
            var unreadNotifications = await GetUnreadNotificationsAsync(ownerId);
            var poiStats = BuildPoiAnalytics(locations, playLogs);
            var activeTourSessions = await GetActiveOwnerTourSessionsAsync(locations);
            var totalTourStarts = CountDistinctSessions(playLogs, PlaySources.TourStart);
            var totalTourResumes = CountDistinctSessions(playLogs, PlaySources.TourResume);
            var totalTourCompletions = CountDistinctSessions(playLogs, PlaySources.TourComplete);
            var totalTourDismissals = CountDistinctSessions(playLogs, PlaySources.TourDismiss);

            var dashboard = new OwnerDashboardDto
            {
                OwnerId = ownerId,
                DisplayName = user.UserName ?? user.Email ?? "POI Owner",
                Email = user.Email,
                RoleDisplayName = AppRoles.ToDisplayName(AppRoles.PoiOwner),
                TotalPois = locations.Count,
                ApprovedPois = locations.Count(l => l.IsApproved),
                PendingPois = locations.Count(l => !l.IsApproved),
                PoisWithAudio = locations.Count(HasReadyAudio),
                TotalPoiTranslations = translationSummary.TotalPoiTranslations,
                FullyLocalizedPois = translationSummary.FullyLocalizedPois,
                PoiTranslationCoveragePercent = translationSummary.PoiTranslationCoveragePercent,
                TotalMenuItems = menuSummary.TotalMenuItems,
                AvailableMenuItems = menuSummary.AvailableMenuItems,
                TotalMenuTranslations = translationSummary.TotalMenuTranslations,
                FullyLocalizedMenuItems = translationSummary.FullyLocalizedMenuItems,
                MenuTranslationCoveragePercent = translationSummary.MenuTranslationCoveragePercent,
                TotalPlays30Days = playLogs.Count,
                QrScans30Days = playLogs.Count(log => log.Source == PlaySources.QrScan),
                TourStarts30Days = totalTourStarts,
                TourResumes30Days = totalTourResumes,
                TourProgressEvents30Days = playLogs.Count(log => log.Source == PlaySources.TourProgress),
                TourCompletions30Days = totalTourCompletions,
                TourDismissals30Days = totalTourDismissals,
                ActiveTourSessions = activeTourSessions,
                TourCompletionRate30Days = totalTourStarts > 0 ? Math.Round(totalTourCompletions * 100d / totalTourStarts, 1) : 0,
                TourDismissRate30Days = totalTourStarts > 0 ? Math.Round(totalTourDismissals * 100d / totalTourStarts, 1) : 0,
                UnreadNotifications = unreadNotifications,
                LastPlayedAt = playLogs.OrderByDescending(log => log.PlayedAt).Select(log => (DateTime?)log.PlayedAt).FirstOrDefault(),
                RecentPois = poiStats
                    .OrderByDescending(poi => poi.LastPlayedAt ?? poi.ApprovedAt ?? DateTime.MinValue)
                    .ThenByDescending(poi => poi.PlayCount)
                    .Take(5)
                    .ToList()
            };

            return Ok(dashboard);
        }

        /// <summary>Owner profile and account summary.</summary>
        [HttpGet("profile")]
        public async Task<ActionResult<OwnerProfileDto>> GetProfile()
        {
            var ownerContext = await GetCurrentOwnerAsync();
            if (ownerContext == null)
            {
                return Unauthorized();
            }

            var (ownerId, user) = ownerContext.Value;
            var locations = await GetOwnerLocationsAsync(ownerId);
            var menuSummary = GetOwnerMenuSummary(locations);
            var translationSummary = GetOwnerTranslationSummary(locations);

            var profile = new OwnerProfileDto
            {
                OwnerId = ownerId,
                DisplayName = user.UserName ?? user.Email ?? "POI Owner",
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                RoleDisplayName = AppRoles.ToDisplayName(AppRoles.PoiOwner),
                TotalPois = locations.Count,
                ApprovedPois = locations.Count(l => l.IsApproved),
                PendingPois = locations.Count(l => !l.IsApproved),
                PoisWithAudio = locations.Count(HasReadyAudio),
                TotalPoiTranslations = translationSummary.TotalPoiTranslations,
                FullyLocalizedPois = translationSummary.FullyLocalizedPois,
                PoiTranslationCoveragePercent = translationSummary.PoiTranslationCoveragePercent,
                TotalMenuItems = menuSummary.TotalMenuItems,
                AvailableMenuItems = menuSummary.AvailableMenuItems,
                TotalMenuTranslations = translationSummary.TotalMenuTranslations,
                FullyLocalizedMenuItems = translationSummary.FullyLocalizedMenuItems,
                MenuTranslationCoveragePercent = translationSummary.MenuTranslationCoveragePercent
            };

            return Ok(profile);
        }

        /// <summary>Update basic owner profile information.</summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateOwnerProfileDto request)
        {
            var ownerContext = await GetCurrentOwnerAsync();
            if (ownerContext == null)
            {
                return Unauthorized();
            }

            var (_, user) = ownerContext.Value;

            if (!string.IsNullOrWhiteSpace(request.DisplayName))
            {
                user.UserName = request.DisplayName.Trim();
            }

            if (request.Email != null)
            {
                user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
            }

            if (request.PhoneNumber != null)
            {
                user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Không thể cập nhật hồ sơ POI Owner.",
                    errors = result.Errors.Select(error => error.Description)
                });
            }

            return Ok(new
            {
                message = "Hồ sơ POI Owner đã được cập nhật.",
                displayName = user.UserName,
                email = user.Email,
                phoneNumber = user.PhoneNumber
            });
        }

        /// <summary>Owner analytics for their POIs.</summary>
        [HttpGet("analytics")]
        [HttpGet("stats")]
        public async Task<ActionResult<OwnerAnalyticsDto>> GetAnalytics([FromQuery] int days = 30, [FromQuery] int? locationId = null)
        {
            var ownerContext = await GetCurrentOwnerAsync();
            if (ownerContext == null)
            {
                return Unauthorized();
            }

            var (ownerId, _) = ownerContext.Value;
            var normalizedDays = Math.Clamp(days, 1, 365);
            var locations = await GetScopedOwnerLocationsAsync(ownerId, locationId);
            if (locations == null)
            {
                return NotFound(new { message = "Không tìm thấy POI thuộc quyền owner hiện tại." });
            }

            var playLogs = await GetOwnerPlayLogsAsync(locations, normalizedDays);
            var menuSummary = GetOwnerMenuSummary(locations);
            var translationSummary = GetOwnerTranslationSummary(locations);
            var poiStats = BuildPoiAnalytics(locations, playLogs);
            var activeTourSessions = await GetActiveOwnerTourSessionsAsync(locations);
            var totalTourStarts = CountDistinctSessions(playLogs, PlaySources.TourStart);
            var totalTourResumes = CountDistinctSessions(playLogs, PlaySources.TourResume);
            var totalTourCompletions = CountDistinctSessions(playLogs, PlaySources.TourComplete);
            var totalTourDismissals = CountDistinctSessions(playLogs, PlaySources.TourDismiss);

            var analytics = new OwnerAnalyticsDto
            {
                Days = normalizedDays,
                TotalPois = locations.Count,
                ApprovedPois = locations.Count(l => l.IsApproved),
                PendingPois = locations.Count(l => !l.IsApproved),
                PoisWithAudio = locations.Count(HasReadyAudio),
                TotalPoiTranslations = translationSummary.TotalPoiTranslations,
                FullyLocalizedPois = translationSummary.FullyLocalizedPois,
                PoiTranslationCoveragePercent = translationSummary.PoiTranslationCoveragePercent,
                TotalMenuItems = menuSummary.TotalMenuItems,
                AvailableMenuItems = menuSummary.AvailableMenuItems,
                TotalMenuTranslations = translationSummary.TotalMenuTranslations,
                FullyLocalizedMenuItems = translationSummary.FullyLocalizedMenuItems,
                MenuTranslationCoveragePercent = translationSummary.MenuTranslationCoveragePercent,
                TotalPlays = playLogs.Count,
                TotalQrScans = playLogs.Count(log => log.Source == PlaySources.QrScan),
                TotalTourStarts = totalTourStarts,
                TotalTourResumes = totalTourResumes,
                TotalTourProgressEvents = playLogs.Count(log => log.Source == PlaySources.TourProgress),
                TotalTourCompletions = totalTourCompletions,
                TotalTourDismissals = totalTourDismissals,
                ActiveTourSessions = activeTourSessions,
                TourCompletionRate = totalTourStarts > 0 ? Math.Round(totalTourCompletions * 100d / totalTourStarts, 1) : 0,
                TourDismissRate = totalTourStarts > 0 ? Math.Round(totalTourDismissals * 100d / totalTourStarts, 1) : 0,
                AvgListenDurationSeconds = playLogs.Count == 0 ? 0 : Math.Round(playLogs.Average(log => log.DurationSeconds), 2),
                LastPlayedAt = playLogs.OrderByDescending(log => log.PlayedAt).Select(log => (DateTime?)log.PlayedAt).FirstOrDefault(),
                Pois = poiStats.OrderByDescending(poi => poi.PlayCount).ThenBy(poi => poi.Name).ToList(),
                Sources = playLogs
                    .GroupBy(log => string.IsNullOrWhiteSpace(log.Source) ? PlaySources.Manual : log.Source)
                    .OrderByDescending(group => group.Count())
                    .Select(group => new OwnerBreakdownDto
                    {
                        Label = group.Key,
                        Count = group.Count()
                    })
                    .ToList(),
                Languages = playLogs
                    .Where(log => !string.IsNullOrWhiteSpace(log.Language))
                    .GroupBy(log => log.Language!)
                    .OrderByDescending(group => group.Count())
                    .Select(group => new OwnerBreakdownDto
                    {
                        Label = group.Key,
                        Count = group.Count()
                    })
                    .ToList()
            };

            return Ok(analytics);
        }

        /// <summary>Export owner analytics summary as CSV.</summary>
        [HttpGet("analytics/export")]
        public async Task<IActionResult> ExportAnalytics([FromQuery] int days = 30, [FromQuery] int? locationId = null)
        {
            var ownerContext = await GetCurrentOwnerAsync();
            if (ownerContext == null)
            {
                return Unauthorized();
            }

            var (ownerId, user) = ownerContext.Value;
            var normalizedDays = Math.Clamp(days, 1, 365);
            var locations = await GetScopedOwnerLocationsAsync(ownerId, locationId);
            if (locations == null)
            {
                return NotFound(new { message = "Không tìm thấy POI thuộc quyền owner hiện tại." });
            }

            var playLogs = await GetOwnerPlayLogsAsync(locations, normalizedDays);
            var poiStats = BuildPoiAnalytics(locations, playLogs);
            var activeTourSessions = await GetActiveOwnerTourSessionsAsync(locations);
            var totalTourStarts = CountDistinctSessions(playLogs, PlaySources.TourStart);
            var totalTourResumes = CountDistinctSessions(playLogs, PlaySources.TourResume);
            var totalTourCompletions = CountDistinctSessions(playLogs, PlaySources.TourComplete);
            var totalTourDismissals = CountDistinctSessions(playLogs, PlaySources.TourDismiss);
            var csv = new StringBuilder();
            csv.AppendLine("Section,Field,Value");
            csv.AppendLine($"Meta,GeneratedAtUtc,{EscapeCsv(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))}");
            csv.AppendLine($"Meta,Owner,{EscapeCsv(user.UserName ?? user.Email ?? "POI Owner")}");
            csv.AppendLine($"Meta,Days,{normalizedDays}");
            csv.AppendLine($"Meta,LocationScope,{EscapeCsv(locationId.HasValue && locationId.Value > 0 ? locations.First().Name : "All Owner POIs")}");
            csv.AppendLine($"Summary,TotalPlays,{playLogs.Count}");
            csv.AppendLine($"Summary,TotalQrScans,{playLogs.Count(log => log.Source == PlaySources.QrScan)}");
            csv.AppendLine($"Summary,TotalTourStarts,{totalTourStarts}");
            csv.AppendLine($"Summary,AvgDurationSeconds,{(playLogs.Count > 0 ? playLogs.Average(log => log.DurationSeconds) : 0):0.##}");
            csv.AppendLine($"Journey,ActiveTourSessions,{activeTourSessions}");
            csv.AppendLine($"Journey,TotalTourResumes,{totalTourResumes}");
            csv.AppendLine($"Journey,TotalTourProgressEvents,{playLogs.Count(log => log.Source == PlaySources.TourProgress)}");
            csv.AppendLine($"Journey,TotalTourCompletions,{totalTourCompletions}");
            csv.AppendLine($"Journey,TotalTourDismissals,{totalTourDismissals}");
            csv.AppendLine($"Journey,TourCompletionRate,{(totalTourStarts > 0 ? Math.Round(totalTourCompletions * 100d / totalTourStarts, 1) : 0):0.#}%");
            csv.AppendLine($"Journey,TourDismissRate,{(totalTourStarts > 0 ? Math.Round(totalTourDismissals * 100d / totalTourStarts, 1) : 0):0.#}%");

            foreach (var source in playLogs
                         .GroupBy(log => string.IsNullOrWhiteSpace(log.Source) ? PlaySources.Manual : log.Source!)
                         .OrderByDescending(group => group.Count()))
            {
                csv.AppendLine($"Sources,{EscapeCsv(source.Key)},{source.Count()}");
            }

            foreach (var language in playLogs
                         .Where(log => !string.IsNullOrWhiteSpace(log.Language))
                         .GroupBy(log => log.Language!)
                         .OrderByDescending(group => group.Count()))
            {
                csv.AppendLine($"Languages,{EscapeCsv(language.Key)},{language.Count()}");
            }

            foreach (var poi in poiStats.OrderByDescending(item => item.PlayCount).ThenBy(item => item.Name))
            {
                csv.AppendLine($"POIs,{EscapeCsv(poi.Name)},{poi.PlayCount}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"owner-analytics-summary-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
        }

        private async Task<(string OwnerId, IdentityUser User)?> GetCurrentOwnerAsync()
        {
            var ownerId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                return null;
            }

            var user = await _userManager.FindByIdAsync(ownerId);
            if (user == null)
            {
                return null;
            }

            return (ownerId, user);
        }

        private async Task<List<Location>> GetOwnerLocationsAsync(string ownerId)
        {
            return await _context.Locations
                .AsNoTracking()
                .Include(location => location.AudioFiles)
                .Include(location => location.Translations)
                .Include(location => location.Category)
                .Include(location => location.MenuItems)
                .ThenInclude(menuItem => menuItem.Translations)
                .AsSplitQuery()
                .Where(location => location.OwnerId == ownerId)
                .OrderByDescending(location => location.IsApproved)
                .ThenByDescending(location => location.ApprovedAt ?? DateTime.MinValue)
                .ThenByDescending(location => location.Id)
                .ToListAsync();
        }

        private async Task<List<Location>?> GetScopedOwnerLocationsAsync(string ownerId, int? locationId)
        {
            var locations = await GetOwnerLocationsAsync(ownerId);
            if (locationId.HasValue && locationId.Value > 0)
            {
                locations = locations.Where(location => location.Id == locationId.Value).ToList();
                if (locations.Count == 0)
                {
                    return null;
                }
            }

            return locations;
        }

        private async Task<List<PlayLog>> GetOwnerPlayLogsAsync(List<Location> locations, int days)
        {
            if (locations.Count == 0)
            {
                return [];
            }

            var locationIds = locations.Select(location => location.Id).ToList();
            var since = DateTime.UtcNow.AddDays(-days);

            return await _context.PlayLogs
                .AsNoTracking()
                .Where(log => locationIds.Contains(log.LocationId) && log.PlayedAt >= since)
                .OrderByDescending(log => log.PlayedAt)
                .ToListAsync();
        }

        private async Task<int> GetUnreadNotificationsAsync(string ownerId)
        {
            return await _context.Notifications
                .CountAsync(notification =>
                    (notification.UserId == ownerId || notification.TargetRole == AppRoles.PoiOwner)
                    && !notification.IsRead);
        }

        private async Task<int> GetActiveOwnerTourSessionsAsync(List<Location> locations)
        {
            if (locations.Count == 0)
            {
                return 0;
            }

            var locationIds = locations.Select(location => location.Id).ToList();
            return await _context.TourSessions
                .AsNoTracking()
                .CountAsync(session => !session.IsCompleted && locationIds.Contains(session.CurrentLocationId));
        }

        private static (int TotalMenuItems, int AvailableMenuItems) GetOwnerMenuSummary(List<Location> locations)
        {
            if (locations.Count == 0)
            {
                return (0, 0);
            }

            var menuItems = locations.SelectMany(location => location.MenuItems).ToList();
            return (menuItems.Count, menuItems.Count(item => item.IsAvailable));
        }

        private static List<OwnerPoiAnalyticsItemDto> BuildPoiAnalytics(IEnumerable<Location> locations, IEnumerable<PlayLog> playLogs)
        {
            var playLogsByLocation = playLogs
                .GroupBy(log => log.LocationId)
                .ToDictionary(group => group.Key, group => group.OrderByDescending(log => log.PlayedAt).ToList());

            return locations.Select(location =>
            {
                playLogsByLocation.TryGetValue(location.Id, out var logs);
                logs ??= [];

                var topLanguage = logs
                    .Where(log => !string.IsNullOrWhiteSpace(log.Language))
                    .GroupBy(log => log.Language!)
                    .OrderByDescending(group => group.Count())
                    .ThenBy(group => group.Key)
                    .Select(group => group.Key)
                    .FirstOrDefault();

                var translationCount = LocalizationCoverageMetrics.CountAvailableLanguages(
                    location.Translations.Select(translation => translation.LanguageCode));
                var menuItems = location.MenuItems ?? [];
                var menuTranslationCount = menuItems.Sum(item =>
                    LocalizationCoverageMetrics.CountAvailableLanguages(item.Translations.Select(translation => translation.LanguageCode)));
                var fullyLocalizedMenuItems = menuItems.Count(item =>
                    LocalizationCoverageMetrics.HasFullCoverage(item.Translations.Select(translation => translation.LanguageCode)));

                return new OwnerPoiAnalyticsItemDto
                {
                    LocationId = location.Id,
                    Name = location.Name,
                    Address = location.Address,
                    ImageUrl = location.ImageUrl,
                    CategoryName = location.Category?.Name,
                    IsApproved = location.IsApproved,
                    HasAudio = HasReadyAudio(location),
                    AudioStatus = PoiAudioStatuses.Normalize(location.AudioStatus, location.AudioFiles.Any()),
                    TranslationCount = translationCount,
                    TranslationCoveragePercent = LocalizationCoverageMetrics.CalculateCoveragePercent(translationCount, 1),
                    MenuItemCount = menuItems.Count,
                    MenuTranslationCount = menuTranslationCount,
                    FullyLocalizedMenuItems = fullyLocalizedMenuItems,
                    MenuTranslationCoveragePercent = LocalizationCoverageMetrics.CalculateCoveragePercent(menuTranslationCount, menuItems.Count),
                    PlayCount = logs.Count,
                    QrScanCount = logs.Count(log => log.Source == PlaySources.QrScan),
                    TourStartCount = CountDistinctSessions(logs, PlaySources.TourStart),
                    TourResumeCount = CountDistinctSessions(logs, PlaySources.TourResume),
                    TourProgressCount = logs.Count(log => log.Source == PlaySources.TourProgress),
                    TourCompletionCount = CountDistinctSessions(logs, PlaySources.TourComplete),
                    TourDismissCount = CountDistinctSessions(logs, PlaySources.TourDismiss),
                    TourCompletionRate = CountDistinctSessions(logs, PlaySources.TourStart) > 0
                        ? Math.Round(CountDistinctSessions(logs, PlaySources.TourComplete) * 100d / CountDistinctSessions(logs, PlaySources.TourStart), 1)
                        : 0,
                    AvgListenDurationSeconds = logs.Count == 0 ? 0 : Math.Round(logs.Average(log => log.DurationSeconds), 2),
                    TopLanguage = topLanguage,
                    LastSource = logs.FirstOrDefault()?.Source,
                    LastPlayedAt = logs.FirstOrDefault()?.PlayedAt,
                    ApprovedAt = location.ApprovedAt
                };
            }).ToList();
        }

        private static bool HasReadyAudio(Location location)
        {
            return location.AudioFiles.Any() || PoiAudioStatuses.Normalize(location.AudioStatus, location.AudioFiles.Any()) == PoiAudioStatuses.Ready;
        }

        private static OwnerTranslationSummary GetOwnerTranslationSummary(List<Location> locations)
        {
            if (locations.Count == 0)
            {
                return OwnerTranslationSummary.Empty;
            }

            var menuItems = locations.SelectMany(location => location.MenuItems).ToList();
            var totalPoiTranslations = locations.Sum(location =>
                LocalizationCoverageMetrics.CountAvailableLanguages(location.Translations.Select(translation => translation.LanguageCode)));
            var fullyLocalizedPois = locations.Count(location =>
                LocalizationCoverageMetrics.HasFullCoverage(location.Translations.Select(translation => translation.LanguageCode)));
            var totalMenuTranslations = menuItems.Sum(item =>
                LocalizationCoverageMetrics.CountAvailableLanguages(item.Translations.Select(translation => translation.LanguageCode)));
            var fullyLocalizedMenuItems = menuItems.Count(item =>
                LocalizationCoverageMetrics.HasFullCoverage(item.Translations.Select(translation => translation.LanguageCode)));

            return new OwnerTranslationSummary(
                totalPoiTranslations,
                fullyLocalizedPois,
                LocalizationCoverageMetrics.CalculateCoveragePercent(totalPoiTranslations, locations.Count),
                totalMenuTranslations,
                fullyLocalizedMenuItems,
                LocalizationCoverageMetrics.CalculateCoveragePercent(totalMenuTranslations, menuItems.Count));
        }

        private sealed record OwnerTranslationSummary(
            int TotalPoiTranslations,
            int FullyLocalizedPois,
            double PoiTranslationCoveragePercent,
            int TotalMenuTranslations,
            int FullyLocalizedMenuItems,
            double MenuTranslationCoveragePercent)
        {
            public static OwnerTranslationSummary Empty { get; } = new(0, 0, 0, 0, 0, 0);
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
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
