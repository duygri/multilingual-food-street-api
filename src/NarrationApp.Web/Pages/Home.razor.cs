using NarrationApp.Web.Support;

namespace NarrationApp.Web.Pages;

public partial class Home
{
    protected override async Task OnInitializedAsync()
    {
        var state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated == true)
        {
            NavigationManager.NavigateTo(RouteHelper.GetDefaultRoute(state.User), replace: true);
            return;
        }

        NavigationManager.NavigateTo("/auth/login", replace: true);
    }
}
