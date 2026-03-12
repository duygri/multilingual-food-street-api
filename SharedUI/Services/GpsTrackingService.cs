using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace FoodStreet.Client.Services
{
    public class GpsTrackingService : IGpsTrackingService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly HttpClient _httpClient;
        private readonly ITtsService _ttsService;
        private DotNetObjectReference<GpsTrackingService>? _dotNetRef;
        
        // State
        public bool IsTracking { get; private set; }
        public double? CurrentLatitude { get; private set; }
        public double? CurrentLongitude { get; private set; }
        public double? Accuracy { get; private set; }
        public string? LastError { get; private set; }
        public string SessionId { get; private set; }

        // === THÊM MỚI: Anti-spam ===
        // Lưu POI nào đã trigger + thời điểm trigger
        private readonly Dictionary<int, DateTime> _triggeredPois = new();
        
        // Cooldown: 5 phút không phát lại cùng POI
        private readonly TimeSpan _cooldownDuration = TimeSpan.FromMinutes(5);
        
        // Lưu danh sách POI đang ở trong (để detect Enter/Exit)
        private readonly HashSet<int> _currentGeofencePois = new();

        // Events
        public event Action<double, double>? OnPositionUpdated;
        public event Action<List<NearbyPoiDto>>? OnGeofenceEntered;
        public event Action<string>? OnError;

        public GpsTrackingService(IJSRuntime jsRuntime, HttpClient httpClient, ITtsService ttsService)
        {
            _jsRuntime = jsRuntime;
            _httpClient = httpClient;
            _ttsService = ttsService;
            SessionId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Bắt đầu theo dõi GPS
        /// </summary>
        public async Task StartTrackingAsync()
        {
            if (IsTracking) return;

            _dotNetRef = DotNetObjectReference.Create(this);
            var result = await _jsRuntime.InvokeAsync<bool>("GpsTracker.start", _dotNetRef);
            
            if (result)
            {
                IsTracking = true;
                LastError = null;
            }
        }

        /// <summary>
        /// Dừng theo dõi GPS
        /// </summary>
        public async Task StopTrackingAsync()
        {
            if (!IsTracking) return;

            await _jsRuntime.InvokeVoidAsync("GpsTracker.stop");
            IsTracking = false;
        }

        /// <summary>
        /// Lấy vị trí một lần
        /// </summary>
        public async Task GetCurrentPositionAsync()
        {
            _dotNetRef ??= DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync("GpsTracker.getCurrentPosition", _dotNetRef);
        }

        /// <summary>
        /// Callback từ JavaScript khi vị trí thay đổi
        /// </summary>
        [JSInvokable]
        public async Task OnPositionChanged(double latitude, double longitude, double accuracy, double speed)
        {
            CurrentLatitude = latitude;
            CurrentLongitude = longitude;
            Accuracy = accuracy;

            // Gửi lên server
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/gps/update", new
                {
                    sessionId = SessionId,
                    latitude,
                    longitude,
                    accuracy,
                    speed
                });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GpsUpdateResponse>();
                    
                    if (result?.EnteredPois != null)
                    {
                        // Lọc ra những POI THỰC SỰ MỚI vào (chưa trigger hoặc đã hết cooldown)
                        var newlyEntered = new List<NearbyPoiDto>();
                        var now = DateTime.UtcNow;

                        foreach (var poi in result.EnteredPois)
                        {
                            // Kiểm tra cooldown
                            if (_triggeredPois.TryGetValue(poi.Id, out var lastTriggered))
                            {
                                // Đã trigger trước đó, check hết cooldown chưa
                                if (now - lastTriggered < _cooldownDuration)
                                {
                                    // Còn trong cooldown → BỎ QUA
                                    continue;
                                }
                            }

                            // POI mới hoặc đã hết cooldown → cho phép trigger
                            newlyEntered.Add(poi);
                            _triggeredPois[poi.Id] = now; // Ghi nhận thời điểm trigger
                        }

                        // Chỉ invoke event nếu có POI mới thực sự
                        if (newlyEntered.Count > 0)
                        {
                            // Xem TTS
                            foreach (var poi in newlyEntered)
                            {
                                if (!string.IsNullOrWhiteSpace(poi.Description))
                                    _ = _ttsService.PlayTextAsync($"Bạn vừa đến {poi.Name}. {poi.Description}");
                                else
                                    _ = _ttsService.PlayTextAsync($"Bạn vừa đến {poi.Name}");
                            }

                            OnGeofenceEntered?.Invoke(newlyEntered);
                        }

                        // Cập nhật danh sách POI hiện tại
                        _currentGeofencePois.Clear();
                        foreach (var poi in result.EnteredPois)
                        {
                            _currentGeofencePois.Add(poi.Id);
                        }

                        // Dọn dẹp: xóa cooldown cũ quá 30 phút (tránh leak memory)
                        var expiredKeys = _triggeredPois
                            .Where(kv => now - kv.Value > TimeSpan.FromMinutes(30))
                            .Select(kv => kv.Key)
                            .ToList();
                        foreach (var key in expiredKeys)
                        {
                            _triggeredPois.Remove(key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GPS] API error: {ex.Message}");
            }

            OnPositionUpdated?.Invoke(latitude, longitude);
        }

        /// <summary>
        /// Callback từ JavaScript khi có lỗi GPS
        /// </summary>
        [JSInvokable]
        public void OnGpsError(string message)
        {
            LastError = message;
            IsTracking = false;
            OnError?.Invoke(message);
        }

        public async ValueTask DisposeAsync()
        {
            await StopTrackingAsync();
            _dotNetRef?.Dispose();
        }
    }

    // Response DTOs
    public class GpsUpdateResponse
    {
        public bool Recorded { get; set; }
        public List<NearbyPoiDto> EnteredPois { get; set; } = new();
        public int NearbyCount { get; set; }
    }

    public class NearbyPoiDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Distance { get; set; }
        public double Radius { get; set; }
        public bool IsInGeofence { get; set; }
        public string? ImageUrl { get; set; }
    }
}
