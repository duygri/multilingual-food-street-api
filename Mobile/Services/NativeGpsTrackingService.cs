using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FoodStreet.Client.Services;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;

namespace FoodStreet.Mobile.Services
{
    public class NativeGpsTrackingService : IGpsTrackingService
    {
        private readonly HttpClient _httpClient;
        private readonly ITtsService _ttsService;
        private CancellationTokenSource? _cts;
        
        // State
        public bool IsTracking { get; private set; }
        public double? CurrentLatitude { get; private set; }
        public double? CurrentLongitude { get; private set; }
        public double? Accuracy { get; private set; }
        public string? LastError { get; private set; }
        public string SessionId { get; private set; }

        // Anti-spam
        private readonly Dictionary<int, DateTime> _triggeredPois = new();
        private readonly TimeSpan _cooldownDuration = TimeSpan.FromMinutes(5);
        private readonly HashSet<int> _currentGeofencePois = new();

        public event Action<double, double>? OnPositionUpdated;
        public event Action<List<NearbyPoiDto>>? OnGeofenceEntered;
        public event Action<string>? OnError;

        public NativeGpsTrackingService(HttpClient httpClient, ITtsService ttsService)
        {
            _httpClient = httpClient;
            _ttsService = ttsService;
            SessionId = Guid.NewGuid().ToString();
        }

        public async Task StartTrackingAsync()
        {
            if (IsTracking) return;

            var permission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (permission != PermissionStatus.Granted)
            {
                permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (permission != PermissionStatus.Granted)
                {
                    LastError = "Location permission denied";
                    OnError?.Invoke(LastError);
                    return;
                }
            }
            
            var alwaysPermission = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
            if (alwaysPermission != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.LocationAlways>();
            }

            IsTracking = true;
            LastError = null;
            _cts = new CancellationTokenSource();

#if ANDROID
            var context = Platform.AppContext;
            var intent = new global::Android.Content.Intent(context, typeof(FoodStreet.Mobile.Platforms.Android.GpsForegroundService));
            context.StartForegroundService(intent);
#else
            _ = TrackingLoopAsync(_cts.Token);
#endif
        }

        public Task StopTrackingAsync()
        {
            if (!IsTracking) return Task.CompletedTask;
            IsTracking = false;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

#if ANDROID
            var context = Platform.AppContext;
            var intent = new global::Android.Content.Intent(context, typeof(FoodStreet.Mobile.Platforms.Android.GpsForegroundService));
            context.StopService(intent);
#endif

            return Task.CompletedTask;
        }

        public async Task GetCurrentPositionAsync()
        {
            await GetLocationAndUpdateAsync(CancellationToken.None);
        }

        private async Task TrackingLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await GetLocationAndUpdateAsync(token);
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                LastError = ex.Message;
                IsTracking = false;
                OnError?.Invoke(LastError);
            }
        }

        private async Task GetLocationAndUpdateAsync(CancellationToken token)
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(5));
                var location = await Geolocation.Default.GetLocationAsync(request, token);

                if (location != null)
                {
                    CurrentLatitude = location.Latitude;
                    CurrentLongitude = location.Longitude;
                    Accuracy = location.Accuracy;

                    // Execute on Main Thread for UI
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OnPositionUpdated?.Invoke(location.Latitude, location.Longitude);
                    });

                    await SendLocationToServerAsync(location.Latitude, location.Longitude, location.Accuracy ?? 0, location.Speed ?? 0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NativeGPS] Error: {ex.Message}");
            }
        }

        private async Task SendLocationToServerAsync(double latitude, double longitude, double accuracy, double speed)
        {
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
                        var newlyEntered = new List<NearbyPoiDto>();
                        var now = DateTime.UtcNow;

                        foreach (var poi in result.EnteredPois)
                        {
                            if (_triggeredPois.TryGetValue(poi.Id, out var lastTriggered))
                            {
                                if (now - lastTriggered < _cooldownDuration)
                                    continue;
                            }

                            newlyEntered.Add(poi);
                            _triggeredPois[poi.Id] = now;
                        }

                        if (newlyEntered.Count > 0)
                        {
                            // Trigger TTS narration natively
                            foreach (var poi in newlyEntered)
                            {
                                if (!string.IsNullOrWhiteSpace(poi.Description))
                                {
                                    _ = _ttsService.PlayTextAsync($"Bạn vừa đến {poi.Name}. {poi.Description}");
                                }
                                else
                                {
                                    _ = _ttsService.PlayTextAsync($"Bạn vừa đến {poi.Name}");
                                }
                            }

                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                OnGeofenceEntered?.Invoke(newlyEntered);
                            });
                        }

                        _currentGeofencePois.Clear();
                        foreach (var poi in result.EnteredPois)
                        {
                            _currentGeofencePois.Add(poi.Id);
                        }

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
                Console.WriteLine($"[NativeGPS] API error: {ex.Message}");
            }
        }

        public ValueTask DisposeAsync()
        {
            StopTrackingAsync();
            return ValueTask.CompletedTask;
        }
    }
}
