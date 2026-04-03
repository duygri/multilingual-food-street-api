using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;
using FoodStreet.Client.DTOs;

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
        event Action<RealtimeActivityDto>? OnRealtimeActivity;
        List<NotificationDto> Notifications { get; }
        int UnreadCount { get; }
        string ConnectionStatus { get; }
        RealtimeActivityDto? LastActivity { get; }
        Task InitializeAsync();
        Task LoadNotificationsAsync();
        Task MarkAsReadAsync(int id);
        Task MarkAllAsReadAsync();
        Task DeleteAsync(int id);
        Task ClearReadAsync();
    }

    public class NotificationService : INotificationService
    {
        private const string NotificationApiBase = "api/notifications";
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private HubConnection? _hubConnection;
        private bool _isInitialized;
        private System.Threading.Timer? _pollingTimer;


        public event Action? OnNotificationsChanged;
        public event Action<RealtimeActivityDto>? OnRealtimeActivity;
        public List<NotificationDto> Notifications { get; private set; } = new();
        public int UnreadCount { get; private set; }
        public string ConnectionStatus { get; private set; } = "⏳";
        public RealtimeActivityDto? LastActivity { get; private set; }

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

                RegisterRealtimeHandler(RealtimeEventNames.PoiUpdated);
                RegisterRealtimeHandler(RealtimeEventNames.MenuUpdated, reloadNotifications: true);
                RegisterRealtimeHandler(RealtimeEventNames.ModerationChanged, reloadNotifications: true);
                RegisterRealtimeHandler(RealtimeEventNames.AudioReady, reloadNotifications: true);
                RegisterRealtimeHandler(RealtimeEventNames.TranslationUpdated);
                RegisterRealtimeHandler(RealtimeEventNames.TourPublished);
                RegisterRealtimeHandler(RealtimeEventNames.QrScanned);

                _hubConnection.Reconnecting += (_) =>
                {

                    ConnectionStatus = "🟡";
                    OnNotificationsChanged?.Invoke();
                    return Task.CompletedTask;
                };

                _hubConnection.Reconnected += async (_) =>
                {

                    ConnectionStatus = "🟢";
                    OnNotificationsChanged?.Invoke();
                    await LoadNotificationsAsync();
                };

                _hubConnection.Closed += (_) =>
                {

                    ConnectionStatus = "🔴";
                    OnNotificationsChanged?.Invoke();
                    return Task.CompletedTask;
                };

                await _hubConnection.StartAsync();

                ConnectionStatus = "🟢";
                Console.WriteLine("[Notification] SignalR connected OK");
                OnNotificationsChanged?.Invoke();
            }
            catch (Exception ex)
            {

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

        private void RegisterRealtimeHandler(string eventName, bool reloadNotifications = false)
        {
            _hubConnection?.On<RealtimeActivityDto>(eventName, async activity =>
            {
                activity.EventName = string.IsNullOrWhiteSpace(activity.EventName) ? eventName : activity.EventName;
                await HandleRealtimeActivityAsync(activity, reloadNotifications);
            });
        }

        private async Task HandleRealtimeActivityAsync(RealtimeActivityDto activity, bool reloadNotifications)
        {
            LastActivity = activity;

            if (reloadNotifications)
            {
                await LoadNotificationsAsync();
            }

            OnRealtimeActivity?.Invoke(activity);
            OnNotificationsChanged?.Invoke();
        }

        private async Task PollNotifications()
        {
            try
            {
                var previousCount = Notifications.Count;
                var previousUnread = UnreadCount;
                var notifications = await _httpClient.GetFromJsonAsync<List<NotificationDto>>(NotificationApiBase);
                if (notifications != null)
                {
                    Notifications = notifications;

                    var countResponse = await _httpClient.GetFromJsonAsync<int>($"{NotificationApiBase}/unread-count");
                    UnreadCount = countResponse;

                    // Chỉ notify UI nếu có thay đổi
                    if (UnreadCount != previousUnread || notifications.Count != previousCount)
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
                var notifications = await _httpClient.GetFromJsonAsync<List<NotificationDto>>(NotificationApiBase);
                if (notifications != null)
                {
                    Notifications = notifications;
                }

                var countResponse = await _httpClient.GetFromJsonAsync<int>($"{NotificationApiBase}/unread-count");
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
                await _httpClient.PostAsync($"{NotificationApiBase}/{id}/read", null);
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
                await _httpClient.PostAsync($"{NotificationApiBase}/read-all", null);
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
                await _httpClient.DeleteAsync($"{NotificationApiBase}/{id}");
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
                await _httpClient.DeleteAsync($"{NotificationApiBase}/clear-read");
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
