using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Notification;
using NarrationApp.Shared.Enums;
using NarrationApp.SharedUI.Components;
using NarrationApp.SharedUI.Services;

namespace NarrationApp.Web.Tests.Components;

public sealed class NotificationCenterTests : TestContext
{
    [Fact]
    public void Toggle_uses_bell_icon_instead_of_text_label()
    {
        Services.AddSingleton<INotificationCenterService>(new TestNotificationCenterService(Array.Empty<NotificationDto>()));

        var cut = RenderComponent<NotificationCenter>();
        var toggle = cut.Find("button[data-action='toggle']");

        Assert.Equal("Thông báo", toggle.GetAttribute("aria-label"));
        Assert.Contains("notification-center__icon", toggle.InnerHtml);
        Assert.DoesNotContain("Notifications", cut.Markup);
    }

    [Fact]
    public void Renders_unread_badge_and_notification_items_loaded_from_service()
    {
        Services.AddSingleton<INotificationCenterService>(new TestNotificationCenterService(
        [
            new NotificationDto
            {
                Id = 1,
                UserId = Guid.NewGuid(),
                Type = NotificationType.TourPublished,
                Title = "Tour mới",
                Message = "Hành trình Quận 4 đã xuất bản",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
            },
            new NotificationDto
            {
                Id = 2,
                UserId = Guid.NewGuid(),
                Type = NotificationType.AudioReady,
                Title = "Audio sẵn sàng",
                Message = "Bản audio tiếng Việt đã tạo xong",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-2)
            }
        ]));

        var cut = RenderComponent<NotificationCenter>();
        cut.Find("button[data-action='toggle']").Click();
        var toggle = cut.Find("button[data-action='toggle']");
        var panel = cut.Find(".notification-center__panel");

        Assert.Contains("2", cut.Markup);
        Assert.Contains("Tour mới", cut.Markup);
        Assert.Contains("Audio sẵn sàng", cut.Markup);
        Assert.Equal("true", toggle.GetAttribute("aria-expanded"));
        Assert.Equal("dialog", panel.GetAttribute("role"));
    }

    [Fact]
    public void Clicking_mark_all_read_invokes_service_and_clears_badge()
    {
        var service = new TestNotificationCenterService(
        [
            new NotificationDto
            {
                Id = 3,
                UserId = Guid.NewGuid(),
                Type = NotificationType.ModerationResult,
                Title = "Đã duyệt",
                Message = "POI của bạn đã được phê duyệt",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10)
            }
        ]);
        Services.AddSingleton<INotificationCenterService>(service);

        var cut = RenderComponent<NotificationCenter>();
        cut.Find("button[data-action='toggle']").Click();
        cut.Find("button[data-action='mark-all']").Click();

        Assert.True(service.MarkAllReadCalled);
        Assert.DoesNotContain("notification-center__badge", cut.Markup);
    }

    private sealed class TestNotificationCenterService(IReadOnlyList<NotificationDto> seedItems) : INotificationCenterService
    {
        private List<NotificationDto> _items = seedItems.ToList();

        public event Action? Changed;

        public bool MarkAllReadCalled { get; private set; }

        public ValueTask<IReadOnlyList<NotificationDto>> GetAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyList<NotificationDto>>(_items);
        }

        public ValueTask<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(_items.Count(item => !item.IsRead));
        }

        public ValueTask MarkAllReadAsync(CancellationToken cancellationToken = default)
        {
            MarkAllReadCalled = true;
            _items = _items
                .Select(item => new NotificationDto
                {
                    Id = item.Id,
                    UserId = item.UserId,
                    Type = item.Type,
                    Title = item.Title,
                    Message = item.Message,
                    IsRead = true,
                    CreatedAtUtc = item.CreatedAtUtc
                })
                .ToList();

            Changed?.Invoke();
            return ValueTask.CompletedTask;
        }

        public ValueTask MarkReadAsync(int notificationId, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
