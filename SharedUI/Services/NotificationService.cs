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

        public event Action? OnNotificationsChanged;
        public List<NotificationDto> Notifications { get; private set; } = new();
        public int UnreadCount { get; private set; }

        public NotificationService(HttpClient httpClient, IAuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            var token = await _authService.GetTokenAsync();
            if (string.IsNullOrEmpty(token)) return;

            // Lấy hub URL từ HttpClient.BaseAddress (cùng server với API)
            var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "https://localhost:7214";
            var hubUrl = $"{baseUrl}/hubs/notification";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(token)!;
                })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                .ConfigureLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Debug);
                    logging.AddProvider(new CustomConsoleLoggerProvider());
                })
                .Build();

            // Lắng nghe thông báo mới
            _hubConnection.On<NotificationDto>("ReceiveNotification", notification =>
            {
                Notifications.Insert(0, notification);
                UnreadCount++;
                OnNotificationsChanged?.Invoke();
            });

            // Xử lý reconnect: tải lại thông báo khi kết nối lại
            _hubConnection.Reconnecting += (error) =>
            {
                Console.WriteLine($"[SignalR] Đang kết nối lại... {error?.Message}");
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += async (connectionId) =>
            {
                Console.WriteLine($"[SignalR] Đã kết nối lại: {connectionId}");
                // Reload notifications sau khi reconnect để không bỏ lỡ
                await LoadNotificationsAsync();
            };

            _hubConnection.Closed += async (error) =>
            {
                Console.WriteLine($"[SignalR] Mất kết nối: {error?.Message}");
                // Thử kết nối lại sau 5 giây nếu connection bị đóng hoàn toàn
                await Task.Delay(5000);
                try
                {
                    await _hubConnection.StartAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SignalR] Kết nối lại thất bại: {ex.Message}");
                }
            };

            try
            {
                await _hubConnection.StartAsync();
                _isInitialized = true;
                Console.WriteLine("[SignalR] Kết nối thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Kết nối thất bại: {ex.Message}");
            }

            // Load thông báo có sẵn từ API
            await LoadNotificationsAsync();
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
                Console.WriteLine($"[SignalR] Load notifications thất bại: {ex.Message}");
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
            if (_hubConnection != null)
            {
                _hubConnection.Reconnecting -= null;
                _hubConnection.Reconnected -= null;
                _hubConnection.Closed -= null;
                await _hubConnection.DisposeAsync();
            }
        }
    }

    public class CustomConsoleLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CustomConsoleLogger();
        public void Dispose() { }
    }

    public class CustomConsoleLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Console.WriteLine($"[SignalR Client] {logLevel}: {formatter(state, exception)}");
        }
    }
}
