using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace FoodStreet.Client.Services
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Type { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? RelatedId { get; set; }
        public string? SenderName { get; set; }
    }

    public interface INotificationService : IAsyncDisposable
    {
        event Action? OnNotificationsChanged;
        List<NotificationDto> Notifications { get; }
        int UnreadCount { get; }
        string ConnectionStatus { get; }
        Task InitializeAsync();
        Task LoadNotificationsAsync();
        Task MarkAsReadAsync(int id);
        Task MarkAllAsReadAsync();
        Task DeleteAsync(int id);
        Task ClearReadAsync();
    }

    public class NotificationService : INotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private HubConnection? _hubConnection;
        private bool _isInitialized;
        private System.Threading.Timer? _pollingTimer;
        private bool _signalRConnected;

        public event Action? OnNotificationsChanged;
        public List<NotificationDto> Notifications { get; private set; } = new();
        public int UnreadCount { get; private set; }
        public string ConnectionStatus { get; private set; } = "⏳";

        public NotificationService(HttpClient httpClient, IAuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            var token = await _authService.GetTokenAsync();
            if (string.IsNullOrEmpty(token)) return;

            // Load thông báo có sẵn từ API ngay lập tức
            await LoadNotificationsAsync();

            // Thử kết nối SignalR (không block nếu fail)
            _ = TryConnectSignalR(token);

            // Luôn bật polling fallback (mỗi 15 giây)
            StartPolling();
        }

        private async Task TryConnectSignalR(string token)
        {
            try
            {
                var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "https://localhost:7214";
                var hubUrl = $"{baseUrl}/hubs/notification";

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(token)!;
                    })
                    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                    .Build();

                // Lắng nghe thông báo mới realtime
                _hubConnection.On<NotificationDto>("ReceiveNotification", notification =>
                {
                    // Tránh trùng lặp
                    if (!Notifications.Any(n => n.Id == notification.Id))
                    {
                        Notifications.Insert(0, notification);
                        UnreadCount++;
                        OnNotificationsChanged?.Invoke();
                    }
                });

                _hubConnection.Reconnecting += (_) =>
                {
                    _signalRConnected = false;
                    ConnectionStatus = "🟡";
                    OnNotificationsChanged?.Invoke();
                    return Task.CompletedTask;
                };

                _hubConnection.Reconnected += async (_) =>
                {
                    _signalRConnected = true;
                    ConnectionStatus = "🟢";
                    OnNotificationsChanged?.Invoke();
                    await LoadNotificationsAsync();
                };

                _hubConnection.Closed += (_) =>
                {
                    _signalRConnected = false;
                    ConnectionStatus = "🔴";
                    OnNotificationsChanged?.Invoke();
                    return Task.CompletedTask;
                };

                await _hubConnection.StartAsync();
                _signalRConnected = true;
                ConnectionStatus = "🟢";
                Console.WriteLine("[Notification] SignalR connected OK");
                OnNotificationsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                _signalRConnected = false;
                ConnectionStatus = "🔴 Polling";
                Console.WriteLine($"[Notification] SignalR failed, using polling: {ex.Message}");
                OnNotificationsChanged?.Invoke();
            }
        }

        private void StartPolling()
        {
            _pollingTimer = new System.Threading.Timer(
                async _ => await PollNotifications(),
                null,
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(15)
            );
        }

        private async Task PollNotifications()
        {
            try
            {
                var notifications = await _httpClient.GetFromJsonAsync<List<NotificationDto>>("api/notification");
                if (notifications != null)
                {
                    // Kiểm tra có thông báo mới không
                    var oldCount = UnreadCount;
                    Notifications = notifications;

                    var countResponse = await _httpClient.GetFromJsonAsync<int>("api/notification/unread-count");
                    UnreadCount = countResponse;

                    // Chỉ notify UI nếu có thay đổi
                    if (UnreadCount != oldCount || notifications.Count != Notifications.Count)
                    {
                        OnNotificationsChanged?.Invoke();
                    }
                }
            }
            catch
            {
                // Ignore polling errors silently
            }
        }

        public async Task LoadNotificationsAsync()
        {
            try
            {
                var notifications = await _httpClient.GetFromJsonAsync<List<NotificationDto>>("api/notification");
                if (notifications != null)
                {
                    Notifications = notifications;
                }

                var countResponse = await _httpClient.GetFromJsonAsync<int>("api/notification/unread-count");
                UnreadCount = countResponse;

                OnNotificationsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Notification] Load failed: {ex.Message}");
            }
        }

        public async Task MarkAsReadAsync(int id)
        {
            try
            {
                await _httpClient.PostAsync($"api/notification/{id}/read", null);
                var notification = Notifications.FirstOrDefault(n => n.Id == id);
                if (notification != null && !notification.IsRead)
                {
                    notification.IsRead = true;
                    UnreadCount = Math.Max(0, UnreadCount - 1);
                    OnNotificationsChanged?.Invoke();
                }
            }
            catch { }
        }

        public async Task MarkAllAsReadAsync()
        {
            try
            {
                await _httpClient.PostAsync("api/notification/read-all", null);
                foreach (var n in Notifications) n.IsRead = true;
                UnreadCount = 0;
                OnNotificationsChanged?.Invoke();
            }
            catch { }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                await _httpClient.DeleteAsync($"api/notification/{id}");
                var notification = Notifications.FirstOrDefault(n => n.Id == id);
                if (notification != null)
                {
                    if (!notification.IsRead) UnreadCount = Math.Max(0, UnreadCount - 1);
                    Notifications.Remove(notification);
                    OnNotificationsChanged?.Invoke();
                }
            }
            catch { }
        }

        public async Task ClearReadAsync()
        {
            try
            {
                await _httpClient.DeleteAsync("api/notification/clear-read");
                Notifications.RemoveAll(n => n.IsRead);
                OnNotificationsChanged?.Invoke();
            }
            catch { }
        }

        public async ValueTask DisposeAsync()
        {
            _pollingTimer?.Dispose();
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}
