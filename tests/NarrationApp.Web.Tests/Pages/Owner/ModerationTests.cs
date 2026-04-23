using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Pages.Owner;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Owner;

public sealed class ModerationTests : TestContext
{
    [Fact]
    public void Moderation_page_renders_stepper_pending_history_and_rejection_cta()
    {
        Services.AddSingleton<IModerationPortalService>(new TestModerationPortalService());

        var cut = RenderComponent<Moderation>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Moderation workspace", cut.Markup);
            Assert.Contains("Soạn nội dung", cut.Markup);
            Assert.Contains("Gửi duyệt", cut.Markup);
            Assert.Contains("Admin phản hồi", cut.Markup);
            Assert.Contains("Xuất bản", cut.Markup);
            Assert.Contains("POI #1", cut.Markup);
            Assert.Contains("Đang chờ admin", cut.Markup);
            Assert.Contains("Lịch sử moderation", cut.Markup);
            Assert.Contains("Thiếu mô tả nguồn rõ ràng.", cut.Markup);
            Assert.Contains("Sửa trong POI detail", cut.Markup);
            Assert.Contains("/owner/pois/2", cut.Markup);
            Assert.DoesNotContain("Moderation Queue", cut.Markup);
        });
    }

    [Fact]
    public void Moderation_page_renders_empty_state_when_owner_has_no_requests()
    {
        Services.AddSingleton<IModerationPortalService>(new EmptyModerationPortalService());

        var cut = RenderComponent<Moderation>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Chưa có yêu cầu moderation", cut.Markup);
            Assert.Contains("Khi owner gửi POI đi duyệt, trạng thái xử lý sẽ xuất hiện tại đây.", cut.Markup);
            Assert.Empty(cut.FindAll(".owner-moderation-pending__item"));
            Assert.Empty(cut.FindAll("tbody tr"));
        });
    }

    [Fact]
    public void Moderation_page_renders_error_state_for_transport_or_parse_failures()
    {
        Services.AddSingleton<IModerationPortalService>(new FailingModerationPortalService());

        var cut = RenderComponent<Moderation>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Không thể tải moderation workspace", cut.Markup);
            Assert.Contains("Không đọc được dữ liệu moderation.", cut.Markup);
        });
    }

    [Fact]
    public void Moderation_page_uses_generic_actions_for_non_poi_moderation_items()
    {
        Services.AddSingleton<IModerationPortalService>(new NonPoiModerationPortalService());

        var cut = RenderComponent<Moderation>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Owner registration #owner-42", cut.Markup);
            Assert.Contains("Mở dashboard", cut.Markup);
            Assert.Contains("Xem dashboard", cut.Markup);
            Assert.DoesNotContain("/owner/pois/owner-42", cut.Markup);
            Assert.DoesNotContain("Sửa trong POI detail", cut.Markup);
        });
    }

    private sealed class TestModerationPortalService : IModerationPortalService
    {
        public Task<IReadOnlyList<ModerationRequestDto>> GetMineAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ModerationRequestDto>>(
            [
                new ModerationRequestDto
                {
                    Id = 101,
                    EntityType = "poi",
                    EntityId = "1",
                    Status = ModerationStatus.Pending,
                    RequestedBy = Guid.NewGuid(),
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-2)
                },
                new ModerationRequestDto
                {
                    Id = 102,
                    EntityType = "poi",
                    EntityId = "2",
                    Status = ModerationStatus.Rejected,
                    RequestedBy = Guid.NewGuid(),
                    ReviewedBy = Guid.NewGuid(),
                    ReviewNote = "Thiếu mô tả nguồn rõ ràng.",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
                },
                new ModerationRequestDto
                {
                    Id = 103,
                    EntityType = "poi",
                    EntityId = "3",
                    Status = ModerationStatus.Approved,
                    RequestedBy = Guid.NewGuid(),
                    ReviewedBy = Guid.NewGuid(),
                    ReviewNote = "Đã duyệt nội dung.",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
                }
            ]);
        }

        public Task<ModerationRequestDto> CreateAsync(CreateModerationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class EmptyModerationPortalService : IModerationPortalService
    {
        public Task<IReadOnlyList<ModerationRequestDto>> GetMineAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ModerationRequestDto>>(Array.Empty<ModerationRequestDto>());
        }

        public Task<ModerationRequestDto> CreateAsync(CreateModerationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FailingModerationPortalService : IModerationPortalService
    {
        public Task<IReadOnlyList<ModerationRequestDto>> GetMineAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Không đọc được dữ liệu moderation.");
        }

        public Task<ModerationRequestDto> CreateAsync(CreateModerationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class NonPoiModerationPortalService : IModerationPortalService
    {
        public Task<IReadOnlyList<ModerationRequestDto>> GetMineAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ModerationRequestDto>>(
            [
                new ModerationRequestDto
                {
                    Id = 201,
                    EntityType = "owner_registration",
                    EntityId = "owner-42",
                    Status = ModerationStatus.Pending,
                    RequestedBy = Guid.NewGuid(),
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-30)
                }
            ]);
        }

        public Task<ModerationRequestDto> CreateAsync(CreateModerationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
