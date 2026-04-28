using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Web.Services;
using NarrationApp.Web.Support;

namespace NarrationApp.Web.Layout;

public partial class MainLayout
{
    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _ = InvokeAsync(async () =>
        {
            if (_isOwner)
            {
                await RefreshLayoutStateAsync();
            }
            else
            {
                UpdateRouteCopy();
            }

            StateHasChanged();
        });
    }

    private void HandleOwnerSummaryRefreshRequested()
    {
        _ = InvokeAsync(async () =>
        {
            await RefreshLayoutStateAsync();
            StateHasChanged();
        });
    }

    private void HandleAuthenticationStateChanged(Task<AuthenticationState> authenticationStateTask)
    {
        _ = InvokeAsync(async () =>
        {
            await RefreshLayoutStateAsync(authenticationStateTask);
            StateHasChanged();
        });
    }

    private async Task RefreshLayoutStateAsync(Task<AuthenticationState>? authenticationStateTask = null)
    {
        var state = authenticationStateTask is null
            ? await AuthenticationStateProvider.GetAuthenticationStateAsync()
            : await authenticationStateTask;

        var user = state.User;
        _isAuthenticated = user.Identity?.IsAuthenticated == true;
        _email = user.Identity?.Name ?? "guest@narration.app";
        _displayName = RouteHelper.GetDisplayName(user);
        _isOwner = RouteHelper.IsOwner(user);
        _roleLabel = RouteHelper.GetRoleLabel(user);
        _ownerSummary = _isOwner ? await LoadOwnerSummaryAsync() : null;
        _navigationItems = BuildNavigation(user, _ownerSummary);
        UpdatePortalBrand(user);
        UpdateRouteCopy();
    }

    private async Task<OwnerShellSummaryDto?> LoadOwnerSummaryAsync()
    {
        var ownerPortalService = ServiceProvider.GetService<IOwnerPortalService>();
        if (ownerPortalService is null)
        {
            return null;
        }

        try
        {
            return await ownerPortalService.GetShellSummaryAsync();
        }
        catch
        {
            return null;
        }
    }

    private void UpdatePortalBrand(ClaimsPrincipal user)
    {
        var role = user.FindFirst(ClaimTypes.Role)?.Value;

        switch (role)
        {
            case "admin":
                _portalBrand = "Vĩnh Khánh Admin";
                _portalEyebrow = "v2.1 — Quản trị hệ thống";
                _portalTagline = string.Empty;
                break;
            case "poi_owner":
                _portalBrand = "Vĩnh Khánh Owner";
                _portalEyebrow = "v2.1 — Quản lý nội dung";
                _portalTagline = string.Empty;
                break;
            default:
                _portalBrand = "Vĩnh Khánh Portal";
                _portalEyebrow = "v2.1 — Truy cập hệ thống";
                _portalTagline = string.Empty;
                break;
        }
    }
}
