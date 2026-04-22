using NarrationApp.Shared.DTOs.Owner;

namespace NarrationApp.Web.Services;

public sealed class OwnerProfileService(ApiClient apiClient) : IOwnerProfileService
{
    public Task<OwnerProfileDto> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<OwnerProfileDto>("api/owner/profile", cancellationToken);
    }

    public Task<OwnerProfileDto> UpdateProfileAsync(UpdateOwnerProfileRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PutAsync<UpdateOwnerProfileRequest, OwnerProfileDto>("api/owner/profile", request, cancellationToken);
    }
}
