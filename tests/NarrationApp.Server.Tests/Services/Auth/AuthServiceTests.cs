using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NarrationApp.Server.Configuration;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Auth;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Services.Auth;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_returns_a_jwt_for_valid_seed_credentials()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var beforeLoginUtc = DateTime.UtcNow;
        var sut = CreateService(dbContext);

        var response = await sut.LoginAsync(new LoginRequest
        {
            Email = AppConstants.DefaultAdminEmail,
            Password = AppConstants.DefaultAdminPassword
        });
        dbContext.ChangeTracker.Clear();
        var loggedInUser = await dbContext.AppUsers.AsNoTracking().SingleAsync(user => user.Email == AppConstants.DefaultAdminEmail);

        Assert.Equal(AppConstants.DefaultAdminEmail, response.Email);
        Assert.Equal("System Admin", response.FullName);
        Assert.Equal(UserRole.Admin, response.Role);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.NotNull(loggedInUser.LastLoginAtUtc);
        Assert.InRange(loggedInUser.LastLoginAtUtc!.Value, beforeLoginUtc, DateTime.UtcNow);
    }

    [Fact]
    public async Task RegisterAsync_creates_a_tourist_account_and_returns_a_token()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var sut = CreateService(dbContext);

        var response = await sut.RegisterAsync(new RegisterRequest
        {
            Email = "tourist1@narration.app",
            Password = "Tourist@123",
            PreferredLanguage = "en"
        });

        var createdUser = await dbContext.AppUsers.SingleAsync(user => user.Email == "tourist1@narration.app");

        Assert.Equal("tourist1@narration.app", response.FullName);
        Assert.Equal(UserRole.Tourist, response.Role);
        Assert.Equal("en", createdUser.PreferredLanguage);
        Assert.True(BCrypt.Net.BCrypt.Verify("Tourist@123", createdUser.PasswordHash));
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
    }

    [Fact]
    public async Task LoginTouristAsync_returns_a_jwt_for_valid_tourist_credentials()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var sut = CreateService(dbContext);

        await sut.RegisterAsync(new RegisterRequest
        {
            Email = "tourist-mobile@narration.app",
            Password = "Tourist@123",
            PreferredLanguage = "ja"
        });

        var response = await sut.LoginTouristAsync(new LoginRequest
        {
            Email = "tourist-mobile@narration.app",
            Password = "Tourist@123"
        });

        Assert.Equal("tourist-mobile@narration.app", response.Email);
        Assert.Equal("tourist-mobile@narration.app", response.FullName);
        Assert.Equal(UserRole.Tourist, response.Role);
        Assert.Equal("ja", response.PreferredLanguage);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
    }

    [Fact]
    public async Task GetCurrentUserAsync_returns_a_tourist_fallback_display_name()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var sut = CreateService(dbContext);

        var registerResponse = await sut.RegisterAsync(new RegisterRequest
        {
            Email = "tourist-profile@narration.app",
            Password = "Tourist@123",
            PreferredLanguage = "vi"
        });

        var response = await sut.GetCurrentUserAsync(registerResponse.UserId);

        Assert.Equal("tourist-profile@narration.app", response.FullName);
        Assert.Equal(UserRole.Tourist, response.Role);
    }

    [Fact]
    public async Task RegisterOwnerAsync_creates_an_inactive_owner_application_and_pending_moderation_request()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var sut = CreateService(dbContext);

        var response = await sut.RegisterOwnerAsync(new RegisterOwnerRequest
        {
            FullName = "Owner Candidate",
            Email = "candidate-owner@narration.app",
            Password = "Owner@123"
        });

        var createdUser = await dbContext.AppUsers.SingleAsync(user => user.Email == "candidate-owner@narration.app");
        var moderation = await dbContext.ModerationRequests.SingleAsync(item => item.EntityType == "owner_registration" && item.EntityId == createdUser.Id.ToString());

        Assert.Equal("Owner Candidate", createdUser.FullName);
        Assert.False(createdUser.IsActive);
        Assert.Equal("candidate-owner@narration.app", response.Email);
        Assert.Equal(createdUser.Id, response.UserId);
        Assert.Equal(ModerationStatus.Pending, moderation.Status);
    }

    [Fact]
    public async Task LoginAsync_rejects_pending_owner_accounts_with_a_pending_message()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var sut = CreateService(dbContext);
        var ownerRoleId = await dbContext.Roles.Where(role => role.Name == "poi_owner").Select(role => role.Id).SingleAsync();
        var pendingOwner = new AppUser
        {
            Id = Guid.NewGuid(),
            FullName = "Pending Owner",
            Email = "pending-owner@narration.app",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Owner@123"),
            PreferredLanguage = "vi",
            RoleId = ownerRoleId,
            IsActive = false
        };

        dbContext.AppUsers.Add(pendingOwner);
        dbContext.ModerationRequests.Add(new ModerationRequest
        {
            EntityType = "owner_registration",
            EntityId = pendingOwner.Id.ToString(),
            Status = ModerationStatus.Pending,
            RequestedBy = pendingOwner.Id,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<AuthFlowException>(() => sut.LoginAsync(new LoginRequest
        {
            Email = pendingOwner.Email,
            Password = "Owner@123"
        }));

        Assert.Equal("owner_pending_approval", exception.ErrorCode);
    }

    [Fact]
    public async Task LoginAsync_rejects_tourist_accounts_for_web_portal_use()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var sut = CreateService(dbContext);

        await sut.RegisterAsync(new RegisterRequest
        {
            Email = "tourist-web@narration.app",
            Password = "Tourist@123",
            PreferredLanguage = "vi"
        });

        var exception = await Assert.ThrowsAsync<AuthFlowException>(() => sut.LoginAsync(new LoginRequest
        {
            Email = "tourist-web@narration.app",
            Password = "Tourist@123"
        }));

        Assert.Equal("mobile_app_only", exception.ErrorCode);
    }

    private static AuthService CreateService(Server.Data.AppDbContext dbContext)
    {
        return new AuthService(
            dbContext,
            Options.Create(new JwtOptions
            {
                Issuer = "Tests",
                Audience = "Tests",
                SigningKey = "tests-only-signing-key-with-sufficient-length-1234567890",
                ExpiresInMinutes = 60
            }));
    }
}
