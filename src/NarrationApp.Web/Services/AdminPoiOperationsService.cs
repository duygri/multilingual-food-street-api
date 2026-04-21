namespace NarrationApp.Web.Services;

public sealed class AdminPoiOperationsService(ApiClient apiClient) : IAdminPoiOperationsService
{
    public Task DeleteAsync(int poiId, CancellationToken cancellationToken = default)
    {
        return apiClient.DeleteAsync($"api/pois/{poiId}", cancellationToken);
    }
}
