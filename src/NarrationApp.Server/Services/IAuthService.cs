using NarrationApp.Shared.DTOs.Auth;

namespace NarrationApp.Server.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<OwnerRegistrationResponse> RegisterOwnerAsync(RegisterOwnerRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginTouristAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
}
