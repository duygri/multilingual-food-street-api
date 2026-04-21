using NarrationApp.Shared.Enums;

namespace NarrationApp.Mobile.Features.Home;

public sealed record TouristAuthSession(
    Guid UserId,
    string Email,
    string PreferredLanguage,
    UserRole Role,
    string Token,
    DateTime ExpiresAtUtc)
{
    public bool IsExpired(DateTime? utcNow = null)
    {
        return ExpiresAtUtc <= (utcNow ?? DateTime.UtcNow).AddMinutes(1);
    }
}

public interface ITouristAuthSessionStore
{
    ValueTask<TouristAuthSession?> GetAsync(CancellationToken cancellationToken = default);

    ValueTask SetAsync(TouristAuthSession session, CancellationToken cancellationToken = default);

    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
