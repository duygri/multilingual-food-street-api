using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoodStreet.Client.Services
{
    public interface IGpsTrackingService : IAsyncDisposable
    {
        bool IsTracking { get; }
        double? CurrentLatitude { get; }
        double? CurrentLongitude { get; }
        double? Accuracy { get; }
        string? LastError { get; }
        string SessionId { get; }

        event Action<double, double>? OnPositionUpdated;
        event Action<List<NearbyPoiDto>>? OnGeofenceEntered;
        event Action<string>? OnError;

        Task StartTrackingAsync();
        Task StopTrackingAsync();
        Task GetCurrentPositionAsync();
    }
}
