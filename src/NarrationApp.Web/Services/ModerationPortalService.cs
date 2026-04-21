using NarrationApp.Shared.DTOs.Moderation;

namespace NarrationApp.Web.Services;

public sealed class ModerationPortalService(ApiClient apiClient) : IModerationPortalService
{
    public Task<IReadOnlyList<ModerationRequestDto>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<IReadOnlyList<ModerationRequestDto>>("api/moderation-requests/my", cancellationToken);
    }

    public Task<ModerationRequestDto> CreateAsync(CreateModerationRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PostAsync<CreateModerationRequest, ModerationRequestDto>("api/moderation-requests", request, cancellationToken);
    }
}
