using NarrationApp.Shared.Enums;

namespace NarrationApp.Shared.DTOs.Auth;

public sealed class LoginRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}

public sealed class RegisterRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string PreferredLanguage { get; init; } = string.Empty;
}

public sealed class RegisterOwnerRequest
{
    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}

public sealed class OwnerRegistrationResponse
{
    public Guid UserId { get; init; }

    public string Email { get; init; } = string.Empty;

    public DateTime SubmittedAtUtc { get; init; }
}

public sealed class AuthResponse
{
    public Guid UserId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string PreferredLanguage { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    public string Token { get; init; } = string.Empty;

    public DateTime ExpiresAtUtc { get; init; }
}

public sealed class UpdateProfileRequest
{
    public string PreferredLanguage { get; init; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}
