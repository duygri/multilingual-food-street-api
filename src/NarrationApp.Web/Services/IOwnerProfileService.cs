using NarrationApp.Shared.DTOs.Auth;
using NarrationApp.Shared.DTOs.Owner;

namespace NarrationApp.Web.Services;

public interface IOwnerProfileService
{
    Task<OwnerProfileDto> GetProfileAsync(CancellationToken cancellationToken = default);

    Task<OwnerProfileDto> UpdateProfileAsync(UpdateOwnerProfileRequest request, CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
