using System.Net;
using System.Net.Http;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Notification;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;
using NarrationApp.SharedUI.Auth;
using NarrationApp.SharedUI.Services;
using NarrationApp.Web.Layout;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Layout;

public sealed class MainLayoutTests : TestContext
{
    [Fact]
    public void Main_layout_behavior_is_split_into_focused_partials()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var layoutRoot = Path.Combine(projectRoot, "src", "NarrationApp.Web", "Layout");
        var markupPath = Path.Combine(layoutRoot, "MainLayout.razor");
        var expectedPartials = new[]
        {
            ("MainLayout.razor.cs", "OnInitializedAsync"),
            ("MainLayout.State.razor.cs", "RefreshLayoutStateAsync"),
            ("MainLayout.RouteCopy.razor.cs", "UpdateRouteCopy"),
            ("MainLayout.Navigation.razor.cs", "BuildNavigation")
        };

        var markup = File.ReadAllText(markupPath);
        Assert.DoesNotContain("@code", markup, StringComparison.Ordinal);

        foreach (var (fileName, marker) in expectedPartials)
        {
            var path = Path.Combine(layoutRoot, fileName);
            Assert.True(File.Exists(path), $"{fileName} should exist.");
            var source = File.ReadAllText(path);
            Assert.Contains("partial class MainLayout", source, StringComparison.Ordinal);
            Assert.Contains(marker, source, StringComparison.Ordinal);
        }

