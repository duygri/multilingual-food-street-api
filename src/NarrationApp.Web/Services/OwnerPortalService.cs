using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.DTOs.Poi;

namespace NarrationApp.Web.Services;

public sealed class OwnerPortalService(ApiClient apiClient) : IOwnerPortalService
{
    public Task<OwnerShellSummaryDto> GetShellSummaryAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<OwnerShellSummaryDto>("api/owner/shell-summary", cancellationToken);
    }

    public Task<OwnerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<OwnerDashboardDto>("api/owner/dashboard", cancellationToken);
    }

    public Task<OwnerDashboardWorkspaceDto> GetDashboardWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<OwnerDashboardWorkspaceDto>("api/owner/dashboard/workspace", cancellationToken);
    }

    public Task<IReadOnlyList<PoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<IReadOnlyList<PoiDto>>("api/owner/pois", cancellationToken);
    }

    public Task<OwnerPoisWorkspaceDto> GetPoisWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<OwnerPoisWorkspaceDto>("api/owner/pois/workspace", cancellationToken);
    }

    public Task<PoiDto> GetPoiAsync(int poiId, CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<PoiDto>($"api/owner/pois/{poiId}", cancellationToken);
    }

    public Task<OwnerPoiDetailWorkspaceDto> GetPoiWorkspaceAsync(int poiId, CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<OwnerPoiDetailWorkspaceDto>($"api/owner/pois/{poiId}/workspace", cancellationToken);
    }

    public Task<OwnerPoiStatsDto> GetPoiStatsAsync(int poiId, CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<OwnerPoiStatsDto>($"api/owner/pois/{poiId}/stats", cancellationToken);
    }

    public Task<OwnerModerationWorkspaceDto> GetModerationWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<OwnerModerationWorkspaceDto>("api/owner/moderation/workspace", cancellationToken);
    }

    public Task<PoiDto> CreatePoiAsync(CreatePoiRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PostAsync<CreatePoiRequest, PoiDto>("api/pois", request, cancellationToken);
    }

    public Task<PoiDto> UpdatePoiAsync(int poiId, UpdatePoiRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PutAsync<UpdatePoiRequest, PoiDto>($"api/pois/{poiId}", request, cancellationToken);
    }

    public Task<PoiDto> UploadPoiImageAsync(
        int poiId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = new(contentType);
        form.Add(fileContent, "file", fileName);

        return apiClient.PostMultipartAsync<PoiDto>($"api/pois/{poiId}/image", form, cancellationToken);
    }

    public Task<PoiDto> DeletePoiImageAsync(int poiId, CancellationToken cancellationToken = default)
    {
        return apiClient.DeleteAsync<PoiDto>($"api/pois/{poiId}/image", cancellationToken);
    }

    public Task DeletePoiAsync(int poiId, CancellationToken cancellationToken = default)
    {
        return apiClient.DeleteAsync($"api/pois/{poiId}", cancellationToken);
    }
}
