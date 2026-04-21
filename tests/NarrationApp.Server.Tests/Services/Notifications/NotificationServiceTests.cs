using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Services.Notifications;

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task CreateAndMarkReadAsync_updates_unread_count()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var user = await dbContext.AppUsers.FirstAsync();
        var sut = new NotificationService(dbContext, new NullNotificationBroadcaster());

        var notification = await sut.CreateAsync(user.Id, NotificationType.System, "Hello", "World");
        var unreadBefore = await sut.GetUnreadCountAsync(user.Id);
        await sut.MarkReadAsync(user.Id, notification.Id);
        var unreadAfter = await sut.GetUnreadCountAsync(user.Id);

        Assert.Equal(1, unreadBefore.Count);
        Assert.Equal(0, unreadAfter.Count);
    }
}
