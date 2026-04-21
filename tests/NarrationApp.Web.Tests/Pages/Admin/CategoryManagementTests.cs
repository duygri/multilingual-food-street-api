using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Category;
using NarrationApp.Web.Pages.Admin;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Admin;

public sealed class CategoryManagementTests : TestContext
{
    [Fact]
    public void Create_update_and_delete_category_from_sample_table_surface()
    {
        var service = new TestCategoryPortalService();
        Services.AddSingleton<ICategoryPortalService>(service);

        var cut = RenderComponent<CategoryManagement>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Hải sản", cut.Markup);
            Assert.Contains("Quản lý danh mục POI", cut.Markup);
            Assert.Contains("Tên danh mục", cut.Markup);
            Assert.DoesNotContain("Tạo danh mục mới</h2>", cut.Markup);
            Assert.DoesNotContain("Category editor", cut.Markup);
            Assert.Empty(cut.FindAll("[data-panel='category-editor']"));
        });

        cut.Find("button[data-action='new-category']").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-panel='category-editor']")));
        cut.Find("input[data-field='category-name']").Change("Món nướng");
        cut.Find("input[data-field='category-slug']").Change("mon-nuong");
        cut.Find("textarea[data-field='category-description']").Change("Nhóm món nướng phục vụ về đêm.");
        cut.Find("input[data-field='category-icon']").Change("🔥");
        cut.Find("input[data-field='category-order']").Change("9");
        cut.Find("button[data-action='save-category']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Đã tạo danh mục mới", cut.Markup);
            Assert.Contains("Món nướng", cut.Markup);
        });

        cut.Find("button[data-action='edit-category-1']").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-panel='category-editor']")));
        cut.Find("input[data-field='category-name']").Change("Hải sản tươi");
        cut.Find("button[data-action='save-category']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Đã cập nhật danh mục", cut.Markup);
            Assert.Contains("Hải sản tươi", cut.Markup);
        });

        cut.Find("button[data-action='delete-category-1']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Hải sản tươi", cut.Markup);
            Assert.Contains("Bún/Phở", cut.Markup);
        });

        Assert.Single(service.CreateRequests);
        Assert.Single(service.UpdateRequests);
        Assert.Single(service.DeleteRequests);
    }

    private sealed class TestCategoryPortalService : ICategoryPortalService
    {
        private readonly List<CategoryDto> _categories =
        [
            new CategoryDto
            {
                Id = 1,
                Name = "Hải sản",
                Slug = "hai-san",
                Description = "Các quầy hải sản đêm nổi bật.",
                Icon = "🦐",
                DisplayOrder = 1,
                IsActive = true
            },
            new CategoryDto
            {
                Id = 2,
                Name = "Bún/Phở",
                Slug = "bun-pho",
                Description = "Các quầy bún, phở phục vụ khuya.",
                Icon = "🍜",
                DisplayOrder = 2,
                IsActive = true
            }
        ];

        public List<SaveCategoryRequest> CreateRequests { get; } = [];

        public List<(int Id, SaveCategoryRequest Request)> UpdateRequests { get; } = [];

        public List<int> DeleteRequests { get; } = [];

        public Task<IReadOnlyList<CategoryDto>> GetAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<CategoryDto>>(_categories.OrderBy(item => item.DisplayOrder).ToArray());
        }

        public Task<CategoryDto> CreateAsync(SaveCategoryRequest request, CancellationToken cancellationToken = default)
        {
            CreateRequests.Add(request);
            var created = new CategoryDto
            {
                Id = 9,
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Description,
                Icon = request.Icon,
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive
            };
            _categories.Add(created);
            return Task.FromResult(created);
        }

        public Task<CategoryDto> UpdateAsync(int id, SaveCategoryRequest request, CancellationToken cancellationToken = default)
        {
            UpdateRequests.Add((id, request));
            var updated = new CategoryDto
            {
                Id = id,
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Description,
                Icon = request.Icon,
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive
            };
            _categories[_categories.FindIndex(item => item.Id == id)] = updated;
            return Task.FromResult(updated);
        }

        public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            DeleteRequests.Add(id);
            _categories.RemoveAll(item => item.Id == id);
            return Task.CompletedTask;
        }
    }
}
