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
