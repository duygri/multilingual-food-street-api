using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Controllers;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Notification;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Controllers;

public sealed class OwnerControllerTests
{
    [Fact]
    public async Task GetShellSummaryAsync_returns_owner_counts_for_shell()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var owner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "owner-shell-summary@narration.app");
        var otherOwner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "other-shell-owner@narration.app");

        var publishedPoi = new Poi
        {
            Name = "Published POI",
            Slug = "published-shell-poi",
            OwnerId = owner.Id,
            Lat = 10.758,
            Lng = 106.701,
            Priority = 1,
            NarrationMode = NarrationMode.Both,
            Description = "Published description",
            TtsScript = "Published script",
            Status = PoiStatus.Published,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };
        var pendingPoi = new Poi
        {
            Name = "Pending POI",
            Slug = "pending-shell-poi",
            OwnerId = owner.Id,
            Lat = 10.759,
            Lng = 106.702,
            Priority = 2,
            NarrationMode = NarrationMode.Both,
            Description = "Pending description",
            TtsScript = "Pending script",
            Status = PoiStatus.PendingReview,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        dbContext.Pois.AddRange(
            publishedPoi,
            pendingPoi,
            new Poi
            {
                Name = "Other Owner POI",
                Slug = "other-owner-shell-poi",
                OwnerId = otherOwner.Id,
                Lat = 10.760,
                Lng = 106.703,
                Priority = 3,
                NarrationMode = NarrationMode.Both,
                Description = "Other owner description",
                TtsScript = "Other owner script",
                Status = PoiStatus.Published,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });

        await dbContext.SaveChangesAsync();

        dbContext.ModerationRequests.AddRange(
            new ModerationRequest
            {
                EntityType = "poi",
                EntityId = publishedPoi.Id.ToString(),
                RequestedBy = owner.Id,
                Status = ModerationStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddHours(-6)
            },
            new ModerationRequest
            {
                EntityType = "poi",
                EntityId = publishedPoi.Id.ToString(),
                RequestedBy = owner.Id,
                Status = ModerationStatus.Approved,
                CreatedAt = DateTime.UtcNow.AddHours(-4)
            },
            new ModerationRequest
            {
                EntityType = "poi",
                EntityId = pendingPoi.Id.ToString(),
                RequestedBy = otherOwner.Id,
                Status = ModerationStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            });

        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, owner.Id, unreadCount: 4);

        var actionResult = await controller.GetShellSummaryAsync(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<OwnerShellSummaryDto>>(okResult.Value);
        var summary = Assert.IsType<OwnerShellSummaryDto>(response.Data);

        Assert.True(response.Succeeded);
        Assert.Equal(2, summary.TotalPois);
        Assert.Equal(1, summary.PublishedPois);
        Assert.Equal(1, summary.PendingModerationRequests);
        Assert.Equal(4, summary.UnreadNotifications);
    }

    [Fact]
    public async Task GetPoisAsync_returns_category_names_for_owned_pois()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var owner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "owner-poi-list@narration.app");
        var category = new Category
        {
            Name = "Ăn vặt",
            Slug = "an-vat",
            Description = "Snacks",
            Icon = "snack",
            DisplayOrder = 2,
            IsActive = true
        };

        dbContext.Pois.Add(new Poi
        {
            Name = "Ốc đêm Vĩnh Khánh",
            Slug = "oc-dem-vinh-khanh",
            OwnerId = owner.Id,
            Lat = 10.758,
            Lng = 106.701,
            Priority = 7,
            Category = category,
            NarrationMode = NarrationMode.Both,
            Description = "Quán ốc đêm đông khách.",
            TtsScript = "Kịch bản tiếng Việt.",
            Status = PoiStatus.Published,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, owner.Id, unreadCount: 0);

        var actionResult = await controller.GetPoisAsync(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<PoiDto>>>(okResult.Value);
        var poi = Assert.Single(Assert.IsType<PoiDto[]>(response.Data));

        Assert.True(response.Succeeded);
        Assert.Equal("Ăn vặt", poi.CategoryName);
    }

    [Fact]
    public async Task GetPoiAsync_returns_owned_poi_with_translations_and_geofences()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var owner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "owner-poi-detail@narration.app");
        var category = new Category
        {
            Name = "Hải sản",
            Slug = "hai-san",
            Description = "Seafood",
            Icon = "shrimp",
            DisplayOrder = 1,
            IsActive = true
        };

        var poi = new Poi
        {
            Name = "Bún mắm Vĩnh Khánh",
            Slug = "bun-mam-vinh-khanh",
            OwnerId = owner.Id,
            Lat = 10.758,
            Lng = 106.701,
            Priority = 7,
            Category = category,
            NarrationMode = NarrationMode.Both,
            Description = "Món bún mắm đậm vị về đêm.",
            TtsScript = "Kịch bản tiếng Việt.",
            Status = PoiStatus.Rejected,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            Translations =
            [
                new PoiTranslation
                {
                    LanguageCode = "vi",
                    Title = "Bún mắm",
                    Description = "Đậm vị",
                    Story = "Câu chuyện",
                    Highlight = "Nước lèo",
                    IsFallback = false
                },
                new PoiTranslation
                {
                    LanguageCode = "en",
                    Title = "Fermented noodle soup",
                    Description = "Rich broth",
                    Story = "Story",
                    Highlight = "Broth",
                    IsFallback = true
                }
            ],
            Geofences =
            [
                new Geofence
                {
                    Name = "Vùng kích hoạt chính",
                    RadiusMeters = 35,
                    Priority = 8,
                    DebounceSeconds = 10,
                    CooldownSeconds = 600,
                    IsActive = true,
                    TriggerAction = "auto_play",
                    NearestOnly = true
                }
            ]
        };

        dbContext.Pois.Add(poi);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, owner.Id, unreadCount: 0);

        var actionResult = await controller.GetPoiAsync(poi.Id, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<PoiDto>>(okResult.Value);
        var result = Assert.IsType<PoiDto>(response.Data);

        Assert.True(response.Succeeded);
        Assert.Equal(poi.Id, result.Id);
        Assert.Equal(owner.Id, result.OwnerId);
        Assert.Equal("Bún mắm Vĩnh Khánh", result.Name);
        Assert.Equal("Hải sản", result.CategoryName);
        Assert.Equal(PoiStatus.Rejected, result.Status);
        Assert.Equal(2, result.Translations.Count);
        Assert.Contains(result.Translations, item => item.LanguageCode == "vi" && item.Title == "Bún mắm");
        Assert.Single(result.Geofences);
        Assert.Equal("Vùng kích hoạt chính", result.Geofences[0].Name);
    }

    [Fact]
    public async Task GetPoiAsync_returns_not_found_for_other_owner_poi()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var owner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "owner-scope@narration.app");
        var otherOwner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "other-owner@narration.app");
        var otherPoi = new Poi
        {
            Name = "Other POI",
            Slug = "other-poi",
            OwnerId = otherOwner.Id,
            Lat = 10.758,
            Lng = 106.701,
            Priority = 10,
            NarrationMode = NarrationMode.TtsOnly,
            Description = "Owned by someone else.",
            TtsScript = "Script",
            Status = PoiStatus.Published,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        dbContext.Pois.Add(otherPoi);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, owner.Id, unreadCount: 0);

        var actionResult = await controller.GetPoiAsync(otherPoi.Id, CancellationToken.None);
        var notFound = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<PoiDto>>(notFound.Value);

        Assert.False(response.Succeeded);
        Assert.Equal("poi_not_found", response.Error?.Code);
    }

    [Fact]
    public async Task GetProfileAsync_returns_editable_owner_profile_data_and_activity_summary()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var owner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "owner-profile@narration.app");
        owner.FullName = "Owner Profile";
        owner.Phone = "+84 90 123 4567";
        owner.ManagedArea = "District 1";
        owner.PreferredLanguage = "en";
        owner.CreatedAtUtc = DateTime.UtcNow.AddDays(-30);
        owner.LastLoginAtUtc = DateTime.UtcNow.AddHours(-6);

        var publishedPoi = new Poi
        {
            Name = "Published POI",
            Slug = "published-poi",
            OwnerId = owner.Id,
            Lat = 10.774,
            Lng = 106.701,
            Priority = 1,
            NarrationMode = NarrationMode.TtsOnly,
            Description = "Published description",
            TtsScript = "Published script",
            Status = PoiStatus.Published,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };

        var draftPoi = new Poi
        {
            Name = "Draft POI",
            Slug = "draft-poi",
            OwnerId = owner.Id,
            Lat = 10.775,
            Lng = 106.702,
            Priority = 2,
            NarrationMode = NarrationMode.TtsOnly,
            Description = "Draft description",
            TtsScript = "Draft script",
            Status = PoiStatus.Draft,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var pendingPoi = new Poi
        {
            Name = "Pending POI",
            Slug = "pending-poi",
            OwnerId = owner.Id,
            Lat = 10.776,
            Lng = 106.703,
            Priority = 3,
            NarrationMode = NarrationMode.TtsOnly,
            Description = "Pending description",
            TtsScript = "Pending script",
            Status = PoiStatus.PendingReview,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        dbContext.Pois.AddRange(publishedPoi, draftPoi, pendingPoi);
        await dbContext.SaveChangesAsync();

        dbContext.AudioAssets.AddRange(
            new AudioAsset
            {
                PoiId = publishedPoi.Id,
                LanguageCode = "vi",
                SourceType = AudioSourceType.Tts,
                Provider = "test",
                StoragePath = "audio/published.mp3",
                Url = "https://cdn.test/audio/published.mp3",
                Status = AudioStatus.Ready,
                DurationSeconds = 14,
                GeneratedAt = DateTime.UtcNow.AddDays(-2)
            },
            new AudioAsset
            {
                PoiId = draftPoi.Id,
                LanguageCode = "en",
                SourceType = AudioSourceType.Tts,
                Provider = "test",
                StoragePath = "audio/draft.mp3",
                Url = "https://cdn.test/audio/draft.mp3",
                Status = AudioStatus.Ready,
                DurationSeconds = 18,
                GeneratedAt = DateTime.UtcNow.AddDays(-1)
            });

        dbContext.VisitEvents.AddRange(
            new VisitEvent
            {
                DeviceId = "device-alpha",
                PoiId = publishedPoi.Id,
                EventType = EventType.AudioPlay,
                Source = "owner-web",
                CreatedAt = DateTime.UtcNow.AddHours(-4)
            },
            new VisitEvent
            {
                DeviceId = "device-beta",
                PoiId = draftPoi.Id,
                EventType = EventType.QrScan,
                Source = "owner-web",
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            },
            new VisitEvent
            {
                DeviceId = "device-gamma",
                PoiId = pendingPoi.Id,
                EventType = EventType.GeofenceEnter,
                Source = "owner-web",
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            });

        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, owner.Id, unreadCount: 4);

        var actionResult = await controller.GetProfileAsync(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<OwnerProfileDto>>(okResult.Value);
        var profile = Assert.IsType<OwnerProfileDto>(response.Data);

        Assert.True(response.Succeeded);
        Assert.Equal(owner.Id, profile.UserId);
        Assert.Equal("Owner Profile", profile.FullName);
        Assert.Equal("owner-profile@narration.app", profile.Email);
        Assert.Equal("+84 90 123 4567", profile.Phone);
        Assert.Equal("District 1", profile.ManagedArea);
        Assert.Equal("en", profile.PreferredLanguage);
        Assert.Equal(owner.CreatedAtUtc, profile.CreatedAtUtc);
        Assert.Equal(owner.LastLoginAtUtc, profile.LastLoginAtUtc);
        Assert.Equal(3, profile.ActivitySummary.TotalPois);
        Assert.Equal(1, profile.ActivitySummary.PublishedPois);
        Assert.Equal(1, profile.ActivitySummary.DraftPois);
        Assert.Equal(1, profile.ActivitySummary.PendingReviewPois);
        Assert.Equal(2, profile.ActivitySummary.TotalAudioAssets);
        Assert.Equal(1, profile.ActivitySummary.TotalAudioPlays);
        Assert.Equal(4, profile.ActivitySummary.UnreadNotifications);
    }

    [Fact]
    public async Task PutProfileAsync_updates_full_name_phone_managed_area_and_preferred_language()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var owner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "owner-edit@narration.app");
        owner.FullName = "Original Owner";
        owner.Phone = "+84 90 111 2222";
        owner.ManagedArea = "Old District";
        owner.PreferredLanguage = "vi";
        owner.CreatedAtUtc = DateTime.UtcNow.AddDays(-12);
        owner.LastLoginAtUtc = DateTime.UtcNow.AddDays(-1);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, owner.Id, unreadCount: 0);
        var request = new UpdateOwnerProfileRequest
        {
            FullName = "  Updated Owner  ",
            Phone = "  +84 90 333 4444  ",
            ManagedArea = "  New District  ",
            PreferredLanguage = "  EN  "
        };

        var actionResult = await controller.UpdateProfileAsync(request, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<OwnerProfileDto>>(okResult.Value);
        var profile = Assert.IsType<OwnerProfileDto>(response.Data);

        var persistedOwner = await dbContext.AppUsers.SingleAsync(item => item.Id == owner.Id);

        Assert.True(response.Succeeded);
        Assert.Equal("Updated Owner", profile.FullName);
        Assert.Equal("+84 90 333 4444", profile.Phone);
        Assert.Equal("New District", profile.ManagedArea);
        Assert.Equal("en", profile.PreferredLanguage);
        Assert.Equal("Updated Owner", persistedOwner.FullName);
        Assert.Equal("+84 90 333 4444", persistedOwner.Phone);
        Assert.Equal("New District", persistedOwner.ManagedArea);
        Assert.Equal("en", persistedOwner.PreferredLanguage);
    }

    [Fact]
    public async Task PutProfileAsync_preserves_omitted_optional_fields_and_clears_blank_optional_fields()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var owner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "owner-partial@narration.app");
        owner.FullName = "Original Owner";
        owner.Phone = "+84 90 111 2222";
        owner.ManagedArea = "Old District";
        owner.PreferredLanguage = "vi";
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, owner.Id, unreadCount: 0);

        var preserveResult = await controller.UpdateProfileAsync(new UpdateOwnerProfileRequest
        {
            FullName = "Updated Owner",
            PreferredLanguage = "EN"
        }, CancellationToken.None);

        var preserveOkResult = Assert.IsType<OkObjectResult>(preserveResult.Result);
        var preserveResponse = Assert.IsType<ApiResponse<OwnerProfileDto>>(preserveOkResult.Value);
        var preservedProfile = Assert.IsType<OwnerProfileDto>(preserveResponse.Data);

        Assert.Equal("+84 90 111 2222", preservedProfile.Phone);
        Assert.Equal("Old District", preservedProfile.ManagedArea);

        var clearResult = await controller.UpdateProfileAsync(new UpdateOwnerProfileRequest
        {
            FullName = "Updated Owner",
            Phone = "   ",
            ManagedArea = "",
            PreferredLanguage = "en"
        }, CancellationToken.None);

        var clearOkResult = Assert.IsType<OkObjectResult>(clearResult.Result);
        var clearResponse = Assert.IsType<ApiResponse<OwnerProfileDto>>(clearOkResult.Value);
        var clearedProfile = Assert.IsType<OwnerProfileDto>(clearResponse.Data);

        var persistedOwner = await dbContext.AppUsers.SingleAsync(item => item.Id == owner.Id);

        Assert.Null(clearedProfile.Phone);
        Assert.Null(clearedProfile.ManagedArea);
        Assert.Null(persistedOwner.Phone);
        Assert.Null(persistedOwner.ManagedArea);
    }

    [Theory]
    [MemberData(nameof(InvalidProfileRequests))]
    public async Task PutProfileAsync_rejects_invalid_profile_values_without_persisting_changes(UpdateOwnerProfileRequest request)
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var owner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "owner-invalid@narration.app");
        owner.FullName = "Original Owner";
        owner.Phone = "+84 90 111 2222";
        owner.ManagedArea = "Old District";
        owner.PreferredLanguage = "vi";
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, owner.Id, unreadCount: 0);

        var actionResult = await controller.UpdateProfileAsync(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<OwnerProfileDto>>(badRequest.Value);
        var persistedOwner = await dbContext.AppUsers.SingleAsync(item => item.Id == owner.Id);

        Assert.False(response.Succeeded);
        Assert.Equal("invalid_owner_profile", response.Error?.Code);
        Assert.Equal("Original Owner", persistedOwner.FullName);
        Assert.Equal("+84 90 111 2222", persistedOwner.Phone);
        Assert.Equal("Old District", persistedOwner.ManagedArea);
        Assert.Equal("vi", persistedOwner.PreferredLanguage);
    }

    public static IEnumerable<object[]> InvalidProfileRequests()
    {
        yield return
        [
            new UpdateOwnerProfileRequest { FullName = "   ", PreferredLanguage = "en" }
        ];
        yield return
        [
            new UpdateOwnerProfileRequest { FullName = "Owner", PreferredLanguage = "   " }
        ];
        yield return
        [
            new UpdateOwnerProfileRequest { FullName = new string('A', 151), PreferredLanguage = "en" }
        ];
        yield return
        [
            new UpdateOwnerProfileRequest { FullName = "Owner", Phone = new string('1', 31), PreferredLanguage = "en" }
        ];
        yield return
        [
            new UpdateOwnerProfileRequest { FullName = "Owner", ManagedArea = new string('A', 251), PreferredLanguage = "en" }
        ];
        yield return
        [
            new UpdateOwnerProfileRequest { FullName = "Owner", PreferredLanguage = new string('a', 11) }
        ];
    }

    private static OwnerController CreateController(AppDbContext dbContext, Guid userId, int unreadCount)
    {
        var controller = new OwnerController(dbContext, new StubNotificationService(unreadCount));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, "poi_owner")
                ], "test"))
            }
        };

        return controller;
    }

    private sealed class StubNotificationService(int unreadCount) : INotificationService
    {
        public Task<NotificationDto> CreateAsync(Guid userId, NotificationType type, string title, string message, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<NotificationDto>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<NotificationDto>>([]);
        }

        public Task<UnreadCountDto> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new UnreadCountDto { Count = unreadCount });
        }

        public Task MarkReadAsync(Guid userId, int notificationId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(Guid userId, int notificationId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
