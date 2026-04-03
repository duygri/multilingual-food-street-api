using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FoodStreet.Client.Services
{
    public interface INarrationEngine : IDisposable
    {
        void Start();
        void Stop();
    }

    public class NarrationEngine : INarrationEngine
    {
        private readonly IGpsTrackingService _gpsTrackingService;
        private readonly ITtsService _ttsService;
        private readonly TourPlayerService _tourPlayerService;
        private readonly HttpClient _httpClient;

        private Timer? _heartbeatTimer;
        private readonly ConcurrentDictionary<int, PoiEntryState> _pendingPois = new();
        
        // Single-slot queue state
        private bool _isCurrentlyPlayingOrLoading;

        public NarrationEngine(
            IGpsTrackingService gpsTrackingService, 
            ITtsService ttsService,
            TourPlayerService tourPlayerService,
            HttpClient httpClient)
        {
            _gpsTrackingService = gpsTrackingService;
            _ttsService = ttsService;
            _tourPlayerService = tourPlayerService;
            _httpClient = httpClient;
            
            // Lắng nghe sự kiện để biết bao giờ audio kết thúc
            _tourPlayerService.OnChange += OnPlayerStateChanged;
        }

        private void OnPlayerStateChanged()
        {
            // Nếu UI TourPlayer ẩn hoặc đã dừng chơi, giải phóng slot
            if (!_tourPlayerService.IsPlaying && !_tourPlayerService.IsVisible)
            {
                _isCurrentlyPlayingOrLoading = false;
            }
        }

        public void Start()
        {
            Stop(); // Avoid double subscription
            _gpsTrackingService.OnGeofenceEntered += OnGeofenceEntered;
            _gpsTrackingService.OnGeofenceExited += OnGeofenceExited;
            // Giai đoạn 3: Reconcile loop mỗi 1s
            _heartbeatTimer = new Timer(ReconcileLoop, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        public void Stop()
        {
            _gpsTrackingService.OnGeofenceEntered -= OnGeofenceEntered;
            _gpsTrackingService.OnGeofenceExited -= OnGeofenceExited;
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;
        }

        private void OnGeofenceEntered(List<NearbyPoiDto> enteredPois)
        {
            var now = DateTime.UtcNow;
            foreach (var poi in enteredPois)
            {
                if (!_pendingPois.ContainsKey(poi.Id))
                {
                    _pendingPois[poi.Id] = new PoiEntryState 
                    { 
                        Poi = poi, 
                        FirstDetectedAt = now 
                    };
                }
            }
        }

        /// <summary>
        /// When user exits a geofence zone, remove from pending to avoid
        /// playing audio for a POI the user already walked away from.
        /// </summary>
        private void OnGeofenceExited(List<int> exitedPoiIds)
        {
            foreach (var id in exitedPoiIds)
            {
                _pendingPois.TryRemove(id, out _);
            }
        }

        private async void ReconcileLoop(object? state)
        {
            if (_isCurrentlyPlayingOrLoading) return; // AudioQueue Single-slot: Đợi phát xong

            var now = DateTime.UtcNow;
            NearbyPoiDto? selectedPoi = null;

            // Lọc ra các POI đã ở trong vùng liên tục 3 giây (Debounce 3s)
            var confirmedPois = _pendingPois.Values
                .Where(p => (now - p.FirstDetectedAt).TotalSeconds >= 3)
                .Select(p => p.Poi)
                .ToList();

            if (confirmedPois.Any())
            {
                // Chọn POI ưu tiên cao nhất, sau đó là khoảng cách gần nhất
                selectedPoi = confirmedPois
                    .OrderByDescending(p => p.Priority)
                    .ThenBy(p => p.Distance)
                    .FirstOrDefault();
            }

            if (selectedPoi != null)
            {
                _isCurrentlyPlayingOrLoading = true;
                _pendingPois.TryRemove(selectedPoi.Id, out _);
                
                try 
                {
                    await PlayWithFallbackAsync(selectedPoi);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NarrationEngine] Lỗi phát âm thanh: {ex.Message}");
                    _isCurrentlyPlayingOrLoading = false;
                }
            }
            
            // Xóa các POI bị treo quá 30 giây (có thể tín hiệu yếu không confirm được)
            var expiredKeys = _pendingPois.Where(kv => (now - kv.Value.FirstDetectedAt).TotalSeconds > 30).Select(k => k.Key).ToList();
            foreach (var key in expiredKeys)
            {
                _pendingPois.TryRemove(key, out _);
            }
        }

        // Giai đoạn 4 - 4-Tier Hybrid Playback
        private async Task PlayWithFallbackAsync(NearbyPoiDto poi)
        {
            var title = poi.Name;
            var textToRead = string.IsNullOrWhiteSpace(poi.Description) ? $"Bạn vừa đến {poi.Name}" : $"Bạn vừa đến {poi.Name}. {poi.Description}";

            // Tier 1: Pre-generated Audio (0ms)
            // Phát từ cache/URL gốc nếu có sẵn và không phải hàng Fallback
            if (!string.IsNullOrWhiteSpace(poi.AudioUrl) && !poi.IsFallback)
            {
                await _tourPlayerService.Play(new PlayableItem(poi.Id, title, "Audio gốc chất lượng cao", poi.ImageUrl, poi.AudioUrl, null));
                return;
            }

            // Tier 1.5 & Tier 2: Cloud TTS Stream / On-demand
            // Hiển thị UI đang chuẩn bị tải âm thanh AI
            await _tourPlayerService.Play(new PlayableItem(poi.Id, title, "Đang tải âm thanh AI Cloud...", poi.ImageUrl, null, null));

            try 
            {
                // Gọi API để synthesize âm thanh (yêu cầu backend có endpoint /api/audio/tts)
                var response = await _httpClient.PostAsJsonAsync("api/audio/tts", new
                {
                    Text = textToRead,
                    PoiId = poi.Id,
                    Language = poi.LanguageCode
                });
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<TtsResponseDto>();
                    if (result != null && !string.IsNullOrWhiteSpace(result.Url))
                    {
                        // Tier 2 thành công
                        await _tourPlayerService.Play(new PlayableItem(poi.Id, title, "Phát AI Audio Cloud", poi.ImageUrl, result.Url, null));
                        return;
                    }
                }
            }
            catch (Exception)
            {
                // Lỗi mạng hoặc server không phản hồi -> Xử lý tiếp ở Tier 3
                Console.WriteLine("[NarrationEngine] Không lấy được Audio Cloud, chuyển sang Local TTS");
            }

            // Tier 3: Local Speech Synthesis (0ms, Offline)
            // Dự phòng cuối cùng bằng Text-to-speech nội bộ của thiết bị hoặc trình duyệt
            await _tourPlayerService.Play(new PlayableItem(poi.Id, title, "Phát bằng hệ thống thiết bị", poi.ImageUrl, null, textToRead));
        }

        public void Dispose()
        {
            Stop();
            _tourPlayerService.OnChange -= OnPlayerStateChanged;
        }

        private class PoiEntryState
        {
            public NearbyPoiDto Poi { get; set; } = null!;
            public DateTime FirstDetectedAt { get; set; }
        }

        private class TtsResponseDto
        {
            public string? Url { get; set; }
        }
    }
}
