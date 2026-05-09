using NarrationApp.Shared.DTOs.Admin;

namespace NarrationApp.Web.Services;

public sealed class AdminPoiOperationsService(ApiClient apiClient) : IAdminPoiOperationsService
{
    public Task<AdminPoiDto> UpdatePriorityAsync(int poiId, int priority, CancellationToken cancellationToken = default)
    {
        return apiClient.PutAsync<UpdatePoiPriorityRequest, AdminPoiDto>(
            $"api/admin/pois/{poiId}/priority",
            new UpdatePoiPriorityRequest { Priority = priority },
            cancellationToken);
    }

    public Task DeleteAsync(int poiId, CancellationToken cancellationToken = default)
    {
        return apiClient.DeleteAsync($"api/pois/{poiId}", cancellationToken);
    }
}
