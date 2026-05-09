using NarrationApp.Shared.DTOs.Admin;

namespace NarrationApp.Web.Services;

public interface IAdminPoiOperationsService
{
    Task<AdminPoiDto> UpdatePriorityAsync(int poiId, int priority, CancellationToken cancellationToken = default);

    Task DeleteAsync(int poiId, CancellationToken cancellationToken = default);
}
