namespace NarrationApp.Web.Components;

public partial class RedirectToLogin
{
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            NavigationManager.NavigateTo("/auth/login", replace: true);
        }
    }
}
