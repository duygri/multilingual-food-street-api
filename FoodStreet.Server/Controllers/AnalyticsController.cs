using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodStreet.Server.Constants;
using PROJECT_C_.Data;
using PROJECT_C_.Models;
using System.Text;

namespace PROJECT_C_.Controllers
{
    [ApiController]
    [Route("api/admin/analytics")]
    [Route("api/analytics")]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private static readonly string[] TourLifecycleSources =
        [
            PlaySources.TourStart,
            PlaySources.TourResume,
            PlaySources.TourProgress,
            PlaySources.TourDismiss,
            PlaySources.TourComplete
        ];

        public AnalyticsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Log a play event (called by mobile app or web player)
        /// </summary>
        [HttpPost("play")]
        public async Task<IActionResult> LogPlay([FromBody] LogPlayRequest request)
        {
            var playLog = new PlayLog
            {
                LocationId = request.LocationId,
                DurationSeconds = request.DurationSeconds,
                SessionId = request.SessionId,
                DeviceType = request.DeviceType ?? "unknown",
                Language = Request.Headers["Accept-Language"].ToString().Split(',').FirstOrDefault() ?? "vi-VN",
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Source = PlaySources.Normalize(request.Source),
                PlayedAt = DateTime.UtcNow
            };

            _context.PlayLogs.Add(playLog);
            await _context.SaveChangesAsync();

            return Ok(new { id = playLog.Id });
        }

