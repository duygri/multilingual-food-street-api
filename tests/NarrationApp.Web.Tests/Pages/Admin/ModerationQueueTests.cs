using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Pages.Admin;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Admin;

public sealed class ModerationQueueTests : TestContext
{
    [Fact]
    public void Moderation_queue_behavior_is_split_into_focused_partials()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var pageRoot = Path.Combine(projectRoot, "src", "NarrationApp.Web", "Pages", "Admin");
        var markupPath = Path.Combine(pageRoot, "ModerationQueue.razor");
        var expectedPartials = new[]
        {
            ("ModerationQueue.razor.cs", "OnInitializedAsync"),
            ("ModerationQueue.Actions.razor.cs", "ReviewAsync"),
            ("ModerationQueue.Presentation.razor.cs", "GetEntityTitle")
        };

        var markup = File.ReadAllText(markupPath);
        Assert.DoesNotContain("@code", markup, StringComparison.Ordinal);

        foreach (var (fileName, marker) in expectedPartials)
        {
            var path = Path.Combine(pageRoot, fileName);
            Assert.True(File.Exists(path), $"{fileName} should exist.");
            var source = File.ReadAllText(path);
            Assert.Contains("partial class ModerationQueue", source, StringComparison.Ordinal);
            Assert.Contains(marker, source, StringComparison.Ordinal);
        }

        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "ModerationQueue.razor.cs")).Length <= 40);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "ModerationQueue.Actions.razor.cs")).Length <= 70);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "ModerationQueue.Presentation.razor.cs")).Length <= 60);
    }

    [Fact]
    public void Approve_action_reviews_request_from_sample_strict_queue()
    {
        var service = new TestAdminPortalService(
        [
            new ModerationRequestDto
            {
                Id = 17,
                EntityType = "poi",
                EntityId = "4",
                RequestedBy = Guid.NewGuid(),
                Status = ModerationStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-20)
            }
        ]);

        Services.AddSingleton<IAdminPortalService>(service);

        var cut = RenderComponent<ModerationQueue>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find(".moderation-surface"));
            Assert.Contains("Moderation Queue", cut.Markup);
            Assert.Contains("Chờ duyệt", cut.Markup);
            Assert.Contains("Xem", cut.Markup);
            Assert.Contains("poi", cut.Markup);
            Assert.Contains("Duyệt", cut.Markup);
            Assert.Contains("Từ chối", cut.Markup);
            Assert.DoesNotContain("Tuyến xử lý moderation", cut.Markup);
        });
        cut.Find("button[data-action='approve-17']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("approve-17", cut.Markup);
            Assert.Contains("Hàng đợi moderation đã trống", cut.Markup);
            Assert.Contains("empty-state-block", cut.Markup);
        });

        Assert.Equal(new[] { 17 }, service.ApprovedIds);
    }

    private sealed class TestAdminPortalService(IReadOnlyList<ModerationRequestDto> seedItems) : IAdminPortalService
    {
        private readonly List<ModerationRequestDto> _items = seedItems.ToList();

        public List<int> ApprovedIds { get; } = [];

        public Task<DashboardDto> GetOverviewAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DashboardDto());
        }

        public Task<IReadOnlyList<AdminPoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AdminPoiDto>>(Array.Empty<AdminPoiDto>());
        }

        public Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<UserSummaryDto>>(Array.Empty<UserSummaryDto>());
        }

        public Task<IReadOnlyList<VisitorDeviceSummaryDto>> GetVisitorDevicesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<VisitorDeviceSummaryDto>>(Array.Empty<VisitorDeviceSummaryDto>());
        }

        public Task<IReadOnlyList<ModerationRequestDto>> GetPendingModerationAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ModerationRequestDto>>(_items.ToArray());
        }

        public Task<ModerationRequestDto> ApproveModerationAsync(int requestId, ReviewModerationRequest request, CancellationToken cancellationToken = default)
        {
            ApprovedIds.Add(requestId);
            var item = _items.Single(entry => entry.Id == requestId);
            _items.Remove(item);

            return Task.FromResult(new ModerationRequestDto
            {
                Id = item.Id,
                EntityType = item.EntityType,
                EntityId = item.EntityId,
                RequestedBy = item.RequestedBy,
                ReviewedBy = item.ReviewedBy,
                Status = ModerationStatus.Approved,
                ReviewNote = request.ReviewNote,
                CreatedAtUtc = item.CreatedAtUtc
            });
        }

        public Task<ModerationRequestDto> RejectModerationAsync(int requestId, ReviewModerationRequest request, CancellationToken cancellationToken = default)
        {
            var item = _items.Single(entry => entry.Id == requestId);
            _items.Remove(item);

            return Task.FromResult(new ModerationRequestDto
            {
                Id = item.Id,
                EntityType = item.EntityType,
                EntityId = item.EntityId,
                RequestedBy = item.RequestedBy,
                ReviewedBy = item.ReviewedBy,
                Status = ModerationStatus.Rejected,
                ReviewNote = request.ReviewNote,
                CreatedAtUtc = item.CreatedAtUtc
            });
        }

        public Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<TopPoiDto>> GetTopPoisAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AudioPlayAnalyticsDto> GetAudioPlayAnalyticsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
