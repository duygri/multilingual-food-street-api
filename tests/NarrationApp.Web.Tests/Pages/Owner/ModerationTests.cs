using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Pages.Owner;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Owner;

public sealed class ModerationTests : TestContext
{
    [Fact]
    public void Moderation_page_renders_workspace_stat_cards_pending_table_and_history_table()
    {
        Services.AddSingleton<IModerationPortalService>(new TestModerationPortalService());
        Services.AddSingleton<IOwnerPortalService>(new TestOwnerPortalService());

        var cut = RenderComponent<Moderation>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Đang chờ", cut.Markup);
            Assert.Contains("Đã duyệt", cut.Markup);
            Assert.Contains("Bị từ chối", cut.Markup);
            Assert.Contains("Yêu cầu đang chờ duyệt", cut.Markup);
            Assert.Contains("Lịch sử duyệt", cut.Markup);
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.Contains("Thiếu mô tả nguồn rõ ràng.", cut.Markup);
        });

        var headers = cut.FindAll("th").Select(header => header.TextContent.Trim()).ToArray();
        Assert.Contains("POI", headers);
        Assert.Contains("LOẠI YÊU CẦU", headers);
        Assert.Contains("GỬI LÚC", headers);
        Assert.Contains("CHỜ DUYỆT", headers);
        Assert.Contains("KẾT QUẢ", headers);
        Assert.Contains("GHI CHÚ ADMIN", headers);
        Assert.Contains("THAO TÁC", headers);
    }

    [Fact]
    public void Moderation_page_renders_empty_pending_workspace_state_when_no_pending_rows_exist()
    {
        Services.AddSingleton<IModerationPortalService>(new EmptyModerationPortalService());
        Services.AddSingleton<IOwnerPortalService>(new EmptyOwnerPortalService());

        var cut = RenderComponent<Moderation>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Yêu cầu đang chờ duyệt", cut.Markup);
            Assert.Contains("Chưa có yêu cầu đang chờ duyệt.", cut.Markup);
            Assert.Contains("Lịch sử duyệt", cut.Markup);
        });
    }

    private sealed class TestOwnerPortalService : IOwnerPortalService
    {
        public Task<OwnerModerationWorkspaceDto> GetModerationWorkspaceAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new OwnerModerationWorkspaceDto
            {
                Summary = new OwnerModerationWorkspaceSummaryDto
                {
                    PendingCount = 1,
                    ApprovedCount = 1,
                    RejectedCount = 1
                },
                FlowState = new OwnerModerationFlowStateDto
                {
                    DraftCount = 3,
                    PendingCount = 1,
                    NeedsChangesCount = 1,
                    ApprovedCount = 1
                },
                PendingRows =
                [
                    new OwnerModerationPendingRowDto
                    {
                        ModerationRequestId = 101,
                        PoiId = 1,
                        PoiName = "Bún mắm Vĩnh Khánh",
                        RequestType = "Gửi duyệt",
                        SubmittedAtUtc = DateTime.UtcNow.AddHours(-2),
                        WaitTimeLabel = "2 giờ",
                        ActionLabel = "Mở POI"
                    }
                ],
                HistoryRows =
                [
                    new OwnerModerationHistoryRowDto
                    {
                        ModerationRequestId = 102,
                        PoiId = 2,
                        PoiName = "Ốc đêm Vĩnh Khánh",
                        RequestType = "Gửi duyệt",
                        SubmittedAtUtc = DateTime.UtcNow.AddDays(-1),
                        ReviewedAtUtc = DateTime.UtcNow.AddHours(-5),
                        Result = "Bị từ chối",
                        AdminNote = "Thiếu mô tả nguồn rõ ràng.",
                        ActionLabel = "Sửa trong POI detail"
                    },
                    new OwnerModerationHistoryRowDto
                    {
                        ModerationRequestId = 103,
                        PoiId = 3,
                        PoiName = "Cơm tấm than hồng",
                        RequestType = "Gửi duyệt",
                        SubmittedAtUtc = DateTime.UtcNow.AddDays(-2),
                        ReviewedAtUtc = DateTime.UtcNow.AddDays(-1),
                        Result = "Đã duyệt",
                        AdminNote = "Đã duyệt nội dung.",
                        ActionLabel = "Xem POI"
                    }
                ]
            });

        public Task<OwnerShellSummaryDto> GetShellSummaryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new OwnerShellSummaryDto());

        public Task<OwnerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new OwnerDashboardDto());

        public Task<IReadOnlyList<PoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PoiDto>>(Array.Empty<PoiDto>());

        public Task<PoiDto> GetPoiAsync(int poiId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<OwnerPoiStatsDto> GetPoiStatsAsync(int poiId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PoiDto> CreatePoiAsync(CreatePoiRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PoiDto> UpdatePoiAsync(int poiId, UpdatePoiRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeletePoiAsync(int poiId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class EmptyOwnerPortalService : IOwnerPortalService
    {
        public Task<OwnerModerationWorkspaceDto> GetModerationWorkspaceAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new OwnerModerationWorkspaceDto
            {
                Summary = new OwnerModerationWorkspaceSummaryDto(),
                FlowState = new OwnerModerationFlowStateDto(),
                PendingRows = [],
                HistoryRows = []
            });

        public Task<OwnerShellSummaryDto> GetShellSummaryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new OwnerShellSummaryDto());

        public Task<OwnerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new OwnerDashboardDto());

        public Task<IReadOnlyList<PoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PoiDto>>(Array.Empty<PoiDto>());

        public Task<PoiDto> GetPoiAsync(int poiId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<OwnerPoiStatsDto> GetPoiStatsAsync(int poiId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PoiDto> CreatePoiAsync(CreatePoiRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PoiDto> UpdatePoiAsync(int poiId, UpdatePoiRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeletePoiAsync(int poiId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class TestModerationPortalService : IModerationPortalService
    {
        public Task<IReadOnlyList<ModerationRequestDto>> GetMineAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ModerationRequestDto>>(
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

        public Task<ModerationRequestDto> CreateAsync(CreateModerationRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class EmptyModerationPortalService : IModerationPortalService
    {
        public Task<IReadOnlyList<ModerationRequestDto>> GetMineAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ModerationRequestDto>>(Array.Empty<ModerationRequestDto>());

        public Task<ModerationRequestDto> CreateAsync(CreateModerationRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
