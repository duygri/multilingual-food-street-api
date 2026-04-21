using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
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
public sealed class AdminController(IModerationService moderationService, IAnalyticsService analyticsService, Server.Data.AppDbContext dbContext) : ControllerBase
{
    [HttpGet("pois")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminPoiDto>>>> PoisAsync(CancellationToken cancellationToken)
    {
        var pendingModerationByPoiId = await dbContext.ModerationRequests
            .AsNoTracking()
            .Where(item => item.EntityType == "poi" && item.Status == ModerationStatus.Pending)
            .ToDictionaryAsync(item => item.EntityId, item => item.Id, cancellationToken);

        var pois = await dbContext.Pois
            .AsNoTracking()
            .Include(item => item.Owner)
            .Include(item => item.Category)
            .Include(item => item.AudioAssets)
            .Include(item => item.Translations)
            .Include(item => item.Geofences)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

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
                PendingModerationId = pendingModerationByPoiId.GetValueOrDefault(item.Id.ToString()),
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
            .Where(item => item.UserId.HasValue)
            .Select(item => new
            {
                UserId = item.UserId!.Value,
                item.DeviceId,
                item.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var activityByUserId = visitEventActivity
            .GroupBy(item => item.UserId)
            .ToDictionary(
                group => group.Key,
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

        var users = await dbContext.AppUsers
            .AsNoTracking()
            .Include(user => user.Role)
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);

        var response = users
            .Select(user =>
            {
                activityByUserId.TryGetValue(user.Id, out var activity);
                var activeDeviceCount = activity?.ActiveDeviceCount ?? 0;

                return new UserSummaryDto
                {
                    Id = user.Id,
                    Email = user.Email,
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

        return Ok(new ApiResponse<IReadOnlyList<UserSummaryDto>> { Succeeded = true, Message = "Users loaded.", Data = response });
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
}