        /// <summary>
        /// Get overall statistics
        /// </summary>
        [HttpGet("stats")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetStats([FromQuery] int days = 30, [FromQuery] int? locationId = null)
        {
            var today = DateTime.UtcNow.Date;
            var thisWeek = today.AddDays(-7);
            var thisMonth = today.AddDays(-30);
            var normalizedDays = Math.Clamp(days, 1, 365);
            var since = DateTime.UtcNow.AddDays(-normalizedDays);
            var playLogs = QueryPlayLogs(locationId: locationId);
            var periodPlayLogs = QueryPlayLogs(since, locationId);
            var tourEventLogs = QueryTourEventLogs(since, locationId);
            var locations = QueryLocations(locationId);
            var activeTourSessions = QueryTourSessions(locationId).Where(session => !session.IsCompleted);
            var tourStartsInWindow = await CountDistinctSessionsAsync(tourEventLogs, PlaySources.TourStart);
            var tourResumesInWindow = await CountDistinctSessionsAsync(tourEventLogs, PlaySources.TourResume);
            var tourCompletionsInWindow = await CountDistinctSessionsAsync(tourEventLogs, PlaySources.TourComplete);
            var tourDismissalsInWindow = await CountDistinctSessionsAsync(tourEventLogs, PlaySources.TourDismiss);
            var tourProgressEventsInWindow = await tourEventLogs.CountAsync(log => log.Source == PlaySources.TourProgress);

            var stats = new
            {
                TotalPlays = await playLogs.CountAsync(),
                TodayPlays = await playLogs.CountAsync(p => p.PlayedAt >= today),
                WeekPlays = await playLogs.CountAsync(p => p.PlayedAt >= thisWeek),
                MonthPlays = await playLogs.CountAsync(p => p.PlayedAt >= thisMonth),
                UniqueSessions = await playLogs.Select(p => p.SessionId).Distinct().CountAsync(),
                TotalPOIs = await locations.CountAsync(),
                POIsWithAudio = await locations.CountAsync(f => f.AudioFiles.Any()),
                PeriodDays = normalizedDays,
                PeriodPlays = await periodPlayLogs.CountAsync(),
                ActiveTourSessions = await activeTourSessions.CountAsync(),
                TourStartsInWindow = tourStartsInWindow,
                TourResumesInWindow = tourResumesInWindow,
                TourProgressEventsInWindow = tourProgressEventsInWindow,
                TourCompletionsInWindow = tourCompletionsInWindow,
                TourDismissalsInWindow = tourDismissalsInWindow,
                TourCompletionRate = tourStartsInWindow > 0
                    ? Math.Round(tourCompletionsInWindow * 100d / tourStartsInWindow, 1)
                    : 0,
                TourDismissRate = tourStartsInWindow > 0
                    ? Math.Round(tourDismissalsInWindow * 100d / tourStartsInWindow, 1)
                    : 0
            };

            return Ok(stats);
        }

        /// <summary>
        /// Get top played POIs
        /// </summary>
        [HttpGet("top-pois")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetTopPOIs([FromQuery] int limit = 10, [FromQuery] int days = 30, [FromQuery] int? locationId = null)
        {
            var since = DateTime.UtcNow.AddDays(-days);

            var topPOIs = await QueryPlayLogs(since, locationId)
                .GroupBy(p => p.LocationId)
                .Select(g => new
                {
                    LocationId = g.Key,
                    PlayCount = g.Count(),
                    TotalDuration = g.Sum(p => p.DurationSeconds),
                    AvgDuration = g.Average(p => p.DurationSeconds)
                })
                .OrderByDescending(x => x.PlayCount)
                .Take(limit)
                .ToListAsync();

            // Get food details
            var locationIds = topPOIs.Select(t => t.LocationId).ToList();
            var locations = await _context.Locations
                .Where(f => locationIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id);

            var result = topPOIs.Select(t => new
            {
                t.LocationId,
                LocationName = locations.ContainsKey(t.LocationId) ? locations[t.LocationId].Name : "Unknown",
                t.PlayCount,
                t.TotalDuration,
                t.AvgDuration
            });

            return Ok(result);
        }

        /// <summary>
        /// Get plays over time (for chart)
        /// </summary>
        [HttpGet("timeline")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetTimeline([FromQuery] int days = 7, [FromQuery] int? locationId = null)
        {
            var since = DateTime.UtcNow.Date.AddDays(-days);

            var timeline = await QueryPlayLogs(since, locationId)
                .GroupBy(p => p.PlayedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return Ok(timeline);
        }

        /// <summary>
        /// Get device breakdown
        /// </summary>
        [HttpGet("devices")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetDeviceStats([FromQuery] int days = 30, [FromQuery] int? locationId = null)
        {
            var since = DateTime.UtcNow.AddDays(-Math.Clamp(days, 1, 365));
            var devices = await QueryPlayLogs(since, locationId)
                .GroupBy(p => p.DeviceType ?? "unknown")
                .Select(g => new
                {
                    Device = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            return Ok(devices);
        }

        /// <summary>
        /// Get source breakdown (qr_scan, geofence, manual)
        /// </summary>
        [HttpGet("sources")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetSourceStats([FromQuery] int days = 30, [FromQuery] int? locationId = null)
        {
            var since = DateTime.UtcNow.AddDays(-Math.Clamp(days, 1, 365));
            var sources = await QueryPlayLogs(since, locationId)
                .GroupBy(p => p.Source)
                .Select(g => new
                {
                    Source = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            return Ok(sources);
        }

        /// <summary>
        /// Get language breakdown for plays
        /// </summary>
        [HttpGet("languages")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetLanguageStats([FromQuery] int days = 30, [FromQuery] int? locationId = null)
        {
            var since = DateTime.UtcNow.AddDays(-Math.Clamp(days, 1, 365));

            var languages = await QueryPlayLogs(since, locationId)
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Language) ? "unknown" : p.Language!)
                .Select(g => new
                {
                    Language = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(item => item.Count)
                .ToListAsync();

            return Ok(languages);
        }

        /// <summary>
        /// Get recent plays
        /// </summary>
        [HttpGet("recent")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetRecentPlays([FromQuery] int limit = 50, [FromQuery] int days = 30, [FromQuery] int? locationId = null)
        {
            var since = DateTime.UtcNow.AddDays(-Math.Clamp(days, 1, 365));
            var recentPlays = await QueryPlayLogs(since, locationId)
                .Include(p => p.Location)
                .OrderByDescending(p => p.PlayedAt)
                .Take(limit)
                .Select(p => new
                {
                    p.Id,
                    p.LocationId,
                    LocationName = p.Location != null ? p.Location.Name : "Unknown",
                    p.PlayedAt,
                    p.DurationSeconds,
                    p.Source,
                    p.DeviceType,
                    p.Language
                })
                .ToListAsync();

            return Ok(recentPlays);
        }

        /// <summary>
        /// Timeline breakdown by source (manual, QR, geofence, tour)
        /// </summary>
        [HttpGet("timeline-sources")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetTimelineBySources([FromQuery] int days = 30, [FromQuery] int? locationId = null)
        {
            var normalizedDays = Math.Clamp(days, 1, 365);
            var since = DateTime.UtcNow.Date.AddDays(-normalizedDays);

            var rows = await QueryPlayLogs(since, locationId)
                .GroupBy(p => p.PlayedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Manual = g.Count(p => (p.Source ?? PlaySources.Manual) == PlaySources.Manual),
                    QrScan = g.Count(p => p.Source == PlaySources.QrScan),
                    Geofence = g.Count(p => p.Source == PlaySources.Geofence),
                    TourStart = g.Count(p => p.Source == PlaySources.TourStart),
                    TourResume = g.Count(p => p.Source == PlaySources.TourResume),
                    TourProgress = g.Count(p => p.Source == PlaySources.TourProgress),
                    TourComplete = g.Count(p => p.Source == PlaySources.TourComplete),
                    TourDismiss = g.Count(p => p.Source == PlaySources.TourDismiss)
                })
                .OrderBy(item => item.Date)
                .ToListAsync();

            return Ok(rows);
        }

        /// <summary>
        /// Get detailed play history with pagination and filters
        /// </summary>
        [HttpGet("history")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? deviceType = null,
            [FromQuery] string? source = null,
            [FromQuery] string? language = null,
            [FromQuery] int? locationId = null)
        {
            var query = _context.PlayLogs.Include(p => p.Location).AsQueryable();

            // Apply filters
            if (fromDate.HasValue)
                query = query.Where(p => p.PlayedAt >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(p => p.PlayedAt <= toDate.Value.AddDays(1));
            if (!string.IsNullOrEmpty(deviceType))
                query = query.Where(p => p.DeviceType == deviceType);
            if (!string.IsNullOrEmpty(source))
                query = query.Where(p => p.Source == source);
            if (!string.IsNullOrEmpty(language))
                query = query.Where(p => p.Language == language);
            if (locationId.HasValue)
                query = query.Where(p => p.LocationId == locationId.Value);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await query
                .OrderByDescending(p => p.PlayedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.LocationId,
                    LocationName = p.Location != null ? p.Location.Name : "Unknown",
                    p.PlayedAt,
                    p.DurationSeconds,
                    p.Source,
                    p.DeviceType,
                    p.Language,
                    p.SessionId,
                    p.Latitude,
                    p.Longitude
                })
                .ToListAsync();

            return Ok(new
            {
                items,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalCount,
                    totalPages
                }
            });
        }

        /// <summary>
        /// Export history to CSV
        /// </summary>
        [HttpGet("export")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> ExportHistory(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? deviceType = null,
            [FromQuery] string? source = null,
            [FromQuery] string? language = null,
            [FromQuery] int? locationId = null)
        {
            var query = _context.PlayLogs.Include(p => p.Location).AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(p => p.PlayedAt >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(p => p.PlayedAt <= toDate.Value.AddDays(1));
            if (!string.IsNullOrEmpty(deviceType))
                query = query.Where(p => p.DeviceType == deviceType);
            if (!string.IsNullOrEmpty(source))
                query = query.Where(p => p.Source == source);
            if (!string.IsNullOrEmpty(language))
                query = query.Where(p => p.Language == language);
            if (locationId.HasValue)
                query = query.Where(p => p.LocationId == locationId.Value);

            var data = await query
                .OrderByDescending(p => p.PlayedAt)
                .Select(p => new
                {
                    p.Id,
                    LocationName = p.Location != null ? p.Location.Name : "Unknown",
                    PlayedAt = p.PlayedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    p.DurationSeconds,
                    p.Source,
                    p.DeviceType,
                    p.Language,
                    p.SessionId
                })
                .ToListAsync();

            // Build CSV
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("ID,Địa điểm,Thời gian,Thời lượng (s),Nguồn,Thiết bị,Ngôn ngữ,Session");
            foreach (var row in data)
            {
                csv.AppendLine($"{row.Id},\"{row.LocationName}\",{row.PlayedAt},{row.DurationSeconds},{row.Source},{row.DeviceType},{row.Language},{row.SessionId}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"usage-history-{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        /// <summary>
        /// Export analytics summary as CSV for the current filter scope.
        /// </summary>
        [HttpGet("export-summary")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> ExportAnalyticsSummary([FromQuery] int days = 30, [FromQuery] int? locationId = null)
        {
            var normalizedDays = Math.Clamp(days, 1, 365);
            var since = DateTime.UtcNow.AddDays(-normalizedDays);
            var playLogs = await QueryPlayLogs(since, locationId)
                .Include(playLog => playLog.Location)
                .OrderByDescending(playLog => playLog.PlayedAt)
                .ToListAsync();
            var locations = await QueryLocations(locationId).ToListAsync();

            var totalPlays = playLogs.Count;
            var uniqueSessions = playLogs.Select(playLog => playLog.SessionId).Distinct().Count();
            var journeyLogs = playLogs
                .Where(playLog => TourLifecycleSources.Contains(playLog.Source ?? PlaySources.Manual))
                .ToList();
            var activeTourSessions = await QueryTourSessions(locationId).CountAsync(session => !session.IsCompleted);
            var tourStarts = CountDistinctSessions(journeyLogs, PlaySources.TourStart);
            var tourResumes = CountDistinctSessions(journeyLogs, PlaySources.TourResume);
            var tourCompletions = CountDistinctSessions(journeyLogs, PlaySources.TourComplete);
            var tourDismissals = CountDistinctSessions(journeyLogs, PlaySources.TourDismiss);
            var topSources = playLogs
                .GroupBy(playLog => string.IsNullOrWhiteSpace(playLog.Source) ? PlaySources.Manual : playLog.Source!)
                .OrderByDescending(group => group.Count())
                .ToList();
            var topLanguages = playLogs
                .GroupBy(playLog => string.IsNullOrWhiteSpace(playLog.Language) ? "unknown" : playLog.Language!)
                .OrderByDescending(group => group.Count())
                .ToList();
            var topPois = playLogs
                .GroupBy(playLog => new
                {
                    playLog.LocationId,
                    LocationName = playLog.Location?.Name ?? "Unknown"
                })
                .OrderByDescending(group => group.Count())
                .ToList();

            var csv = new StringBuilder();
            csv.AppendLine("Section,Field,Value");
            csv.AppendLine($"Meta,GeneratedAtUtc,{EscapeCsv(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))}");
            csv.AppendLine($"Meta,Days,{normalizedDays}");
            csv.AppendLine($"Meta,LocationScope,{EscapeCsv(locationId.HasValue && locationId.Value > 0 ? locations.FirstOrDefault()?.Name ?? $"POI #{locationId.Value}" : "All POIs")}");
            csv.AppendLine($"Summary,TotalPlays,{totalPlays}");
            csv.AppendLine($"Summary,UniqueSessions,{uniqueSessions}");
            csv.AppendLine($"Summary,TotalPOIs,{locations.Count}");
            csv.AppendLine($"Summary,POIsWithAudio,{locations.Count(location => location.AudioFiles.Any())}");
            csv.AppendLine($"Summary,AvgDurationSeconds,{(totalPlays > 0 ? playLogs.Average(playLog => playLog.DurationSeconds) : 0):0.##}");
            csv.AppendLine($"Journey,ActiveTourSessions,{activeTourSessions}");
            csv.AppendLine($"Journey,TourStarts,{tourStarts}");
            csv.AppendLine($"Journey,TourResumes,{tourResumes}");
            csv.AppendLine($"Journey,TourCompletions,{tourCompletions}");
            csv.AppendLine($"Journey,TourDismissals,{tourDismissals}");
            csv.AppendLine($"Journey,TourCompletionRate,{(tourStarts > 0 ? Math.Round(tourCompletions * 100d / tourStarts, 1) : 0):0.#}%");
            csv.AppendLine($"Journey,TourDismissRate,{(tourStarts > 0 ? Math.Round(tourDismissals * 100d / tourStarts, 1) : 0):0.#}%");

            foreach (var sourceGroup in topSources)
            {
                csv.AppendLine($"Sources,{EscapeCsv(sourceGroup.Key)},{sourceGroup.Count()}");
            }

            foreach (var languageGroup in topLanguages)
            {
                csv.AppendLine($"Languages,{EscapeCsv(languageGroup.Key)},{languageGroup.Count()}");
            }

            foreach (var poiGroup in topPois)
            {
                csv.AppendLine($"TopPOIs,{EscapeCsv(poiGroup.Key.LocationName)},{poiGroup.Count()}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"admin-analytics-summary-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
        }

        /// <summary>
        /// Get heatmap data from user locations (anonymized)
        /// </summary>
        [HttpGet("heatmap")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetHeatmapData([FromQuery] int days = 30, [FromQuery] int limit = 500, [FromQuery] int? locationId = null)
        {
            var since = DateTime.UtcNow.AddDays(-days);

            // Lấy vị trí từ UserLocations (GPS tracking ẩn danh)
            var userPoints = await _context.UserLocations
                .Where(u => u.RecordedAt >= since)
                .OrderByDescending(u => u.RecordedAt)
                .Take(limit)
                .Select(u => new { u.Latitude, u.Longitude, Intensity = 0.5 })
                .ToListAsync();

            // Lấy thêm vị trí từ PlayLogs (nơi nghe audio)
            var playPoints = await _context.PlayLogs
                .Where(p => p.PlayedAt >= since
                    && p.Latitude.HasValue
                    && p.Longitude.HasValue
                    && (!locationId.HasValue || locationId.Value <= 0 || p.LocationId == locationId.Value))
                .OrderByDescending(p => p.PlayedAt)
                .Take(limit)
                .Select(p => new { Latitude = p.Latitude!.Value, Longitude = p.Longitude!.Value, Intensity = 1.0 })
                .ToListAsync();

            // Gộp cả 2 nguồn
            var allPoints = userPoints
                .Concat(playPoints)
                .Select(p => new double[] { p.Latitude, p.Longitude, p.Intensity })
                .ToList();

            return Ok(new
            {
                points = allPoints,
                totalUserLocations = userPoints.Count,
                totalPlayLocations = playPoints.Count,
                center = allPoints.Any()
                    ? new { lat = allPoints.Average(p => p[0]), lng = allPoints.Average(p => p[1]) }
                    : new { lat = 10.7580, lng = 106.7034 } // Default: Vĩnh Khánh, Q4
            });
        }

        private IQueryable<PlayLog> QueryPlayLogs(DateTime? since = null, int? locationId = null)
        {
            var query = _context.PlayLogs.AsNoTracking().AsQueryable();

            if (since.HasValue)
            {
                query = query.Where(playLog => playLog.PlayedAt >= since.Value);
            }

            if (locationId.HasValue && locationId.Value > 0)
            {
                query = query.Where(playLog => playLog.LocationId == locationId.Value);
            }

            return query;
        }

        private IQueryable<PlayLog> QueryTourEventLogs(DateTime? since = null, int? locationId = null)
        {
            return QueryPlayLogs(since, locationId)
                .Where(playLog => TourLifecycleSources.Contains(playLog.Source ?? PlaySources.Manual));
        }

        private IQueryable<TourSession> QueryTourSessions(int? locationId = null)
        {
            var query = _context.TourSessions.AsNoTracking().AsQueryable();

            if (locationId.HasValue && locationId.Value > 0)
            {
                query = query.Where(session => session.CurrentLocationId == locationId.Value);
            }

            return query;
        }

        private IQueryable<Location> QueryLocations(int? locationId = null)
        {
            var query = _context.Locations
                .AsNoTracking()
                .Include(location => location.AudioFiles)
                .AsQueryable();

            if (locationId.HasValue && locationId.Value > 0)
            {
                query = query.Where(location => location.Id == locationId.Value);
            }

            return query;
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

        private static async Task<int> CountDistinctSessionsAsync(IQueryable<PlayLog> query, string source)
        {
            return await query
                .Where(log => log.Source == source && log.SessionId != null)
                .Select(log => log.SessionId!)
                .Distinct()
                .CountAsync();
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

    public class LogPlayRequest
    {
        public int LocationId { get; set; }
        public double DurationSeconds { get; set; }
        public string? SessionId { get; set; }
        public string? DeviceType { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Source { get; set; }
    }
}
