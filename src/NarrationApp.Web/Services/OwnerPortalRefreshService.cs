namespace NarrationApp.Web.Services;

public sealed class OwnerPortalRefreshService
{
    public event Action? Changed;

    public void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
