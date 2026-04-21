using NarrationApp.Shared.DTOs.Moderation;

namespace NarrationApp.Web.Services;

public interface IModerationPortalService
{
    Task<IReadOnlyList<ModerationRequestDto>> GetMineAsync(CancellationToken cancellationToken = default);

    Task<ModerationRequestDto> CreateAsync(CreateModerationRequest request, CancellationToken cancellationToken = default);
}
