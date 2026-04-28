using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Notification;
using NarrationApp.Shared.Enums;
using NarrationApp.SharedUI.Services;
using NarrationApp.Web.Pages.Owner;

namespace NarrationApp.Web.Tests.Pages.Owner;

public sealed class NotificationsTests : TestContext
{
    [Fact]
    public void Notifications_behavior_is_split_into_focused_partials()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var pageRoot = Path.Combine(projectRoot, "src", "NarrationApp.Web", "Pages", "Owner");
        var markupPath = Path.Combine(pageRoot, "Notifications.razor");
        var expectedPartials = new[]
        {
            ("Notifications.razor.cs", "OnInitializedAsync"),
            ("Notifications.Actions.razor.cs", "MarkAllReadAsync"),
            ("Notifications.Presentation.razor.cs", "GetNotificationTypeLabel")
        };

        var markup = File.ReadAllText(markupPath);
        Assert.DoesNotContain("@code", markup, StringComparison.Ordinal);

        foreach (var (fileName, marker) in expectedPartials)
        {
            var path = Path.Combine(pageRoot, fileName);
            Assert.True(File.Exists(path), $"{fileName} should exist.");
            var source = File.ReadAllText(path);
            Assert.Contains("partial class Notifications", source, StringComparison.Ordinal);
            Assert.Contains(marker, source, StringComparison.Ordinal);
        }

        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "Notifications.razor.cs")).Length <= 80);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "Notifications.Actions.razor.cs")).Length <= 90);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "Notifications.Presentation.razor.cs")).Length <= 90);
    }

    [Fact]
    public void Notifications_page_renders_unread_summary_filters_and_items()
    {
        Services.AddSingleton<INotificationCenterService>(new TestNotificationCenterService(BuildNotifications()));

        var cut = RenderComponent<Notifications>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Notifications workspace", cut.Markup);
            Assert.Contains("2 chưa đọc", cut.Markup);
            Assert.Contains("POI cần chỉnh", cut.Markup);
            Assert.Contains("Audio đã sẵn sàng", cut.Markup);
            Assert.Contains("Tour mới xuất bản", cut.Markup);
        });

        cut.Find("select[data-field='notification-read-filter']").Change("unread");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("POI cần chỉnh", cut.Markup);
            Assert.Contains("Audio đã sẵn sàng", cut.Markup);
            Assert.DoesNotContain("Tour mới xuất bản", cut.Markup);
        });

        cut.Find("select[data-field='notification-type-filter']").Change(NotificationType.AudioReady.ToString());

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("POI cần chỉnh", cut.Markup);
            Assert.Contains("Audio đã sẵn sàng", cut.Markup);
        });
    }

    [Fact]
    public void Notifications_page_marks_single_item_and_all_items_read()
    {
        var service = new TestNotificationCenterService(BuildNotifications());
        Services.AddSingleton<INotificationCenterService>(service);

        var cut = RenderComponent<Notifications>();

        cut.WaitForAssertion(() => Assert.Contains("2 chưa đọc", cut.Markup));
        cut.Find("button[data-action='mark-read-1']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal([1], service.MarkReadIds);
            Assert.Contains("Đã đánh dấu POI cần chỉnh là đã đọc.", cut.Markup);
            Assert.Contains("1 chưa đọc", cut.Markup);
        });

        cut.Find("button[data-action='mark-all-read']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(service.MarkAllReadCalled);
            Assert.Contains("0 chưa đọc", cut.Markup);
            Assert.Contains("Đã đánh dấu tất cả thông báo là đã đọc.", cut.Markup);
        });
    }

    [Fact]
    public void Notifications_page_refreshes_when_realtime_service_changes()
    {
        var service = new TestNotificationCenterService(Array.Empty<NotificationDto>());
        Services.AddSingleton<INotificationCenterService>(service);

        var cut = RenderComponent<Notifications>();

        cut.WaitForAssertion(() => Assert.Contains("Chưa có thông báo", cut.Markup));

        service.Push(new NotificationDto
        {
            Id = 99,
            UserId = Guid.NewGuid(),
            Type = NotificationType.System,
            Title = "Thông báo hệ thống",
            Message = "Có cập nhật vận hành mới.",
            CreatedAtUtc = DateTime.UtcNow
        });

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Thông báo hệ thống", cut.Markup);
            Assert.Contains("1 chưa đọc", cut.Markup);
        });
    }

    private static IReadOnlyList<NotificationDto> BuildNotifications() =>
    [
        new NotificationDto
        {
            Id = 1,
            UserId = Guid.NewGuid(),
            Type = NotificationType.ModerationResult,
            Title = "POI cần chỉnh",
            Message = "Admin yêu cầu bổ sung mô tả nguồn.",
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
        },
        new NotificationDto
        {
            Id = 2,
            UserId = Guid.NewGuid(),
            Type = NotificationType.AudioReady,
            Title = "Audio đã sẵn sàng",
            Message = "Audio tiếng Việt cho POI #1 đã tạo xong.",
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-3)
        },
        new NotificationDto
        {
            Id = 3,
            UserId = Guid.NewGuid(),
            Type = NotificationType.TourPublished,
            Title = "Tour mới xuất bản",
            Message = "Tour Food Street đã xuất bản.",
            IsRead = true,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1)
        }
    ];

    private sealed class TestNotificationCenterService(IReadOnlyList<NotificationDto> seedItems) : INotificationCenterService
    {
        private List<NotificationDto> _items = seedItems.ToList();

        public event Action? Changed;

        public List<int> MarkReadIds { get; } = [];

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
            _items = _items.Select(item => Clone(item, true)).ToList();
            Changed?.Invoke();
            return ValueTask.CompletedTask;
        }

        public ValueTask MarkReadAsync(int notificationId, CancellationToken cancellationToken = default)
        {
            MarkReadIds.Add(notificationId);
            _items = _items.Select(item => item.Id == notificationId ? Clone(item, true) : item).ToList();
            Changed?.Invoke();
            return ValueTask.CompletedTask;
        }

        public void Push(NotificationDto item)
        {
            _items.Insert(0, item);
            Changed?.Invoke();
        }

        private static NotificationDto Clone(NotificationDto item, bool isRead)
        {
            return new NotificationDto
            {
                Id = item.Id,
                UserId = item.UserId,
                Type = item.Type,
                Title = item.Title,
                Message = item.Message,
                IsRead = isRead,
                CreatedAtUtc = item.CreatedAtUtc
            };
        }
    }
}
