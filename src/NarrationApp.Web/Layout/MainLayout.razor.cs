using Microsoft.AspNetCore.Components.Authorization;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.SharedUI.Models;
using NarrationApp.SharedUI.Services;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Layout;

public partial class MainLayout : IDisposable
{
    private bool _isAuthenticated;
    private bool _isOwner;
    private string _email = string.Empty;
    private string _displayName = string.Empty;
    private string _roleLabel = "Khách";
    private string _heading = "Portal vận hành";
    private string _summary = "Admin / Tổng quan";
    private string _eyebrow = "Cổng truy cập";
    private string _portalBrand = "Vĩnh Khánh Portal";
    private string _portalEyebrow = "v2.1 — Truy cập hệ thống";
    private string _portalTagline = string.Empty;
    private OwnerShellSummaryDto? _ownerSummary;
    private IReadOnlyList<ShellNavItem> _navigationItems = Array.Empty<ShellNavItem>();
    private INotificationCenterService? _notificationCenterService;
    private OwnerPortalRefreshService? _ownerPortalRefreshService;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationStateProvider.AuthenticationStateChanged += HandleAuthenticationStateChanged;
        NavigationManager.LocationChanged += HandleLocationChanged;
        _notificationCenterService = ServiceProvider.GetService<INotificationCenterService>();
        _ownerPortalRefreshService = ServiceProvider.GetService<OwnerPortalRefreshService>();
        if (_notificationCenterService is not null)
        {
            _notificationCenterService.Changed += HandleOwnerSummaryRefreshRequested;
        }

        if (_ownerPortalRefreshService is not null)
        {
            _ownerPortalRefreshService.Changed += HandleOwnerSummaryRefreshRequested;
        }

        await RefreshLayoutStateAsync();
    }

    public void Dispose()
    {
        AuthenticationStateProvider.AuthenticationStateChanged -= HandleAuthenticationStateChanged;
        NavigationManager.LocationChanged -= HandleLocationChanged;
        if (_notificationCenterService is not null)
        {
            _notificationCenterService.Changed -= HandleOwnerSummaryRefreshRequested;
        }

        if (_ownerPortalRefreshService is not null)
        {
            _ownerPortalRefreshService.Changed -= HandleOwnerSummaryRefreshRequested;
        }
    }

    private async Task LogoutAsync()
    {
        await AuthClientService.LogoutAsync();
        NavigationManager.NavigateTo("/auth/login");
    }
}
