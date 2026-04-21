using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Notification;
using NarrationApp.Shared.Enums;
using NarrationApp.SharedUI.Auth;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Services;

public sealed class NotificationCenterApiServiceTests
{
    [Fact]
    public async Task GetAsync_returns_empty_for_anonymous_users_and_disconnects_realtime_client()
    {
        var store = new TestAuthSessionStore();
        var realtimeService = new TestNotificationRealtimeService();
        var apiClient = new ApiClient(new HttpClient(new ThrowingMessageHandler())
        {
            BaseAddress = new Uri("https://localhost:5001/")
        }, store);

        var sut = new NotificationCenterApiService(apiClient, store, realtimeService);

        var items = await sut.GetAsync();

        Assert.Empty(items);
        Assert.Equal(1, realtimeService.DisconnectCalls);
        Assert.Equal(0, realtimeService.ConnectCalls);
    }

    [Fact]
    public async Task GetAsync_connects_realtime_client_and_push_event_raises_changed()
    {
        var store = new TestAuthSessionStore
        {
            Session = new AuthSession
            {
                UserId = Guid.NewGuid(),
                Email = "owner@narration.app",
                Role = UserRole.PoiOwner,
                PreferredLanguage = "vi",
                Token = "jwt-token"
            }
        };
        var realtimeService = new TestNotificationRealtimeService();
        var apiClient = new ApiClient(new HttpClient(new StubNotificationMessageHandler())
        {
            BaseAddress = new Uri("https://localhost:5001/")
        }, store);

        var sut = new NotificationCenterApiService(apiClient, store, realtimeService);
        var changedRaised = false;
        sut.Changed += () => changedRaised = true;

        var items = await sut.GetAsync();
        realtimeService.RaiseNotification(new NotificationDto
        {
            Id = 19,
            UserId = store.Session!.UserId,
            Type = NotificationType.System,
            Title = "Realtime notification",
            Message = "SignalR vừa đẩy một sự kiện mới.",
            CreatedAtUtc = DateTime.UtcNow
        });

        Assert.Single(items);
        Assert.Equal(1, realtimeService.ConnectCalls);
        Assert.True(changedRaised);
    }

    [Fact]
    public async Task GetAsync_returns_empty_when_realtime_negotiation_is_unauthorized()
    {
        var store = new TestAuthSessionStore
        {
            Session = new AuthSession
            {
                UserId = Guid.NewGuid(),
                Email = "admin@narration.app",
                Role = UserRole.Admin,
                PreferredLanguage = "vi",
                Token = "expired-token"
            }
        };
        var realtimeService = new TestNotificationRealtimeService
        {
            ConnectException = new HttpRequestException(
                "Unauthorized negotiate request.",
                inner: null,
                statusCode: HttpStatusCode.Unauthorized)
        };
        var apiClient = new ApiClient(new HttpClient(new ThrowingMessageHandler())
        {
            BaseAddress = new Uri("https://localhost:5001/")
        }, store);

        var sut = new NotificationCenterApiService(apiClient, store, realtimeService);

        var items = await sut.GetAsync();

        Assert.Empty(items);
        Assert.Equal(1, realtimeService.ConnectCalls);
        Assert.Equal(1, realtimeService.DisconnectCalls);
    }

    [Fact]
    public async Task GetAsync_returns_empty_when_notifications_endpoint_is_unauthorized()
    {
        var store = new TestAuthSessionStore
        {
            Session = new AuthSession
            {
                UserId = Guid.NewGuid(),
                Email = "admin@narration.app",
                Role = UserRole.Admin,
                PreferredLanguage = "vi",
                Token = "expired-token"
            }
        };
        var realtimeService = new TestNotificationRealtimeService();
        var apiClient = new ApiClient(new HttpClient(new UnauthorizedNotificationMessageHandler())
        {
            BaseAddress = new Uri("https://localhost:5001/")
        }, store);

        var sut = new NotificationCenterApiService(apiClient, store, realtimeService);

        var items = await sut.GetAsync();

        Assert.Empty(items);
        Assert.Equal(1, realtimeService.ConnectCalls);
        Assert.Equal(1, realtimeService.DisconnectCalls);
    }

    private sealed class TestAuthSessionStore : IAuthSessionStore
    {
        public AuthSession? Session { get; set; }

        public ValueTask<AuthSession?> GetAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(Session);
        }

        public ValueTask SetAsync(AuthSession session, CancellationToken cancellationToken = default)
        {
            Session = session;
            return ValueTask.CompletedTask;
        }

        public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            Session = null;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestNotificationRealtimeService : INotificationRealtimeService
    {
        public event Action<NotificationDto>? NotificationReceived;

        public int ConnectCalls { get; private set; }

        public int DisconnectCalls { get; private set; }

        public Exception? ConnectException { get; init; }

        public ValueTask EnsureConnectedAsync(CancellationToken cancellationToken = default)
        {
            ConnectCalls++;
            if (ConnectException is not null)
            {
                return ValueTask.FromException(ConnectException);
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
        {
            DisconnectCalls++;
            return ValueTask.CompletedTask;
        }

        public void RaiseNotification(NotificationDto notification)
        {
            NotificationReceived?.Invoke(notification);
        }
    }

    private sealed class ThrowingMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("HTTP should not be called for anonymous notification center access.");
        }
    }

    private sealed class StubNotificationMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = request.RequestUri?.AbsolutePath switch
            {
                "/api/notifications" => Envelope(new[]
                {
                    new NotificationDto
                    {
                        Id = 9,
                        UserId = Guid.NewGuid(),
                        Type = NotificationType.AudioReady,
                        Title = "Audio ready",
                        Message = "Audio tiếng Việt đã sẵn sàng.",
                        CreatedAtUtc = DateTime.UtcNow.AddMinutes(-3)
                    }
                }),
                "/api/notifications/unread-count" => Envelope(new UnreadCountDto { Count = 1 }),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };

            return Task.FromResult(response);
        }

        private static HttpResponseMessage Envelope<T>(T data)
        {
            var message = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ApiResponse<T>
                {
                    Succeeded = true,
                    Message = "ok",
                    Data = data
                }, options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
            };

            return message;
        }
    }

    private sealed class UnauthorizedNotificationMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        }
    }
}
