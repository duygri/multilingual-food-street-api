using NarrationApp.Shared.Enums;

namespace NarrationApp.SharedUI.Auth;

public sealed class AuthSession
{
    public Guid UserId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    public string PreferredLanguage { get; init; } = "vi";

    public string Token { get; init; } = string.Empty;
}
