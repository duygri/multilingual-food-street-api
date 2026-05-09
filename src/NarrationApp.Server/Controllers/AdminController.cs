using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Server.Extensions;
using NarrationApp.Server.Services;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin")]
public sealed class AdminController(
    IModerationService moderationService,
    IAnalyticsService analyticsService,
    Server.Data.AppDbContext dbContext,
    IVisitorDeviceDashboardService visitorDeviceDashboardService,
    IPoiReviewService poiReviewService) : ControllerBase
{
    [HttpGet("pois")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminPoiDto>>>> PoisAsync(CancellationToken cancellationToken)
    {
        var pois = await dbContext.Pois
            .AsNoTracking()
            .Include(item => item.Owner)
            .Include(item => item.Category)
            .Include(item => item.AudioAssets)
            .Include(item => item.Translations)
            .Include(item => item.Geofences)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        var pendingPoiEntityIds = pois
            .Where(item => item.Status == PoiStatus.PendingReview)
            .Select(item => item.Id.ToString())
            .ToArray();

        var pendingModerationByPoiId = pendingPoiEntityIds.Length == 0
            ? new Dictionary<string, int>(StringComparer.Ordinal)
            : (await dbContext.ModerationRequests
                    .AsNoTracking()
                    .Where(item => item.EntityType == "poi"
                        && item.Status == ModerationStatus.Pending
                        && pendingPoiEntityIds.Contains(item.EntityId))
                    .OrderByDescending(item => item.CreatedAt)
                    .ToListAsync(cancellationToken))
                .GroupBy(item => item.EntityId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.Ordinal);

        var response = pois
            .Select(item => BuildAdminPoiDto(
                item,
                pendingModerationByPoiId.TryGetValue(item.Id.ToString(), out var pendingModerationId)
                    ? pendingModerationId
                    : null))
            .ToArray();

        return Ok(new ApiResponse<IReadOnlyList<AdminPoiDto>> { Succeeded = true, Message = "Admin POIs loaded.", Data = response });
    }

    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpPut("pois/{id:int}/priority")]
    public async Task<ActionResult<ApiResponse<AdminPoiDto>>> UpdatePoiPriorityAsync(
        int id,
        UpdatePoiPriorityRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Priority <= 0)
        {
            return BadRequest(new ApiResponse<AdminPoiDto>
            {
                Succeeded = false,
                Message = "Priority is invalid.",
                Error = new ErrorResponse { Code = "invalid_poi_priority", Message = "Priority must be greater than zero." }
            });
        }

        var poi = await dbContext.Pois
            .Include(item => item.Owner)
            .Include(item => item.Category)
            .Include(item => item.AudioAssets)
            .Include(item => item.Translations)
            .Include(item => item.Geofences)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (poi is null)
        {
            return NotFound(new ApiResponse<AdminPoiDto>
            {
                Succeeded = false,
                Message = "POI not found.",
                Error = new ErrorResponse { Code = "poi_not_found", Message = "POI not found." }
            });
        }

        poi.Priority = request.Priority;
        await dbContext.SaveChangesAsync(cancellationToken);

        var pendingModerationId = poi.Status != PoiStatus.PendingReview
            ? null
            : await dbContext.ModerationRequests
                .AsNoTracking()
                .Where(item => item.EntityType == "poi"
                    && item.EntityId == poi.Id.ToString()
                    && item.Status == ModerationStatus.Pending)
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => (int?)item.Id)
                .FirstOrDefaultAsync(cancellationToken);

        return Ok(new ApiResponse<AdminPoiDto>
        {
            Succeeded = true,
            Message = "POI priority updated.",
            Data = BuildAdminPoiDto(poi, pendingModerationId)
        });
    }

    [HttpGet("moderation/pending")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ModerationRequestDto>>>> PendingModerationAsync(CancellationToken cancellationToken)
    {
        var response = await moderationService.GetPendingAsync(cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<ModerationRequestDto>> { Succeeded = true, Message = "Pending moderation loaded.", Data = response });
    }

    [HttpGet("reviews/pending")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PoiReviewDto>>>> PendingPoiReviewsAsync(CancellationToken cancellationToken)
    {
        var response = await poiReviewService.GetPendingAsync(cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<PoiReviewDto>> { Succeeded = true, Message = "Pending POI reviews loaded.", Data = response });
    }

    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpPost("reviews/{id:int}/approve")]
    public async Task<ActionResult<ApiResponse<PoiReviewDto>>> ApprovePoiReviewAsync(
        int id,
        [FromBody] ReviewPoiReviewRequest request,
        CancellationToken cancellationToken)
    {
        var response = await poiReviewService.ReviewAsync(id, approve: true, request.ReviewNote, cancellationToken);
        return Ok(new ApiResponse<PoiReviewDto> { Succeeded = true, Message = "POI review approved.", Data = response });
    }

    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpPost("reviews/{id:int}/reject")]
    public async Task<ActionResult<ApiResponse<PoiReviewDto>>> RejectPoiReviewAsync(
        int id,
        [FromBody] ReviewPoiReviewRequest request,
        CancellationToken cancellationToken)
    {
        var response = await poiReviewService.ReviewAsync(id, approve: false, request.ReviewNote, cancellationToken);
        return Ok(new ApiResponse<PoiReviewDto> { Succeeded = true, Message = "POI review rejected.", Data = response });
    }

    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpPost("moderation/{id:int}/approve")]
    public async Task<ActionResult<ApiResponse<ModerationRequestDto>>> ApproveAsync(int id, [FromBody] ReviewModerationRequest request, CancellationToken cancellationToken)
    {
        var response = await moderationService.ReviewAsync(id, User.GetRequiredUserId(), true, request.ReviewNote, cancellationToken);
        return Ok(new ApiResponse<ModerationRequestDto> { Succeeded = true, Message = "Moderation approved.", Data = response });
    }

    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpPost("moderation/{id:int}/reject")]
    public async Task<ActionResult<ApiResponse<ModerationRequestDto>>> RejectAsync(int id, [FromBody] ReviewModerationRequest request, CancellationToken cancellationToken)
    {
        var response = await moderationService.ReviewAsync(id, User.GetRequiredUserId(), false, request.ReviewNote, cancellationToken);
        return Ok(new ApiResponse<ModerationRequestDto> { Succeeded = true, Message = "Moderation rejected.", Data = response });
    }

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserSummaryDto>>>> UsersAsync(CancellationToken cancellationToken)
    {
        var onlineThresholdUtc = DateTime.UtcNow.AddMinutes(-15);
        var visitEventActivity = await dbContext.VisitEvents
            .AsNoTracking()
            .Select(item => new
            {
                item.UserId,
                item.DeviceId,
                item.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var activityByUserId = visitEventActivity
            .Where(item => item.UserId.HasValue)
            .GroupBy(item => item.UserId)
            .ToDictionary(
                group => group.Key!.Value,
                group => new
                {
                    DeviceCount = group
                        .Select(item => item.DeviceId)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count(),
                    ActiveDeviceCount = group
                        .Where(item => item.CreatedAt >= onlineThresholdUtc)
                        .Select(item => item.DeviceId)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count(),
                    LastSeenAtUtc = group.Max(item => (DateTime?)item.CreatedAt)
                });

        var guestActivity = visitEventActivity
            .Where(item => !item.UserId.HasValue && !string.IsNullOrWhiteSpace(item.DeviceId))
            .GroupBy(item => item.DeviceId, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var activeDeviceCount = group.Any(item => item.CreatedAt >= onlineThresholdUtc) ? 1 : 0;
                var deviceId = group.Key.Trim();

                return new UserSummaryDto
                {
                    Id = CreateStableGuestId(deviceId),
                    DisplayName = FormatGuestDeviceName(deviceId),
                    Email = string.Empty,
                    DeviceId = deviceId,
                    PreferredLanguage = string.Empty,
                    IsActive = true,
                    RoleName = "guest",
                    DeviceCount = 1,
                    ActiveDeviceCount = activeDeviceCount,
                    IsOnline = activeDeviceCount > 0,
                    LastSeenAtUtc = group.Max(item => (DateTime?)item.CreatedAt)
                };
            })
            .OrderByDescending(item => item.IsOnline)
            .ThenByDescending(item => item.LastSeenAtUtc)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var users = await dbContext.AppUsers
            .AsNoTracking()
            .Include(user => user.Role)
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);

        var userRows = users
            .Select(user =>
            {
                activityByUserId.TryGetValue(user.Id, out var activity);
                var activeDeviceCount = activity?.ActiveDeviceCount ?? 0;

                return new UserSummaryDto
                {
                    Id = user.Id,
                    DisplayName = user.FullName,
                    Email = user.Email,
                    DeviceId = string.Empty,
                    PreferredLanguage = user.PreferredLanguage,
                    IsActive = user.IsActive,
                    RoleName = user.Role!.Name,
                    DeviceCount = activity?.DeviceCount ?? 0,
                    ActiveDeviceCount = activeDeviceCount,
                    IsOnline = user.IsActive && activeDeviceCount > 0,
                    LastSeenAtUtc = activity?.LastSeenAtUtc
                };
            })
            .ToList();

        var response = userRows
            .Concat(guestActivity)
            .ToArray();

        return Ok(new ApiResponse<IReadOnlyList<UserSummaryDto>> { Succeeded = true, Message = "Users loaded.", Data = response });
    }

    [HttpGet("visitor-devices")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VisitorDeviceSummaryDto>>>> VisitorDevicesAsync(CancellationToken cancellationToken)
    {
        var response = await visitorDeviceDashboardService.GetAsync(cancellationToken);

        return Ok(new ApiResponse<IReadOnlyList<VisitorDeviceSummaryDto>>
        {
            Succeeded = true,
            Message = "Visitor devices loaded.",
            Data = response
        });
    }

    [HttpGet("analytics/event-log.csv")]
    public async Task<FileContentResult> ExportEventLogCsvAsync(CancellationToken cancellationToken)
    {
        var rows = await dbContext.VisitEvents
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.Poi)
            .OrderByDescending(item => item.CreatedAt)
            .Take(5000)
            .Select(item => new EventLogExportRow
            {
                Id = item.Id,
                CreatedAtUtc = item.CreatedAt,
                EventType = item.EventType.ToString(),
                Source = item.Source,
                DeviceId = item.DeviceId,
                UserId = item.UserId,
                UserEmail = item.User != null ? item.User.Email : string.Empty,
                PoiId = item.PoiId,
                PoiName = item.Poi != null ? item.Poi.Name : string.Empty,
                Lat = item.Lat,
                Lng = item.Lng,
                ListenDurationSeconds = item.ListenDurationSeconds
            })
            .ToArrayAsync(cancellationToken);

        var csv = BuildEventLogCsv(rows);
        var body = Encoding.UTF8.GetBytes(csv);
        var preamble = Encoding.UTF8.GetPreamble();
        var fileBytes = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, fileBytes, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, fileBytes, preamble.Length, body.Length);
        var fileName = $"visit-event-log-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

        return File(fileBytes, "text/csv; charset=utf-8", fileName);
    }

    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpPut("users/{id:guid}/role")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateUserRoleAsync(Guid id, UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.AppUsers
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (user is null)
        {
            return NotFound(new ApiResponse<object>
            {
                Succeeded = false,
                Message = "User not found.",
                Error = new ErrorResponse { Code = "user_not_found", Message = "User not found." }
            });
        }

        var roleName = request.Role switch
        {
            UserRole.Admin => "admin",
            UserRole.PoiOwner => "poi_owner",
            UserRole.Tourist => "tourist",
            _ => throw new InvalidOperationException("Unsupported user role.")
        };

        var roleId = await dbContext.Roles
            .Where(item => item.Name == roleName)
            .Select(item => item.Id)
            .SingleAsync(cancellationToken);

        user.RoleId = roleId;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ApiResponse<object> { Succeeded = true, Message = "User role updated." });
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<object>>> StatsAsync(CancellationToken cancellationToken)
    {
        var response = await analyticsService.GetDashboardAsync(cancellationToken);
        return Ok(new ApiResponse<object> { Succeeded = true, Message = "Admin stats loaded.", Data = response });
    }

    [HttpGet("stats/overview")]
    public async Task<ActionResult<ApiResponse<object>>> OverviewAsync(CancellationToken cancellationToken)
    {
        var response = await analyticsService.GetDashboardAsync(cancellationToken);
        return Ok(new ApiResponse<object> { Succeeded = true, Message = "Admin overview loaded.", Data = response });
    }

    private static Guid CreateStableGuestId(string deviceId)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(deviceId.Trim().ToLowerInvariant()));
        return new Guid(hash);
    }

    private static AdminPoiDto BuildAdminPoiDto(Poi item, int? pendingModerationId = null) =>
        new()
        {
            Id = item.Id,
            Name = item.Name,
            Slug = item.Slug,
            OwnerName = item.Owner?.FullName ?? "Unknown owner",
            OwnerEmail = item.Owner?.Email ?? "unknown@narration.app",
            Lat = item.Lat,
            Lng = item.Lng,
            Priority = item.Priority,
            CategoryId = item.CategoryId,
            CategoryName = item.Category?.Name,
            Description = item.Description,
            TtsScript = item.TtsScript,
            Status = item.Status,
            AudioAssetCount = item.AudioAssets.Count,
            TranslationCount = item.Translations.Count,
            GeofenceCount = item.Geofences.Count,
            PendingModerationId = pendingModerationId,
            CreatedAtUtc = item.CreatedAt
        };

    private static string FormatGuestDeviceName(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return "Guest device";
        }

        if (string.Equals(deviceId, "anonymous-device", StringComparison.OrdinalIgnoreCase))
        {
            return "Anonymous device";
        }

        var tokens = deviceId
            .Split(['-', '_', '.', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => !LooksLikeOpaqueToken(token))
            .ToArray();

        if (tokens.Length == 0)
        {
            return $"Guest device {GetDeviceSuffix(deviceId)}".Trim();
        }

        var words = tokens
            .Where(token => !string.Equals(token, "device", StringComparison.OrdinalIgnoreCase))
            .Select(MapDeviceToken)
            .Take(3)
            .ToArray();

        var label = words.Length == 0 ? "Guest device" : string.Join(" ", words);
        return $"{label} {GetDeviceSuffix(deviceId)}".Trim();
    }

    private static bool LooksLikeOpaqueToken(string token)
    {
        if (token.Length < 8)
        {
            return false;
        }

        return token.All(Uri.IsHexDigit);
    }

    private static string MapDeviceToken(string token)
    {
        return token.ToLowerInvariant() switch
        {
            "android" => "Android",
            "ios" => "iPhone",
            "iphone" => "iPhone",
            "ipad" => "iPad",
            "windows" => "Windows",
            "macos" => "macOS",
            "mac" => "Mac",
            "browser" => "Browser",
            "web" => "Web",
            "guest" => "Guest",
            "tourist" => "Tourist",
            _ => char.ToUpperInvariant(token[0]) + token[1..]
        };
    }

    private static string GetDeviceSuffix(string deviceId)
    {
        var suffix = new string(deviceId.Where(char.IsLetterOrDigit).TakeLast(4).ToArray()).ToUpperInvariant();
        return string.IsNullOrWhiteSpace(suffix) ? string.Empty : $"· {suffix}";
    }

    private static string BuildEventLogCsv(IReadOnlyList<EventLogExportRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("event_id,created_at_utc,event_type,source,device_id,user_id,user_email,poi_id,poi_name,lat,lng,listen_duration_seconds");

        foreach (var row in rows)
        {
            AppendCsvRow(
                builder,
                row.Id.ToString(CultureInfo.InvariantCulture),
                row.CreatedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                row.EventType,
                row.Source,
                row.DeviceId,
                row.UserId?.ToString() ?? string.Empty,
                row.UserEmail,
                row.PoiId.ToString(CultureInfo.InvariantCulture),
                row.PoiName,
                FormatCsvDouble(row.Lat),
                FormatCsvDouble(row.Lng),
                row.ListenDurationSeconds.ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static string FormatCsvDouble(double? value) =>
        value.HasValue ? value.Value.ToString("0.######", CultureInfo.InvariantCulture) : string.Empty;

    private static void AppendCsvRow(StringBuilder builder, params string[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            AppendCsvValue(builder, values[index]);
        }

        builder.AppendLine();
    }

    private static void AppendCsvValue(StringBuilder builder, string value)
    {
        builder.Append('"');
        builder.Append(value.Replace("\"", "\"\"", StringComparison.Ordinal));
        builder.Append('"');
    }

    private sealed class EventLogExportRow
    {
        public long Id { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public string EventType { get; init; } = string.Empty;

        public string Source { get; init; } = string.Empty;

        public string DeviceId { get; init; } = string.Empty;

        public Guid? UserId { get; init; }

        public string UserEmail { get; init; } = string.Empty;

        public int PoiId { get; init; }

        public string PoiName { get; init; } = string.Empty;

        public double? Lat { get; init; }

        public double? Lng { get; init; }

        public int ListenDurationSeconds { get; init; }
    }
}