        var coreLines = File.ReadAllLines(Path.Combine(layoutRoot, "MainLayout.razor.cs")).Length;
        Assert.True(coreLines <= 90, $"MainLayout.razor.cs should stay focused on lifecycle and shell state, but has {coreLines} lines.");
        var stateLines = File.ReadAllLines(Path.Combine(layoutRoot, "MainLayout.State.razor.cs")).Length;
        Assert.True(stateLines <= 110, $"MainLayout.State.razor.cs should stay focused on state refresh, but has {stateLines} lines.");
        var routeLines = File.ReadAllLines(Path.Combine(layoutRoot, "MainLayout.RouteCopy.razor.cs")).Length;
        Assert.True(routeLines <= 90, $"MainLayout.RouteCopy.razor.cs should stay focused on route copy, but has {routeLines} lines.");
        var navigationLines = File.ReadAllLines(Path.Combine(layoutRoot, "MainLayout.Navigation.razor.cs")).Length;
        Assert.True(navigationLines <= 150, $"MainLayout.Navigation.razor.cs should stay focused on nav assembly, but has {navigationLines} lines.");
    }

    [Fact]
    public void Admin_navigation_restores_standalone_qr_menu_item()
    {
        ConfigureAuthenticatedAdmin();
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/admin/dashboard");

        var cut = RenderComponent<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Tour", cut.Markup);
            Assert.Contains("QR Codes", cut.Markup);
            Assert.Contains("Ngôn ngữ", cut.Markup);
        });
    }

    [Fact]
    public void Language_management_route_copy_mentions_system_language_governance()
    {
        ConfigureAuthenticatedAdmin();
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/admin/language-management");

        var cut = RenderComponent<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Quản lý ngôn ngữ hệ thống", cut.Markup);
            Assert.Contains("bật hoặc mở rộng", cut.Markup);
        });
    }

    [Fact]
    public void Translation_review_route_copy_references_google_cloud_translation()
    {
        ConfigureAuthenticatedAdmin();
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/admin/translation-review");

        var cut = RenderComponent<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() => Assert.Contains("Google Cloud Translation", cut.Markup));
        Assert.DoesNotContain("Deep Translator", cut.Markup);
    }

    [Fact]
    public void Analytics_route_copy_uses_vietnamese_summary_terms()
    {
        ConfigureAuthenticatedAdmin();
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/admin/analytics");

        var cut = RenderComponent<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("điểm POI nổi bật", cut.Markup);
            Assert.Contains("mức nghe audio", cut.Markup);
        });

        Assert.DoesNotContain("top POIs", cut.Markup);
        Assert.DoesNotContain("audio consumption", cut.Markup);
    }

    [Fact]
    public void Visitor_devices_route_copy_uses_visitor_device_wording()
    {
        ConfigureAuthenticatedAdmin();
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/admin/visitor-devices");

        var cut = RenderComponent<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Thiết bị visitor", cut.Markup);
            Assert.Contains("visitor đang online", cut.Markup);
        });

        Assert.DoesNotContain("User Manager", cut.Markup);
        Assert.DoesNotContain("Users", cut.Markup);
    }

    [Fact]
    public void Owner_poi_management_route_copy_avoids_english_workflow_wording()
    {
        ConfigureAuthenticatedOwner();
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/owner/poi-management");

        var cut = RenderComponent<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("luồng quản lý chủ POI", cut.Markup);
            Assert.Contains("cập nhật vùng kích hoạt", cut.Markup);
        });

        Assert.DoesNotContain("workflow owner", cut.Markup);
    }

    [Theory]
    [InlineData("http://localhost/owner/moderation", "Moderation", "Theo dõi các POI đang chờ duyệt")]
    [InlineData("http://localhost/owner/notifications", "Notifications", "lịch sử thông báo")]
    [InlineData("http://localhost/owner/profile", "Profile", "hồ sơ và bảo mật tài khoản")]
    public void Owner_workspace_routes_render_dedicated_copy(string uri, string expectedHeading, string expectedSummary)
    {
        ConfigureAuthenticatedOwner();
        Services.GetRequiredService<NavigationManager>().NavigateTo(uri);

        var cut = RenderComponent<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(expectedHeading, cut.Markup);
            Assert.Contains(expectedSummary, cut.Markup);
        });
    }

    [Fact]
    public void Owner_layout_renders_grouped_routes_and_owner_badges_without_sidebar_profile_card()
    {
        ConfigureAuthenticatedOwner(
            fullName: "Bà Tám Bún Bò",
            shellSummary: new OwnerShellSummaryDto
            {
                TotalPois = 5,
                PublishedPois = 3,
                PendingModerationRequests = 2,
                UnreadNotifications = 7
            });
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/owner/pois");

        var cut = RenderComponent<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".portal-shell__profile"));
            Assert.Empty(cut.FindAll(".layout-owner-card"));
            Assert.Contains("Tổng quan", cut.Markup);
            Assert.Contains("Nội dung", cut.Markup);
            Assert.Contains("Vận hành", cut.Markup);
            Assert.Contains("Tài khoản", cut.Markup);
            Assert.Contains("Tạo POI mới", cut.Markup);
            Assert.Contains("Moderation", cut.Markup);
            Assert.Contains("Profile", cut.Markup);
            Assert.DoesNotContain("Notifications", cut.Markup);

            var badges = GetNavBadgeTexts(cut);
            Assert.Contains("2", badges);
            Assert.DoesNotContain("7", badges);
            Assert.Contains("7", GetNotificationBadgeTexts(cut));
        });

        cut.Find(".notification-center__toggle").Click();

        cut.WaitForAssertion(() =>
        {
            var shortcut = cut.Find("a[data-action='open-notifications-page']");
            Assert.Equal("/owner/notifications", shortcut.GetAttribute("href"));
        });
    }

    [Fact]
    public void Owner_layout_does_not_render_sidebar_profile_card_or_inline_owner_stats()
    {
        ConfigureAuthenticatedOwner(
            fullName: "Nguyen Van A",
            shellSummary: new OwnerShellSummaryDto
            {
                TotalPois = 6,
                PublishedPois = 4,
                PendingModerationRequests = 2,
                UnreadNotifications = 7
            });
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/owner/pois");

        var cut = RenderComponent<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".portal-shell__profile"));
            Assert.Empty(cut.FindAll(".layout-owner-card"));
            Assert.Empty(cut.FindAll(".layout-owner-card__avatar"));
            Assert.Empty(cut.FindAll(".layout-owner-card__stat-value"));
        });
    }

    [Fact]
    public void Owner_layout_keeps_notifications_in_header_bell_only()
    {
        var (_, notificationService, _) = ConfigureAuthenticatedOwner(
            shellSummary: new OwnerShellSummaryDto
            {
                TotalPois = 5,
                PublishedPois = 3,
                PendingModerationRequests = 2,
                UnreadNotifications = 7
            });
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/owner/pois");

        var cut = RenderComponent<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Notifications", cut.Markup);
            Assert.DoesNotContain("7", GetNavBadgeTexts(cut));
            Assert.Contains("7", GetNotificationBadgeTexts(cut));
        });

        notificationService.UnreadCount = 0;
        notificationService.RaiseChanged();

        cut.WaitForAssertion(() => Assert.DoesNotContain("7", GetNotificationBadgeTexts(cut)));
    }

    [Fact]
    public void Owner_layout_refreshes_sidebar_summary_when_owner_portal_refresh_is_raised()
    {
        var (ownerService, _, refreshService) = ConfigureAuthenticatedOwner(
            shellSummary: new OwnerShellSummaryDto
            {
                TotalPois = 5,
                PublishedPois = 3,
                PendingModerationRequests = 2,
                UnreadNotifications = 7
            });
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/owner/moderation");

        var cut = RenderComponent<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() => Assert.Contains("2", GetNavBadgeTexts(cut)));

        ownerService.ShellSummary = new OwnerShellSummaryDto
        {
            TotalPois = 6,
            PublishedPois = 4,
            PendingModerationRequests = 0,
            UnreadNotifications = 7
        };
        refreshService.NotifyChanged();

        cut.WaitForAssertion(() =>
        {
            var badges = GetNavBadgeTexts(cut);
            Assert.DoesNotContain("2", badges);
            Assert.Contains("7", GetNotificationBadgeTexts(cut));
        });
    }

    [Fact]
    public void Admin_layout_does_not_render_sidebar_profile_slot()
    {
        ConfigureAuthenticatedAdmin();
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/admin/dashboard");

        var cut = RenderComponent<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".portal-shell__profile"));
            Assert.Empty(cut.FindAll(".layout-owner-card"));
            Assert.DoesNotContain("POI Owner", cut.Markup);
        });
    }

    [Fact]
    public void Anonymous_layout_does_not_render_sidebar_profile_slot()
    {
        ConfigureAnonymousUser();
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/auth/login");

        var cut = RenderComponent<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".portal-shell__profile"));
            Assert.Empty(cut.FindAll(".layout-owner-card"));
            Assert.DoesNotContain("POI Owner", cut.Markup);
        });
    }

    private void ConfigureAuthenticatedAdmin()
    {
        var sessionStore = new TestAuthSessionStore
        {
            Session = new AuthSession
            {
                UserId = Guid.NewGuid(),
                Email = "admin@narration.app",
                Role = UserRole.Admin,
                PreferredLanguage = "vi",
                Token = "jwt-token"
            }
        };

        var authStateProvider = new CustomAuthStateProvider(sessionStore);
        var apiClient = new ApiClient(new HttpClient(new StubHttpMessageHandler())
        {
            BaseAddress = new Uri("http://localhost")
        }, sessionStore, authStateProvider);

        Services.AddSingleton<IAuthSessionStore>(sessionStore);
        Services.AddSingleton(authStateProvider);
        Services.AddSingleton<AuthenticationStateProvider>(authStateProvider);
        Services.AddSingleton(new AuthClientService(apiClient, authStateProvider));
        Services.AddSingleton<INotificationCenterService>(new TestNotificationCenterService());
        Services.AddSingleton<IOwnerPortalService>(new TestOwnerPortalService());
    }

    private (TestOwnerPortalService OwnerService, TestNotificationCenterService NotificationService, OwnerPortalRefreshService RefreshService) ConfigureAuthenticatedOwner(
        string fullName = "Demo Owner",
        OwnerShellSummaryDto? shellSummary = null)
    {
        var sessionStore = new TestAuthSessionStore
        {
            Session = new AuthSession
            {
                UserId = Guid.NewGuid(),
                FullName = fullName,
                Email = "owner@narration.app",
                Role = UserRole.PoiOwner,
                PreferredLanguage = "vi",
                Token = "jwt-token"
            }
        };

        var authStateProvider = new CustomAuthStateProvider(sessionStore);
        var apiClient = new ApiClient(new HttpClient(new StubHttpMessageHandler())
        {
            BaseAddress = new Uri("http://localhost")
        }, sessionStore, authStateProvider);

        var notificationService = new TestNotificationCenterService(shellSummary?.UnreadNotifications ?? 0);
        var ownerService = new TestOwnerPortalService(shellSummary);
        var refreshService = new OwnerPortalRefreshService();

        Services.AddSingleton<IAuthSessionStore>(sessionStore);
        Services.AddSingleton(authStateProvider);
        Services.AddSingleton<AuthenticationStateProvider>(authStateProvider);
        Services.AddSingleton(new AuthClientService(apiClient, authStateProvider));
        Services.AddSingleton(notificationService);
        Services.AddSingleton<INotificationCenterService>(notificationService);
        Services.AddSingleton(ownerService);
        Services.AddSingleton<IOwnerPortalService>(ownerService);
        Services.AddSingleton(refreshService);

        return (ownerService, notificationService, refreshService);
    }

    private void ConfigureAnonymousUser()
    {
        var sessionStore = new TestAuthSessionStore();
        var authStateProvider = new CustomAuthStateProvider(sessionStore);
        var apiClient = new ApiClient(new HttpClient(new StubHttpMessageHandler())
        {
            BaseAddress = new Uri("http://localhost")
        }, sessionStore, authStateProvider);

        Services.AddSingleton<IAuthSessionStore>(sessionStore);
        Services.AddSingleton(authStateProvider);
        Services.AddSingleton<AuthenticationStateProvider>(authStateProvider);
        Services.AddSingleton(new AuthClientService(apiClient, authStateProvider));
        Services.AddSingleton<INotificationCenterService>(new TestNotificationCenterService());
        Services.AddSingleton<IOwnerPortalService>(new TestOwnerPortalService());
    }

    private sealed class TestNotificationCenterService(int unreadCount = 0) : INotificationCenterService
    {
        public event Action? Changed;

        public int UnreadCount { get; set; } = unreadCount;

        public ValueTask<IReadOnlyList<NotificationDto>> GetAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyList<NotificationDto>>(Array.Empty<NotificationDto>());
        }

        public ValueTask<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(UnreadCount);
        }

        public ValueTask MarkAllReadAsync(CancellationToken cancellationToken = default)
        {
            Changed?.Invoke();
            return ValueTask.CompletedTask;
        }

        public ValueTask MarkReadAsync(int notificationId, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public void RaiseChanged()
        {
            Changed?.Invoke();
        }
    }

    private sealed class TestOwnerPortalService(OwnerShellSummaryDto? shellSummary = null) : IOwnerPortalService
    {
        public OwnerShellSummaryDto ShellSummary { get; set; } = shellSummary ?? new OwnerShellSummaryDto();
        public OwnerDashboardDto Dashboard { get; set; } = new();

        public Task<OwnerShellSummaryDto> GetShellSummaryAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ShellSummary);
        }

        public Task<OwnerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Dashboard);
        }

        public Task<IReadOnlyList<PoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PoiDto>>(Array.Empty<PoiDto>());
        }

        public Task<PoiDto> GetPoiAsync(int poiId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PoiDto { Id = poiId });
        }

        public Task<OwnerPoiStatsDto> GetPoiStatsAsync(int poiId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OwnerPoiStatsDto { PoiId = poiId });
        }

        public Task<PoiDto> CreatePoiAsync(CreatePoiRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PoiDto());
        }

        public Task<PoiDto> UpdatePoiAsync(int poiId, UpdatePoiRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PoiDto { Id = poiId });
        }

        public Task DeletePoiAsync(int poiId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestAuthSessionStore : IAuthSessionStore
    {
        public AuthSession? Session { get; set; }

        public ValueTask<AuthSession?> GetAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(Session);
        }

        public ValueTask SetAsync(AuthSession session, CancellationToken cancellationToken = default)
        {
            Session = session;
            return ValueTask.CompletedTask;
        }

        public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            Session = null;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private static IReadOnlyList<string> GetNavBadgeTexts(IRenderedComponent<MainLayout> cut)
    {
        return cut.FindAll(".portal-shell__nav-badge")
            .Select(item => item.TextContent)
            .ToArray();
    }

    private static IReadOnlyList<string> GetNotificationBadgeTexts(IRenderedComponent<MainLayout> cut)
    {
        return cut.FindAll(".notification-center__badge")
            .Select(item => item.TextContent)
            .ToArray();
    }
}
