namespace NarrationApp.SharedUI.Auth;

public interface IAuthSessionStore
{
    ValueTask<AuthSession?> GetAsync(CancellationToken cancellationToken = default);

    ValueTask SetAsync(AuthSession session, CancellationToken cancellationToken = default);

    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
