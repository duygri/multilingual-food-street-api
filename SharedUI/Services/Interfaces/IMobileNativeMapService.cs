using System.Threading;
using System.Threading.Tasks;
using FoodStreet.Client.DTOs;

namespace FoodStreet.Client.Services
{
    public interface IMobileNativeMapService
    {
        Task OpenBrowseMapAsync(MobileNativeMapRequest request, CancellationToken cancellationToken = default);
        Task<MobileNativeMapResult?> OpenPickerAsync(MobileNativeMapRequest request, CancellationToken cancellationToken = default);
    }
}
