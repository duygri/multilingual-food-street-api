using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NarrationApp.Server.Configuration;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Auth;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed class AuthService(AppDbContext dbContext, IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await dbContext.AppUsers.AnyAsync(user => user.Email == normalizedEmail, cancellationToken))
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var touristRole = await dbContext.Roles.SingleAsync(role => role.Name == "tourist", cancellationToken);
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            PreferredLanguage = string.IsNullOrWhiteSpace(request.PreferredLanguage)
                ? AppConstants.DefaultLanguage
                : request.PreferredLanguage.Trim().ToLowerInvariant(),
            RoleId = touristRole.Id,
            Role = touristRole,
            IsActive = true
        };

        dbContext.AppUsers.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToAuthResponse(user, touristRole.Name);
    }

    public async Task<OwnerRegistrationResponse> RegisterOwnerAsync(RegisterOwnerRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await dbContext.AppUsers.AnyAsync(user => user.Email == normalizedEmail, cancellationToken))
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var ownerRole = await dbContext.Roles.SingleAsync(role => role.Name == "poi_owner", cancellationToken);
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            PreferredLanguage = AppConstants.DefaultLanguage,
            RoleId = ownerRole.Id,
            Role = ownerRole,
            IsActive = false
        };

        dbContext.AppUsers.Add(user);
        dbContext.ModerationRequests.Add(new ModerationRequest
        {
            EntityType = "owner_registration",
            EntityId = user.Id.ToString(),
            Status = ModerationStatus.Pending,
            RequestedBy = user.Id,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new OwnerRegistrationResponse
        {
            UserId = user.Id,
            Email = user.Email,
            SubmittedAtUtc = DateTime.UtcNow
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var (user, roleName) = await GetAuthenticatedUserAsync(request, cancellationToken);

        if (string.Equals(roleName, "tourist", StringComparison.OrdinalIgnoreCase))
        {
            throw new AuthFlowException(
                "mobile_app_only",
                "Tài khoản du khách chỉ dùng trên ứng dụng di động.",
                HttpStatusCode.Forbidden);
        }

        if (!user.IsActive)
        {
            throw await CreateInactiveUserExceptionAsync(user, roleName, cancellationToken);
        }

        await RecordLoginAsync(user, cancellationToken);
        return ToAuthResponse(user, roleName);
    }

    public async Task<AuthResponse> LoginTouristAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var (user, roleName) = await GetAuthenticatedUserAsync(request, cancellationToken);

        if (!string.Equals(roleName, "tourist", StringComparison.OrdinalIgnoreCase))
        {
            throw new AuthFlowException(
                "tourist_login_only",
                "Endpoint này chỉ dành cho tài khoản tourist trên ứng dụng di động.",
                HttpStatusCode.Forbidden);
        }

        if (!user.IsActive)
        {
            throw new AuthFlowException(
                "account_inactive",
                "Tài khoản hiện chưa thể đăng nhập.",
                HttpStatusCode.Forbidden);
        }

        await RecordLoginAsync(user, cancellationToken);
        return ToAuthResponse(user, roleName);
    }

    public async Task<AuthResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.AppUsers
            .Include(appUser => appUser.Role)
            .SingleOrDefaultAsync(appUser => appUser.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User was not found.");

        return ToAuthResponse(user, user.Role?.Name ?? throw new InvalidOperationException("User role is missing."));
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.AppUsers.SingleOrDefaultAsync(appUser => appUser.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User was not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Current password is invalid.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.AppUsers
            .Include(appUser => appUser.Role)
            .SingleOrDefaultAsync(appUser => appUser.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User was not found.");

        user.PreferredLanguage = string.IsNullOrWhiteSpace(request.PreferredLanguage)
            ? AppConstants.DefaultLanguage
            : request.PreferredLanguage.Trim().ToLowerInvariant();

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToAuthResponse(user, user.Role?.Name ?? throw new InvalidOperationException("User role is missing."));
    }

    private async Task<(AppUser User, string RoleName)> GetAuthenticatedUserAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.AppUsers
            .Include(appUser => appUser.Role)
            .SingleOrDefaultAsync(appUser => appUser.Email == normalizedEmail, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var roleName = user.Role?.Name ?? throw new InvalidOperationException("User role is missing.");
        return (user, roleName);
    }

    private AuthResponse ToAuthResponse(AppUser user, string roleName)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiresInMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, roleName),
            new("preferred_language", user.PreferredLanguage)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PreferredLanguage = user.PreferredLanguage,
            Role = roleName switch
            {
                "admin" => UserRole.Admin,
                "poi_owner" => UserRole.PoiOwner,
                _ => UserRole.Tourist
            },
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expiresAtUtc
        };
    }

    private async Task RecordLoginAsync(AppUser user, CancellationToken cancellationToken)
    {
        user.LastLoginAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthFlowException> CreateInactiveUserExceptionAsync(AppUser user, string roleName, CancellationToken cancellationToken)
    {
        if (!string.Equals(roleName, "poi_owner", StringComparison.OrdinalIgnoreCase))
        {
            return new AuthFlowException(
                "account_inactive",
                "Tài khoản hiện chưa thể đăng nhập.",
                HttpStatusCode.Forbidden);
        }

        var moderation = await dbContext.ModerationRequests
            .AsNoTracking()
            .Where(item => item.EntityType == "owner_registration" && item.EntityId == user.Id.ToString())
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (moderation?.Status == ModerationStatus.Rejected)
        {
            var message = string.IsNullOrWhiteSpace(moderation.ReviewNote)
                ? "Yêu cầu đăng ký owner đã bị từ chối."
                : $"Yêu cầu đăng ký owner đã bị từ chối. {moderation.ReviewNote}";

            return new AuthFlowException(
                "owner_registration_rejected",
                message,
                HttpStatusCode.Forbidden);
        }

        return new AuthFlowException(
            "owner_pending_approval",
            "Tài khoản đang chờ admin duyệt.",
            HttpStatusCode.Forbidden);
    }
}
