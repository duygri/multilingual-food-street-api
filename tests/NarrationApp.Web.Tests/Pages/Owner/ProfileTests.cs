using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Auth;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Web.Pages.Owner;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Owner;

public sealed class ProfileTests : TestContext
{
    [Fact]
    public void Profile_page_renders_owner_values_and_activity_summary()
    {
        Services.AddSingleton<IOwnerProfileService>(new TestOwnerProfileService());

        var cut = RenderComponent<Profile>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Profile workspace", cut.Markup);
            Assert.Contains("Bà Tám Bún Bò", cut.Markup);
            Assert.Contains("owner@narration.app", cut.Markup);
            Assert.Contains("POI Owner", cut.Markup);
            Assert.Contains("Vĩnh Khánh - Quận 4", cut.Markup);
            Assert.Contains("4 POI", cut.Markup);
            Assert.Contains("2 published", cut.Markup);
            Assert.Contains("96 lượt nghe", cut.Markup);
        });

        Assert.Equal("readonly", cut.Find("input[data-field='owner-email']").GetAttribute("readonly"));
        Assert.Equal("readonly", cut.Find("input[data-field='owner-role']").GetAttribute("readonly"));
    }

    [Fact]
    public void Profile_page_updates_editable_owner_profile_fields()
    {
        var service = new TestOwnerProfileService();
        Services.AddSingleton<IOwnerProfileService>(service);

        var cut = RenderComponent<Profile>();

        cut.WaitForAssertion(() => Assert.Contains("Thông tin hồ sơ", cut.Markup));
        cut.Find("input[data-field='owner-full-name']").Change("Cô Tám Vĩnh Khánh");
        cut.Find("input[data-field='owner-phone']").Change("+84 90 999 8888");
        cut.Find("input[data-field='owner-managed-area']").Change("Quận 4 mở rộng");
        cut.Find("select[data-field='owner-preferred-language']").Change("en");
        cut.Find("button[data-action='save-profile']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(service.UpdateRequests);
            Assert.Contains("Đã cập nhật hồ sơ owner.", cut.Markup);
        });

        Assert.Equal("Cô Tám Vĩnh Khánh", service.UpdateRequests[0].FullName);
        Assert.Equal("+84 90 999 8888", service.UpdateRequests[0].Phone);
        Assert.Equal("Quận 4 mở rộng", service.UpdateRequests[0].ManagedArea);
        Assert.Equal("en", service.UpdateRequests[0].PreferredLanguage);
    }

    [Fact]
    public void Profile_page_validates_and_submits_password_change()
    {
        var service = new TestOwnerProfileService();
        Services.AddSingleton<IOwnerProfileService>(service);

        var cut = RenderComponent<Profile>();

        cut.WaitForAssertion(() => Assert.Contains("Đổi mật khẩu", cut.Markup));
        cut.Find("input[data-field='current-password']").Change("old-secret");
        cut.Find("input[data-field='new-password']").Change("new-secret-123");
        cut.Find("input[data-field='confirm-password']").Change("different-secret");
        cut.Find("button[data-action='change-password']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Mật khẩu xác nhận không khớp.", cut.Markup);
            Assert.Empty(service.ChangePasswordRequests);
        });

        cut.Find("input[data-field='confirm-password']").Change("new-secret-123");
        cut.Find("button[data-action='change-password']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(service.ChangePasswordRequests);
            Assert.Contains("Đã đổi mật khẩu owner.", cut.Markup);
        });

        Assert.Equal("old-secret", service.ChangePasswordRequests[0].CurrentPassword);
        Assert.Equal("new-secret-123", service.ChangePasswordRequests[0].NewPassword);
    }

    [Fact]
    public void Profile_page_surfaces_unexpected_profile_update_errors()
    {
        var service = new TestOwnerProfileService
        {
            UpdateException = new InvalidOperationException("Không thể lưu hồ sơ lúc này.")
        };
        Services.AddSingleton<IOwnerProfileService>(service);

        var cut = RenderComponent<Profile>();

        cut.WaitForAssertion(() => Assert.Contains("Thông tin hồ sơ", cut.Markup));
        cut.Find("button[data-action='save-profile']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Không thể lưu hồ sơ lúc này.", cut.Markup);
            Assert.Single(service.UpdateRequests);
        });
    }

    [Fact]
    public void Profile_page_surfaces_unexpected_password_change_errors()
    {
        var service = new TestOwnerProfileService
        {
            ChangePasswordException = new InvalidOperationException("Không thể đổi mật khẩu lúc này.")
        };
        Services.AddSingleton<IOwnerProfileService>(service);

        var cut = RenderComponent<Profile>();

        cut.WaitForAssertion(() => Assert.Contains("Đổi mật khẩu", cut.Markup));
        cut.Find("input[data-field='current-password']").Change("old-secret");
        cut.Find("input[data-field='new-password']").Change("new-secret-123");
        cut.Find("input[data-field='confirm-password']").Change("new-secret-123");
        cut.Find("button[data-action='change-password']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Không thể đổi mật khẩu lúc này.", cut.Markup);
            Assert.Single(service.ChangePasswordRequests);
        });
    }

    private sealed class TestOwnerProfileService : IOwnerProfileService
    {
        private OwnerProfileDto _profile = BuildProfile();

        public List<UpdateOwnerProfileRequest> UpdateRequests { get; } = [];

        public List<ChangePasswordRequest> ChangePasswordRequests { get; } = [];

        public Exception? UpdateException { get; init; }

        public Exception? ChangePasswordException { get; init; }

        public Task<OwnerProfileDto> GetProfileAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_profile);
        }

        public Task<OwnerProfileDto> UpdateProfileAsync(UpdateOwnerProfileRequest request, CancellationToken cancellationToken = default)
        {
            UpdateRequests.Add(request);
            if (UpdateException is not null)
            {
                throw UpdateException;
            }

            _profile = BuildProfile(request.FullName, request.Phone, request.ManagedArea, request.PreferredLanguage);
            return Task.FromResult(_profile);
        }

        public Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
        {
            ChangePasswordRequests.Add(request);
            if (ChangePasswordException is not null)
            {
                throw ChangePasswordException;
            }

            return Task.CompletedTask;
        }

        private static OwnerProfileDto BuildProfile(
            string fullName = "Bà Tám Bún Bò",
            string? phone = "+84 90 123 4567",
            string? managedArea = "Vĩnh Khánh - Quận 4",
            string preferredLanguage = "vi")
        {
            return new OwnerProfileDto
            {
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                FullName = fullName,
                Email = "owner@narration.app",
                Phone = phone,
                ManagedArea = managedArea,
                PreferredLanguage = preferredLanguage,
                CreatedAtUtc = new DateTime(2026, 3, 20, 8, 0, 0, DateTimeKind.Utc),
                LastLoginAtUtc = new DateTime(2026, 4, 22, 10, 0, 0, DateTimeKind.Utc),
                ActivitySummary = new OwnerActivitySummaryDto
                {
                    TotalPois = 4,
                    PublishedPois = 2,
                    DraftPois = 1,
                    PendingReviewPois = 1,
                    TotalAudioAssets = 7,
                    TotalAudioPlays = 96,
                    UnreadNotifications = 3
                }
            };
        }
    }
}
