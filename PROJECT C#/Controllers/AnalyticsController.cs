using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Data;
using PROJECT_C_.Models;

namespace PROJECT_C_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _context;

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
                FoodId = request.FoodId,
                DurationSeconds = request.DurationSeconds,
                SessionId = request.SessionId,
                DeviceType = request.DeviceType ?? "unknown",
                Language = Request.Headers["Accept-Language"].ToString().Split(',').FirstOrDefault() ?? "vi-VN",
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Source = request.Source ?? "manual",
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
        public async Task<IActionResult> GetStats()
        {
            var today = DateTime.UtcNow.Date;
            var thisWeek = today.AddDays(-7);
            var thisMonth = today.AddDays(-30);

            var stats = new
            {
                TotalPlays = await _context.PlayLogs.CountAsync(),
                TodayPlays = await _context.PlayLogs.CountAsync(p => p.PlayedAt >= today),
                WeekPlays = await _context.PlayLogs.CountAsync(p => p.PlayedAt >= thisWeek),
                MonthPlays = await _context.PlayLogs.CountAsync(p => p.PlayedAt >= thisMonth),
                UniqueSessions = await _context.PlayLogs.Select(p => p.SessionId).Distinct().CountAsync(),
                TotalPOIs = await _context.Foods.CountAsync(),
                POIsWithAudio = await _context.Foods.CountAsync(f => f.AudioFiles.Any())
            };

            return Ok(stats);
        }

        /// <summary>
        /// Get top played POIs
        /// </summary>
        [HttpGet("top-pois")]
        public async Task<IActionResult> GetTopPOIs([FromQuery] int limit = 10, [FromQuery] int days = 30)
        {
            var since = DateTime.UtcNow.AddDays(-days);

            var topPOIs = await _context.PlayLogs
                .Where(p => p.PlayedAt >= since)
                .GroupBy(p => p.FoodId)
                .Select(g => new
                {
                    FoodId = g.Key,
                    PlayCount = g.Count(),
                    TotalDuration = g.Sum(p => p.DurationSeconds),
                    AvgDuration = g.Average(p => p.DurationSeconds)
                })
                .OrderByDescending(x => x.PlayCount)
                .Take(limit)
                .ToListAsync();

            // Get food details
            var foodIds = topPOIs.Select(t => t.FoodId).ToList();
            var foods = await _context.Foods
                .Where(f => foodIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id);

            var result = topPOIs.Select(t => new
            {
                t.FoodId,
                FoodName = foods.ContainsKey(t.FoodId) ? foods[t.FoodId].Name : "Unknown",
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
        public async Task<IActionResult> GetTimeline([FromQuery] int days = 7)
        {
            var since = DateTime.UtcNow.Date.AddDays(-days);

            var timeline = await _context.PlayLogs
                .Where(p => p.PlayedAt >= since)
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
        public async Task<IActionResult> GetDeviceStats()
        {
            var devices = await _context.PlayLogs
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
        public async Task<IActionResult> GetSourceStats()
        {
            var sources = await _context.PlayLogs
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
        /// Get recent plays
        /// </summary>
        [HttpGet("recent")]
        [Authorize]
        public async Task<IActionResult> GetRecentPlays([FromQuery] int limit = 50)
        {
            var recentPlays = await _context.PlayLogs
                .Include(p => p.Food)
                .OrderByDescending(p => p.PlayedAt)
                .Take(limit)
                .Select(p => new
                {
                    p.Id,
                    p.FoodId,
                    FoodName = p.Food != null ? p.Food.Name : "Unknown",
                    p.PlayedAt,
                    p.DurationSeconds,
                    p.Source,
                    p.DeviceType
                })
                .ToListAsync();

            return Ok(recentPlays);
        }

        /// <summary>
        /// Get detailed play history with pagination and filters
        /// </summary>
        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? deviceType = null,
            [FromQuery] string? source = null,
            [FromQuery] int? foodId = null)
        {
            var query = _context.PlayLogs.Include(p => p.Food).AsQueryable();

            // Apply filters
            if (fromDate.HasValue)
                query = query.Where(p => p.PlayedAt >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(p => p.PlayedAt <= toDate.Value.AddDays(1));
            if (!string.IsNullOrEmpty(deviceType))
                query = query.Where(p => p.DeviceType == deviceType);
            if (!string.IsNullOrEmpty(source))
                query = query.Where(p => p.Source == source);
            if (foodId.HasValue)
                query = query.Where(p => p.FoodId == foodId.Value);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await query
                .OrderByDescending(p => p.PlayedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.FoodId,
                    FoodName = p.Food != null ? p.Food.Name : "Unknown",
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
        [Authorize]
        public async Task<IActionResult> ExportHistory(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var query = _context.PlayLogs.Include(p => p.Food).AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(p => p.PlayedAt >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(p => p.PlayedAt <= toDate.Value.AddDays(1));

            var data = await query
                .OrderByDescending(p => p.PlayedAt)
                .Select(p => new
                {
                    p.Id,
                    FoodName = p.Food != null ? p.Food.Name : "Unknown",
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
            csv.AppendLine("ID,Món ăn,Thời gian,Thời lượng (s),Nguồn,Thiết bị,Ngôn ngữ,Session");
            foreach (var row in data)
            {
                csv.AppendLine($"{row.Id},\"{row.FoodName}\",{row.PlayedAt},{row.DurationSeconds},{row.Source},{row.DeviceType},{row.Language},{row.SessionId}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"usage-history-{DateTime.UtcNow:yyyyMMdd}.csv");
        }
    }

    public class LogPlayRequest
    {
        public int FoodId { get; set; }
        public double DurationSeconds { get; set; }
        public string? SessionId { get; set; }
        public string? DeviceType { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Source { get; set; }
    }
}
