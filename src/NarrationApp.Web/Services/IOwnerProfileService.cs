using NarrationApp.Shared.DTOs.Owner;

namespace NarrationApp.Web.Services;

public interface IOwnerProfileService
{
    Task<OwnerProfileDto> GetProfileAsync(CancellationToken cancellationToken = default);

    Task<OwnerProfileDto> UpdateProfileAsync(UpdateOwnerProfileRequest request, CancellationToken cancellationToken = default);
}
