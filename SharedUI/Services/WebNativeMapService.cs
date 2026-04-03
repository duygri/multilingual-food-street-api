using System.Threading;
using System.Threading.Tasks;
using FoodStreet.Client.DTOs;

namespace FoodStreet.Client.Services
{
    /// <summary>
    /// No-op stub for web platform. On web, map picker uses JS Interop (Google Maps),
    /// so native map calls are intentionally unsupported.
    /// </summary>
    public sealed class WebNativeMapService : IMobileNativeMapService
    {
        public Task OpenBrowseMapAsync(MobileNativeMapRequest request, CancellationToken cancellationToken = default)
        {
            // Web platform does not support native Android map browsing.
            return Task.CompletedTask;
        }

        public Task<MobileNativeMapResult?> OpenPickerAsync(MobileNativeMapRequest request, CancellationToken cancellationToken = default)
        {
            // Web platform uses JS Interop for map picking, not native Android.
            return Task.FromResult<MobileNativeMapResult?>(null);
        }
    }
}
