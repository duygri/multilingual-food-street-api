namespace NarrationApp.Web.Pages.Owner;

public partial class PoiManagement
{
    protected override void OnInitialized()
    {
        NavigationManager.NavigateTo("/owner/pois", replace: true);
    }
}
