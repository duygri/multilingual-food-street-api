using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.DTOs.Tour;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Pages.Admin;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Admin;

public sealed class TourManagementTests : TestContext
{
    [Fact]
    public void Tour_management_behavior_is_split_into_focused_partials()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var pageRoot = Path.Combine(projectRoot, "src", "NarrationApp.Web", "Pages", "Admin");
        var markupPath = Path.Combine(pageRoot, "TourManagement.razor");
        var expectedPartials = new[]
        {
            ("TourManagement.razor.cs", "OnInitializedAsync"),
            ("TourManagement.Editor.razor.cs", "BeginCreateTour"),
            ("TourManagement.Actions.razor.cs", "SaveTourAsync"),
            ("TourManagement.Presentation.razor.cs", "GetTourStatusLabel")
        };

        var markup = File.ReadAllText(markupPath);
        Assert.DoesNotContain("@code", markup, StringComparison.Ordinal);

        foreach (var (fileName, marker) in expectedPartials)
        {
            var path = Path.Combine(pageRoot, fileName);
            Assert.True(File.Exists(path), $"{fileName} should exist.");
            var source = File.ReadAllText(path);
            Assert.Contains("partial class TourManagement", source, StringComparison.Ordinal);
            Assert.Contains(marker, source, StringComparison.Ordinal);
        }

        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "TourManagement.razor.cs")).Length <= 90);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "TourManagement.Editor.razor.cs")).Length <= 110);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "TourManagement.Actions.razor.cs")).Length <= 120);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "TourManagement.Presentation.razor.cs")).Length <= 80);
    }

    [Fact]
    public void Create_publish_and_delete_tour_updates_sample_strict_tour_table()
    {
        var service = new TestTourPortalService();
        Services.AddSingleton<ITourPortalService>(service);

        var cut = RenderComponent<TourManagement>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find(".tour-surface"));
            Assert.Contains("Tour Management", cut.Markup);
            Assert.Contains("Quản lý Tour", cut.Markup);
            Assert.Contains("Khám phá Xóm Chiếu", cut.Markup);
            Assert.Contains("Tổng điểm dừng", cut.Markup);
            Assert.Contains("Draft", cut.Markup);
            Assert.DoesNotContain("Lượt tham gia", cut.Markup);
            Assert.DoesNotContain("Ước lượng từ các tuyến đang có", cut.Markup);
            Assert.DoesNotContain("Tạo mã truy cập", cut.Markup);
            Assert.Empty(cut.FindAll("input[data-field='tour-title']"));
            Assert.Empty(cut.FindAll("[data-panel='tour-editor']"));
        });

        cut.Find("button[data-action='new-tour']").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-panel='tour-editor']"));
            Assert.Single(cut.FindAll("input[data-field='tour-title']"));
        });
        cut.Find("input[data-field='tour-title']").Change("Tour vị đêm");
        cut.Find("textarea[data-field='tour-description']").Change("Lộ trình ăn đêm dọc phố Vĩnh Khánh.");
        cut.Find("input[data-field='tour-minutes']").Change("42");
        cut.Find("select[data-field='stop-poi-1']").Change("1");
        cut.Find("input[data-field='stop-radius-1']").Change("80");
        cut.Find("button[data-action='save-tour']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Đã tạo tour mới", cut.Markup);
            Assert.Contains("Tour vị đêm", cut.Markup);
        });

        cut.Find("button[data-action='edit-tour-10']").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-panel='tour-editor']")));
        cut.Find("select[data-field='tour-status']").Change("Published");
        cut.Find("button[data-action='save-tour']").Click();

        cut.WaitForAssertion(() => Assert.Contains("Published", cut.Markup));

        cut.Find("button[data-action='delete-tour']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Tour vị đêm", cut.Markup);
            Assert.Contains("Khám phá Xóm Chiếu", cut.Markup);
        });

        Assert.Single(service.CreateRequests);
        Assert.Single(service.UpdateRequests);
        Assert.Single(service.DeleteRequests);
        Assert.Equal(TourStatus.Published, service.UpdateRequests[0].Request.Status);
    }

    private sealed class TestTourPortalService : ITourPortalService
    {
        private readonly List<TourDto> _tours =
        [
            new TourDto
            {
                Id = 7,
                Title = "Khám phá Xóm Chiếu",
                Description = "Tour ẩm thực đường phố.",
                EstimatedMinutes = 45,
                Status = TourStatus.Published,
                Stops =
                [
                    new TourStopDto { Id = 71, TourId = 7, PoiId = 1, Sequence = 1, RadiusMeters = 60 },
                    new TourStopDto { Id = 72, TourId = 7, PoiId = 2, Sequence = 2, RadiusMeters = 60 }
                ]
            },
            new TourDto
            {
                Id = 8,
                Title = "Di tích lịch sử Q4",
                Description = "Tour văn hóa lịch sử.",
                EstimatedMinutes = 60,
                Status = TourStatus.Published,
                Stops =
                [
                    new TourStopDto { Id = 81, TourId = 8, PoiId = 1, Sequence = 1, RadiusMeters = 60 }
                ]
            },
            new TourDto
            {
                Id = 9,
                Title = "Đêm Khánh Hội",
                Description = "Tour về đêm.",
                EstimatedMinutes = 35,
                Status = TourStatus.Draft,
                Stops =
                [
                    new TourStopDto { Id = 91, TourId = 9, PoiId = 2, Sequence = 1, RadiusMeters = 60 }
                ]
            }
        ];

        private readonly IReadOnlyList<PoiDto> _pois =
        [
            new PoiDto { Id = 1, Name = "Bún mắm Vĩnh Khánh", Slug = "bun-mam-vinh-khanh", Status = PoiStatus.Published },
            new PoiDto { Id = 2, Name = "Ốc đêm", Slug = "oc-dem", Status = PoiStatus.Published }
        ];

        public List<CreateTourRequest> CreateRequests { get; } = [];

        public List<(int Id, UpdateTourRequest Request)> UpdateRequests { get; } = [];

        public List<int> DeleteRequests { get; } = [];

        public Task<IReadOnlyList<TourDto>> GetToursAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TourDto>>(_tours.ToArray());
        }

        public Task<IReadOnlyList<PoiDto>> GetPoiOptionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_pois);
        }

        public Task<TourDto> CreateTourAsync(CreateTourRequest request, CancellationToken cancellationToken = default)
        {
            CreateRequests.Add(request);

            var created = new TourDto
            {
                Id = 10,
                Title = request.Title,
                Description = request.Description,
                EstimatedMinutes = request.EstimatedMinutes,
                CoverImage = request.CoverImage,
                Status = TourStatus.Draft,
                Stops = request.Stops
                    .Select((stop, index) => new TourStopDto
                    {
                        Id = 800 + index,
                        TourId = 10,
                        PoiId = stop.PoiId,
                        Sequence = stop.Sequence,
                        RadiusMeters = stop.RadiusMeters
                    })
                    .ToArray()
            };

            _tours.Insert(0, created);
            return Task.FromResult(created);
        }

        public Task<TourDto> UpdateTourAsync(int id, UpdateTourRequest request, CancellationToken cancellationToken = default)
        {
            UpdateRequests.Add((id, request));

            var updated = new TourDto
            {
                Id = id,
                Title = request.Title,
                Description = request.Description,
                EstimatedMinutes = request.EstimatedMinutes,
                CoverImage = request.CoverImage,
                Status = request.Status,
                Stops = request.Stops
                    .Select((stop, index) => new TourStopDto
                    {
                        Id = 900 + index,
                        TourId = id,
                        PoiId = stop.PoiId,
                        Sequence = stop.Sequence,
                        RadiusMeters = stop.RadiusMeters
                    })
                    .ToArray()
            };

            _tours[_tours.FindIndex(item => item.Id == id)] = updated;
            return Task.FromResult(updated);
        }

        public Task DeleteTourAsync(int id, CancellationToken cancellationToken = default)
        {
            DeleteRequests.Add(id);
            _tours.RemoveAll(item => item.Id == id);
            return Task.CompletedTask;
        }
    }

}
