using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Category;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Pages.Owner;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Owner;

public sealed class PoiCreateTests : TestContext
{
    [Fact]
    public void Create_page_renders_workspace_sections_file_picker_and_dual_actions()
    {
        Services.AddSingleton<IOwnerPortalService>(new TestOwnerPortalService());
        Services.AddSingleton<ICategoryPortalService>(new TestCategoryPortalService());
        Services.AddSingleton<IModerationPortalService>(new TestModerationPortalService());

        var cut = RenderComponent<PoiCreate>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Tạo POI mới", cut.Markup);
            Assert.Contains("Thông tin POI", cut.Markup);
            Assert.Contains("Ảnh minh họa", cut.Markup);
            Assert.Contains("Nội dung nguồn", cut.Markup);
            Assert.Contains("Lưu nháp", cut.Markup);
            Assert.Contains("Gửi duyệt", cut.Markup);
            Assert.DoesNotContain("URL hình ảnh", cut.Markup);
        });
    }

    [Fact]
    public void Create_page_saves_draft_and_uploads_selected_image_before_navigating()
    {
        var ownerService = new TestOwnerPortalService();
        Services.AddSingleton<IOwnerPortalService>(ownerService);
        Services.AddSingleton<ICategoryPortalService>(new TestCategoryPortalService());
        Services.AddSingleton<IModerationPortalService>(new TestModerationPortalService());
        var navigation = Services.GetRequiredService<NavigationManager>();

        var cut = RenderComponent<PoiCreate>();

        FillCreateForm(cut);
        cut.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("fake image", "poi.png", contentType: "image/png"));
        cut.Find("button[data-action='save-draft']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(ownerService.CreateRequests);
            Assert.Single(ownerService.UploadImageRequests);
            Assert.Equal("http://localhost/owner/pois/42", navigation.Uri);
        });

        Assert.Equal("poi.png", ownerService.UploadImageRequests[0].FileName);
        Assert.Equal("image/png", ownerService.UploadImageRequests[0].ContentType);
    }

    [Fact]
    public void Create_page_submits_for_review_after_create_and_image_upload()
    {
        var ownerService = new TestOwnerPortalService();
        var moderationService = new TestModerationPortalService();
        Services.AddSingleton<IOwnerPortalService>(ownerService);
        Services.AddSingleton<ICategoryPortalService>(new TestCategoryPortalService());
        Services.AddSingleton<IModerationPortalService>(moderationService);

        var cut = RenderComponent<PoiCreate>();

        FillCreateForm(cut);
        cut.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("fake image", "poi.png", contentType: "image/png"));
        cut.Find("button[data-action='submit-review']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(ownerService.CreateRequests);
            Assert.Single(ownerService.UploadImageRequests);
            Assert.Single(moderationService.CreateRequests);
            Assert.Contains("Đã gửi POI vào hàng chờ duyệt.", cut.Markup);
        });

        Assert.Equal("poi", moderationService.CreateRequests[0].EntityType);
        Assert.Equal("42", moderationService.CreateRequests[0].EntityId);
    }

    private static void FillCreateForm(IRenderedComponent<PoiCreate> cut)
    {
        cut.WaitForAssertion(() => Assert.Contains("Thông tin POI", cut.Markup));
        cut.Find("input[data-field='poi-name']").Change("Ốc đêm Vĩnh Khánh");
        cut.Find("input[data-field='poi-slug']").Change("oc-dem-vinh-khanh");
        cut.Find("input[data-field='poi-lat']").Change("10.759");
        cut.Find("input[data-field='poi-lng']").Change("106.702");
        cut.Find("input[data-field='poi-priority']").Change("14");
        cut.Find("select[data-field='poi-category']").Change("2");
        cut.Find("textarea[data-field='poi-description']").Change("Quán ốc đêm đông khách.");
        cut.Find("textarea[data-field='poi-tts-script']").Change("Kịch bản nguồn để tạo audio.");
    }

    private sealed class TestOwnerPortalService : IOwnerPortalService
    {
        public List<CreatePoiRequest> CreateRequests { get; } = [];

        public List<ImageUploadCall> UploadImageRequests { get; } = [];

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
        {
            CreateRequests.Add(request);

            return Task.FromResult(new PoiDto
            {
                Id = 42,
                Name = request.Name,
                Slug = request.Slug,
                OwnerId = Guid.NewGuid(),
                Lat = request.Lat,
                Lng = request.Lng,
                Priority = request.Priority,
                CategoryId = request.CategoryId,
                NarrationMode = request.NarrationMode,
                Description = request.Description,
                TtsScript = request.TtsScript,
                Status = PoiStatus.Draft,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        public Task<PoiDto> UpdatePoiAsync(int poiId, UpdatePoiRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PoiDto> UploadPoiImageAsync(int poiId, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default)
        {
            UploadImageRequests.Add(new ImageUploadCall(poiId, fileName, contentType));
            return Task.FromResult(new PoiDto
            {
                Id = poiId,
                Name = "Ốc đêm Vĩnh Khánh",
                Slug = "oc-dem-vinh-khanh",
                OwnerId = Guid.NewGuid(),
                Lat = 10.759,
                Lng = 106.702,
                Priority = 14,
                ImageUrl = "https://cdn.test/images/poi.png",
                Status = PoiStatus.Draft,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        public Task<PoiDto> DeletePoiImageAsync(int poiId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeletePoiAsync(int poiId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public readonly record struct ImageUploadCall(int PoiId, string FileName, string ContentType);
    }

    private sealed class TestCategoryPortalService : ICategoryPortalService
    {
        public Task<IReadOnlyList<CategoryDto>> GetAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<CategoryDto>>(
            [
                new CategoryDto { Id = 1, Name = "Hải sản", Slug = "hai-san", Description = "Nhóm hải sản", Icon = "seafood", DisplayOrder = 1, IsActive = true },
                new CategoryDto { Id = 2, Name = "Ăn vặt", Slug = "an-vat", Description = "Nhóm ăn vặt", Icon = "snack", DisplayOrder = 2, IsActive = true }
            ]);
        }

        public Task<CategoryDto> CreateAsync(SaveCategoryRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CategoryDto> UpdateAsync(int id, SaveCategoryRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class TestModerationPortalService : IModerationPortalService
    {
        public List<CreateModerationRequest> CreateRequests { get; } = [];

        public Task<IReadOnlyList<ModerationRequestDto>> GetMineAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ModerationRequestDto>>(Array.Empty<ModerationRequestDto>());

        public Task<ModerationRequestDto> CreateAsync(CreateModerationRequest request, CancellationToken cancellationToken = default)
        {
            CreateRequests.Add(request);

            return Task.FromResult(new ModerationRequestDto
            {
                Id = 301,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                Status = ModerationStatus.Pending,
                RequestedBy = Guid.NewGuid(),
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }
}
