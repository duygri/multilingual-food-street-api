using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using NarrationApp.Server.Extensions;
using NarrationApp.Server.Services;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin")]
public sealed class AdminController(
    IModerationService moderationService,
    IAnalyticsService analyticsService,
    Server.Data.AppDbContext dbContext,
    IQrWebPresenceTracker qrWebPresenceTracker) : ControllerBase
{
    private static readonly TimeSpan QrWebOnlineWindow = TimeSpan.FromSeconds(30);

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
            .Select(item => new AdminPoiDto
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
                PendingModerationId = pendingModerationByPoiId.TryGetValue(item.Id.ToString(), out var pendingModerationId)
                    ? pendingModerationId
                    : null,
                CreatedAtUtc = item.CreatedAt
            })
            .ToArray();

        return Ok(new ApiResponse<IReadOnlyList<AdminPoiDto>> { Succeeded = true, Message = "Admin POIs loaded.", Data = response });
    }

    [HttpGet("moderation/pending")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ModerationRequestDto>>>> PendingModerationAsync(CancellationToken cancellationToken)
    {
        var response = await moderationService.GetPendingAsync(cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<ModerationRequestDto>> { Succeeded = true, Message = "Pending moderation loaded.", Data = response });
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
        var nowUtc = DateTime.UtcNow;
        var onlineThresholdUtc = DateTime.UtcNow.AddMinutes(-15);
        var touristUsersById = await dbContext.AppUsers
            .AsNoTracking()
            .Include(user => user.Role)
            .Where(user => user.Role != null && user.Role.Name == "tourist")
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        var visitEventActivity = await dbContext.VisitEvents
            .AsNoTracking()
            .Select(item => new
            {
                item.UserId,
                item.DeviceId,
                item.PoiId,
                item.EventType,
                item.Source,
                item.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var response = visitEventActivity
            .Where(item => !string.IsNullOrWhiteSpace(item.DeviceId))
            .GroupBy(
                item => $"{(item.UserId.HasValue ? item.UserId.Value.ToString("N") : "guest")}|{item.DeviceId.Trim().ToLowerInvariant()}",
                StringComparer.Ordinal)
            .Select(group =>
            {
                var ordered = group
                    .OrderByDescending(item => item.CreatedAt)
                    .ToArray();

                var latest = ordered[0];
                var deviceId = latest.DeviceId.Trim();
                string roleName;
                Server.Data.Entities.AppUser? tourist = null;

                if (latest.UserId.HasValue)
                {
                    if (!touristUsersById.TryGetValue(latest.UserId.Value, out tourist))
                    {
                        return null;
                    }

                    roleName = "tourist";
                }
                else
                {
                    roleName = "guest";
                }

                var normalizedLanguage = NormalizeLanguageTag(tourist?.PreferredLanguage);
                if (string.IsNullOrWhiteSpace(normalizedLanguage))
                {
                    normalizedLanguage = InferLanguageTag(latest.Source, deviceId);
                }

                var passiveVisitorDevice = IsPassiveVisitorDevice(latest.Source, deviceId);
                var qrWebPresenceLastSeenUtc = IsQrWebVisitor(latest.Source, deviceId)
                    ? qrWebPresenceTracker.GetLastSeenUtc(deviceId)
                    : null;
                var effectiveLastSeenAtUtc = GetEffectiveVisitorLastSeenUtc(latest.CreatedAt, qrWebPresenceLastSeenUtc);

                return new VisitorDeviceSummaryDto
                {
                    Id = CreateStableVisitorId(latest.UserId, deviceId),
                    DisplayName = FormatVisitorDisplayName(deviceId, latest.Source, roleName, tourist?.FullName),
                    AccountLabel = roleName == "tourist" ? tourist?.Email ?? string.Empty : string.Empty,
                    DeviceId = deviceId,
                    PreferredLanguage = normalizedLanguage,
                    RoleName = roleName,
                    IsOnline = IsVisitorOnline(latest.Source, deviceId, effectiveLastSeenAtUtc, onlineThresholdUtc, nowUtc),
                    AutoPlayEnabled = !passiveVisitorDevice,
                    BackgroundTrackingEnabled = !passiveVisitorDevice,
                    TrackingCount = ordered.Length,
                    VisitCount = ordered
                        .Select(item => item.PoiId)
                        .Distinct()
                        .Count(),
                    TriggerCount = ordered.Count(item => item.EventType == EventType.GeofenceEnter),
                    LastSeenAtUtc = effectiveLastSeenAtUtc
                };
            })
            .Where(item => item is not null)
            .Cast<VisitorDeviceSummaryDto>()
            .OrderByDescending(item => item.IsOnline)
            .ThenByDescending(item => item.LastSeenAtUtc)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Ok(new ApiResponse<IReadOnlyList<VisitorDeviceSummaryDto>>
        {
            Succeeded = true,
            Message = "Visitor devices loaded.",
            Data = response
        });
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

    private static Guid CreateStableVisitorId(Guid? userId, string deviceId)
    {
        var seed = $"{(userId.HasValue ? userId.Value.ToString("N") : "guest")}|{deviceId.Trim().ToLowerInvariant()}";
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(seed));
        return new Guid(hash);
    }

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

    private static string FormatVisitorDisplayName(string deviceId, string? source, string roleName, string? fallbackName)
    {
        if (IsQrVisitor(source, deviceId))
        {
            return $"{InferDevicePlatform(deviceId, source)} quét QR";
        }

        var deviceLabel = FormatReadableDeviceName(deviceId);
        if (!string.Equals(deviceLabel, "Thiết bị visitor", StringComparison.OrdinalIgnoreCase))
        {
            return deviceLabel;
        }

        if (!string.IsNullOrWhiteSpace(fallbackName))
        {
            return fallbackName.Trim();
        }

        return string.Equals(roleName, "guest", StringComparison.OrdinalIgnoreCase)
            ? "Khách ẩn danh"
            : "Visitor mobile";
    }

    private static string FormatReadableDeviceName(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return "Thiết bị visitor";
        }

        if (string.Equals(deviceId, "anonymous-device", StringComparison.OrdinalIgnoreCase))
        {
            return "Khách ẩn danh";
        }

        var tokens = deviceId
            .Split(['-', '_', '.', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => !IsVisitorNoiseToken(token))
            .ToArray();

        if (tokens.Length == 0)
        {
            return $"Thiết bị visitor {GetDeviceSuffix(deviceId)}".Trim();
        }

        var label = string.Join(" ", tokens.Select(MapDeviceToken).Take(4));
        return $"{label} {GetDeviceSuffix(deviceId)}".Trim();
    }

    private static bool IsVisitorNoiseToken(string token)
    {
        var normalized = token.Trim().ToLowerInvariant();
        if (normalized.Length == 0)
        {
            return true;
        }

        return normalized is "device" or "devices" or "guest" or "tourist" or "mobile" or "mode" or "app" or "scan" or "qr" or "web"
            || normalized.StartsWith("guest", StringComparison.Ordinal)
            || normalized.StartsWith("tourist", StringComparison.Ordinal)
            || normalized.StartsWith("device", StringComparison.Ordinal)
            || LooksLikeOpaqueToken(token);
    }

    private static bool IsPassiveVisitorDevice(string? source, string deviceId)
    {
        if (IsQrVisitor(source, deviceId))
        {
            return true;
        }

        var combined = $"{source} {deviceId}".ToLowerInvariant();
        return combined.Contains("web", StringComparison.Ordinal)
            || combined.Contains("browser", StringComparison.Ordinal);
    }

    private static bool IsQrVisitor(string? source, string deviceId)
    {
        var combined = $"{source} {deviceId}".ToLowerInvariant();
        return combined.Contains("qr", StringComparison.Ordinal);
    }

    private static bool IsQrWebVisitor(string? source, string deviceId)
    {
        var combined = $"{source} {deviceId}".ToLowerInvariant();
        return combined.Contains("qr-web", StringComparison.Ordinal)
            || combined.Contains("browser", StringComparison.Ordinal);
    }

    private static DateTime GetEffectiveVisitorLastSeenUtc(DateTime latestEventAtUtc, DateTime? qrWebPresenceLastSeenUtc)
    {
        if (!qrWebPresenceLastSeenUtc.HasValue || qrWebPresenceLastSeenUtc.Value <= latestEventAtUtc)
        {
            return latestEventAtUtc;
        }

        return qrWebPresenceLastSeenUtc.Value;
    }

    private static bool IsVisitorOnline(string? source, string deviceId, DateTime effectiveLastSeenAtUtc, DateTime onlineThresholdUtc, DateTime nowUtc)
    {
        if (IsQrWebVisitor(source, deviceId))
        {
            return effectiveLastSeenAtUtc >= nowUtc.Subtract(QrWebOnlineWindow);
        }

        return effectiveLastSeenAtUtc >= onlineThresholdUtc;
    }

    private static string InferDevicePlatform(string deviceId, string? source)
    {
        var combined = $"{deviceId} {source}".ToLowerInvariant();

        if (combined.Contains("iphone", StringComparison.Ordinal) || combined.Contains("ios", StringComparison.Ordinal) || combined.Contains("ipad", StringComparison.Ordinal))
        {
            return "iPhone";
        }

        if (combined.Contains("android", StringComparison.Ordinal) || combined.Contains("pixel", StringComparison.Ordinal) || combined.Contains("samsung", StringComparison.Ordinal) || combined.Contains("mi", StringComparison.Ordinal))
        {
            return "Android";
        }

        if (combined.Contains("windows", StringComparison.Ordinal))
        {
            return "Windows";
        }

        if (combined.Contains("mac", StringComparison.Ordinal))
        {
            return "Mac";
        }

        return "Visitor";
    }

    private static string NormalizeLanguageTag(string? languageCode)
    {
        return (languageCode ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "vi" or "vi-vn" => "vi-VN",
            "en" or "en-us" or "en-gb" => "en-US",
            "ja" or "ja-jp" => "ja-JP",
            "ko" or "ko-kr" => "ko-KR",
            "zh" or "zh-cn" => "zh-CN",
            _ => string.Empty
        };
    }

    private static string InferLanguageTag(string? source, string deviceId)
    {
        var combined = $"{source} {deviceId}".ToLowerInvariant();

        if (combined.Contains("ja", StringComparison.Ordinal))
        {
            return "ja-JP";
        }

        if (combined.Contains("ko", StringComparison.Ordinal))
        {
            return "ko-KR";
        }

        if (combined.Contains("zh", StringComparison.Ordinal))
        {
            return "zh-CN";
        }

        if (combined.Contains("en", StringComparison.Ordinal))
        {
            return "en-US";
        }

        return "vi-VN";
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
}
