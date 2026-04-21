using System.Net;
using System.Net.Http;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Notification;
using NarrationApp.Shared.Enums;
using NarrationApp.SharedUI.Auth;
using NarrationApp.SharedUI.Services;
using NarrationApp.Web.Layout;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Layout;

public sealed class MainLayoutTests : TestContext
{
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
    }

    private void ConfigureAuthenticatedOwner()
    {
        var sessionStore = new TestAuthSessionStore
        {
            Session = new AuthSession
            {
                UserId = Guid.NewGuid(),
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

        Services.AddSingleton<IAuthSessionStore>(sessionStore);
        Services.AddSingleton(authStateProvider);
        Services.AddSingleton<AuthenticationStateProvider>(authStateProvider);
        Services.AddSingleton(new AuthClientService(apiClient, authStateProvider));
        Services.AddSingleton<INotificationCenterService>(new TestNotificationCenterService());
    }

    private sealed class TestNotificationCenterService : INotificationCenterService
    {
        public event Action? Changed;

        public ValueTask<IReadOnlyList<NotificationDto>> GetAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyList<NotificationDto>>(Array.Empty<NotificationDto>());
        }

        public ValueTask<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(0);
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
}
