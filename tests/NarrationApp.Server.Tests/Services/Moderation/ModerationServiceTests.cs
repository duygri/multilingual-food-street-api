using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Services.Moderation;

public sealed class ModerationServiceTests
{
    [Fact]
    public async Task ReviewAsync_approves_request_and_creates_notification_for_requester()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var owner = await dbContext.AppUsers.SingleAsync(user => user.Email == "owner@narration.app");
        var admin = await dbContext.AppUsers.SingleAsync(user => user.Email == "admin@narration.app");
        var notificationService = new NotificationService(dbContext, new NullNotificationBroadcaster());
        var sut = new ModerationService(dbContext, notificationService);

        var created = await sut.CreateAsync(owner.Id, new CreateModerationRequest
        {
            EntityType = "poi",
            EntityId = "1"
        });

        var reviewed = await sut.ReviewAsync(created.Id, admin.Id, true, "Approved");

        Assert.Equal(ModerationStatus.Approved, reviewed.Status);
        Assert.Equal(1, await dbContext.Notifications.CountAsync(item => item.UserId == owner.Id));
    }

    [Fact]
    public async Task GetByRequesterAsync_returns_requests_for_the_given_owner()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var owner = await dbContext.AppUsers.SingleAsync(user => user.Email == "owner@narration.app");
        var anotherOwner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "owner2@narration.app");
        var notificationService = new NotificationService(dbContext, new NullNotificationBroadcaster());
        var sut = new ModerationService(dbContext, notificationService);

        await sut.CreateAsync(owner.Id, new CreateModerationRequest { EntityType = "poi", EntityId = "1" });
        await sut.CreateAsync(owner.Id, new CreateModerationRequest { EntityType = "audio", EntityId = "2" });
        await sut.CreateAsync(anotherOwner.Id, new CreateModerationRequest { EntityType = "translation", EntityId = "3" });

        var result = await sut.GetByRequesterAsync(owner.Id);

        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Equal(owner.Id, item.RequestedBy));
    }

    [Fact]
    public async Task ReviewAsync_for_owner_registration_updates_owner_activation_state()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var admin = await dbContext.AppUsers.SingleAsync(user => user.Email == "admin@narration.app");
        var candidate = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "candidate-owner@narration.app");
        candidate.IsActive = false;
        dbContext.ModerationRequests.Add(new Server.Data.Entities.ModerationRequest
        {
            EntityType = "owner_registration",
            EntityId = candidate.Id.ToString(),
            Status = ModerationStatus.Pending,
            RequestedBy = candidate.Id,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var notificationService = new NotificationService(dbContext, new NullNotificationBroadcaster());
        var sut = new ModerationService(dbContext, notificationService);
        var requestId = await dbContext.ModerationRequests
            .Where(item => item.EntityType == "owner_registration" && item.EntityId == candidate.Id.ToString())
            .Select(item => item.Id)
            .SingleAsync();

        await sut.ReviewAsync(requestId, admin.Id, true, "Approved owner registration.");

        var updatedOwner = await dbContext.AppUsers.SingleAsync(user => user.Id == candidate.Id);
        Assert.True(updatedOwner.IsActive);
    }
}
