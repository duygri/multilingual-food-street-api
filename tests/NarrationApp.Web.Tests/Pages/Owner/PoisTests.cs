using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Pages.Owner;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Owner;

public sealed class PoisTests : TestContext
{
    [Fact]
    public void Pois_behavior_is_split_into_focused_partials()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var pageRoot = Path.Combine(projectRoot, "src", "NarrationApp.Web", "Pages", "Owner");
        var markupPath = Path.Combine(pageRoot, "Pois.razor");
        var expectedPartials = new[]
        {
            ("Pois.razor.cs", "OnInitializedAsync"),
            ("Pois.Filters.razor.cs", "MatchesSearch"),
            ("Pois.Presentation.razor.cs", "GetPoiStatusLabel")
        };

        var markup = File.ReadAllText(markupPath);
        Assert.DoesNotContain("@code", markup, StringComparison.Ordinal);

        foreach (var (fileName, marker) in expectedPartials)
        {
            var path = Path.Combine(pageRoot, fileName);
            Assert.True(File.Exists(path), $"{fileName} should exist.");
            var source = File.ReadAllText(path);
            Assert.Contains("partial class Pois", source, StringComparison.Ordinal);
            Assert.Contains(marker, source, StringComparison.Ordinal);
        }

        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "Pois.razor.cs")).Length <= 40);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "Pois.Filters.razor.cs")).Length <= 50);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "Pois.Presentation.razor.cs")).Length <= 80);
    }

    [Fact]
    public void List_page_renders_workspace_stat_cards_toolbar_and_table_headers()
    {
        var ownerService = new TestOwnerPortalService();
        Services.AddSingleton<IOwnerPortalService>(ownerService);

        var cut = RenderComponent<Pois>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Tổng POI", cut.Markup);
            Assert.Contains("Đang xuất bản", cut.Markup);
            Assert.Contains("Chờ duyệt", cut.Markup);
            Assert.Contains("Nháp / Từ chối", cut.Markup);
            Assert.Contains("Danh sách POI", cut.Markup);
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
        });

        var headers = cut.FindAll("th").Select(header => header.TextContent.Trim()).ToArray();
        Assert.Contains("POI", headers);
        Assert.Contains("DANH MỤC", headers);
        Assert.Contains("TỌA ĐỘ", headers);
        Assert.Contains("PRIORITY", headers);
        Assert.Contains("NỘI DUNG NGUỒN", headers);
        Assert.Contains("TRẠNG THÁI", headers);
        Assert.Contains("THAO TÁC", headers);
    }

    [Fact]
    public void List_page_renders_workspace_rows_with_source_content_labels()
    {
        var ownerService = new TestOwnerPortalService();
        Services.AddSingleton<IOwnerPortalService>(ownerService);

        var cut = RenderComponent<Pois>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Script TTS", cut.Markup);
            Assert.Contains("Audio file", cut.Markup);
            Assert.Contains("Chưa có", cut.Markup);
            Assert.Contains("Tạo POI mới", cut.Markup);
        });
    }

    private sealed class TestOwnerPortalService : IOwnerPortalService
    {
        private readonly OwnerPoisWorkspaceDto _workspace = new()
        {
            Summary = new OwnerPoisWorkspaceSummaryDto
            {
                TotalPois = 3,
                PublishedPois = 1,
                PendingReviewPois = 1,
                DraftOrRejectedPois = 1
            },
            Rows =
            [
                new OwnerPoisWorkspaceRowDto
                {
                    PoiId = 1,
                    PoiName = "Bún mắm Vĩnh Khánh",
                    Slug = "bun-mam-vinh-khanh",
                    CategoryName = "Hải sản",
                    Latitude = 10.758,
                    Longitude = 106.701,
                    Priority = 10,
                    SourceContentKind = OwnerSourceContentKind.ScriptTts,
                    Status = PoiStatus.Published,
                    CanResubmit = false
                },
                new OwnerPoisWorkspaceRowDto
                {
                    PoiId = 2,
                    PoiName = "Ốc đêm Vĩnh Khánh",
                    Slug = "oc-dem-vinh-khanh",
                    CategoryName = "Ăn vặt",
                    Latitude = 10.759,
                    Longitude = 106.702,
                    Priority = 8,
                    SourceContentKind = OwnerSourceContentKind.AudioFile,
                    Status = PoiStatus.PendingReview,
                    CanResubmit = false
                },
                new OwnerPoisWorkspaceRowDto
                {
                    PoiId = 3,
                    PoiName = "Cơm tấm than hồng",
                    Slug = "com-tam-than-hong",
                    CategoryName = "Cơm",
                    Latitude = 10.760,
                    Longitude = 106.703,
                    Priority = 6,
                    SourceContentKind = OwnerSourceContentKind.None,
                    Status = PoiStatus.Draft,
                    CanResubmit = true
                }
            ]
        };

        public Task<OwnerPoisWorkspaceDto> GetPoisWorkspaceAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_workspace);

        public Task<OwnerShellSummaryDto> GetShellSummaryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new OwnerShellSummaryDto());

        public Task<OwnerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new OwnerDashboardDto());

        public Task<IReadOnlyList<PoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PoiDto>>(
                _workspace.Rows.Select(row => new PoiDto
                {
                    Id = row.PoiId,
                    Name = row.PoiName,
                    Slug = row.Slug,
                    CategoryName = row.CategoryName,
                    Lat = row.Latitude,
                    Lng = row.Longitude,
                    Priority = row.Priority,
                    Status = row.Status
                }).ToArray());

        public Task<PoiDto> GetPoiAsync(int poiId, CancellationToken cancellationToken = default)
            => Task.FromResult(_workspace.Rows.Where(row => row.PoiId == poiId).Select(row => new PoiDto
            {
                Id = row.PoiId,
                Name = row.PoiName,
                Slug = row.Slug,
                CategoryName = row.CategoryName,
                Lat = row.Latitude,
                Lng = row.Longitude,
                Priority = row.Priority,
                Status = row.Status
            }).Single());

        public Task<OwnerPoiStatsDto> GetPoiStatsAsync(int poiId, CancellationToken cancellationToken = default)
            => Task.FromResult(new OwnerPoiStatsDto { PoiId = poiId });

        public Task<PoiDto> CreatePoiAsync(CreatePoiRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PoiDto> UpdatePoiAsync(int poiId, UpdatePoiRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeletePoiAsync(int poiId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
