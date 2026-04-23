using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Category;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Pages.Owner;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Owner;

public sealed class PoiCreateTests : TestContext
{
    [Fact]
    public void Create_page_renders_clear_form_sections()
    {
        Services.AddSingleton<IOwnerPortalService>(new TestOwnerPortalService());
        Services.AddSingleton<ICategoryPortalService>(new TestCategoryPortalService());

        var cut = RenderComponent<PoiCreate>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Tạo POI mới", cut.Markup);
            Assert.Contains("Thông tin cơ bản", cut.Markup);
            Assert.Contains("Vị trí và danh mục", cut.Markup);
            Assert.Contains("Hình ảnh đại diện", cut.Markup);
            Assert.Contains("Mô tả nguồn", cut.Markup);
            Assert.Contains("Kịch bản TTS", cut.Markup);
            Assert.Contains("Audio nguồn", cut.Markup);
            Assert.Contains("Geofence mặc định", cut.Markup);
            Assert.DoesNotContain("Trình biên tập POI", cut.Markup);
        });
    }

    [Fact]
    public void Create_success_navigates_to_created_poi_detail_route()
    {
        var ownerService = new TestOwnerPortalService();
        Services.AddSingleton<IOwnerPortalService>(ownerService);
        Services.AddSingleton<ICategoryPortalService>(new TestCategoryPortalService());
        var navigation = Services.GetRequiredService<NavigationManager>();

        var cut = RenderComponent<PoiCreate>();

        cut.WaitForAssertion(() => Assert.Contains("Thông tin cơ bản", cut.Markup));
        cut.Find("input[data-field='poi-name']").Change("Ốc đêm Vĩnh Khánh");
        cut.Find("input[data-field='poi-slug']").Change("oc-dem-vinh-khanh");
        cut.Find("input[data-field='poi-lat']").Change("10.759");
        cut.Find("input[data-field='poi-lng']").Change("106.702");
        cut.Find("input[data-field='poi-priority']").Change("14");
        cut.Find("select[data-field='poi-category']").Change("2");
        cut.Find("textarea[data-field='poi-description']").Change("Quán ốc đêm đông khách.");
        cut.Find("textarea[data-field='poi-tts-script']").Change("Kịch bản nguồn để tạo audio.");
        cut.Find("button[data-action='create-poi']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("http://localhost/owner/pois/42", navigation.Uri);
            Assert.Single(ownerService.CreateRequests);
            Assert.Equal("Ốc đêm Vĩnh Khánh", ownerService.CreateRequests[0].Name);
            Assert.Equal("oc-dem-vinh-khanh", ownerService.CreateRequests[0].Slug);
            Assert.Equal(2, ownerService.CreateRequests[0].CategoryId);
        });
    }

    private sealed class TestOwnerPortalService : IOwnerPortalService
    {
        public List<CreatePoiRequest> CreateRequests { get; } = [];

        public Task<OwnerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OwnerDashboardDto());
        }

        public Task<IReadOnlyList<PoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PoiDto>>(Array.Empty<PoiDto>());
        }

        public Task<PoiDto> GetPoiAsync(int poiId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<OwnerPoiStatsDto> GetPoiStatsAsync(int poiId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

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
        {
            throw new NotSupportedException();
        }

        public Task DeletePoiAsync(int poiId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
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
        {
            throw new NotSupportedException();
        }

        public Task<CategoryDto> UpdateAsync(int id, SaveCategoryRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
