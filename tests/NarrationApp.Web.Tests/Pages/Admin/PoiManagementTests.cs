using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Pages.Admin;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Admin;

public sealed class PoiManagementTests : TestContext
{
    [Fact]
    public void Filters_and_search_update_admin_poi_table_surface()
    {
        var adminService = new TestAdminPortalService();
        var poiOperationsService = new TestAdminPoiOperationsService(adminService);

        Services.AddSingleton<IAdminPortalService>(adminService);
        Services.AddSingleton<IAdminPoiOperationsService>(poiOperationsService);

        var cut = RenderComponent<PoiManagement>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Quản lý POI", cut.Markup);
            Assert.Contains("1 POI đang chờ duyệt", cut.Markup);
            Assert.Contains("Tất cả (3)", cut.Markup);
            Assert.Contains("Đã xuất bản (1)", cut.Markup);
            Assert.Contains("Chờ duyệt (1)", cut.Markup);
            Assert.Contains("Lưu trữ (1)", cut.Markup);
            Assert.Contains("Hiển thị 1-3 / 3 POI", cut.Markup);
        });

        cut.Find("input[data-field='poi-search']").Change("ốc");

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.Contains("Ốc đêm Vĩnh Hội", cut.Markup);
            Assert.DoesNotContain("Hủ tiếu Nam Vang", cut.Markup);
        });

        cut.Find("input[data-field='poi-search']").Change(string.Empty);
        cut.Find("button[data-action='filter-pending']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.DoesNotContain("Ốc đêm Vĩnh Hội", cut.Markup);
            Assert.DoesNotContain("Hủ tiếu Nam Vang", cut.Markup);
        });
    }

    [Fact]
    public void Approve_and_delete_poi_updates_admin_poi_surface()
    {
        var adminService = new TestAdminPortalService();
        var poiOperationsService = new TestAdminPoiOperationsService(adminService);

        Services.AddSingleton<IAdminPortalService>(adminService);
        Services.AddSingleton<IAdminPoiOperationsService>(poiOperationsService);

        var cut = RenderComponent<PoiManagement>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Quản lý POI", cut.Markup);
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.Contains("owner-1@narration.app", cut.Markup);
            Assert.DoesNotContain("POI detail", cut.Markup);
            Assert.Contains("data-action=\"view-poi-11\"", cut.Markup);
            Assert.Empty(cut.FindAll("[data-panel='poi-detail']"));
        });

        cut.Find("button[data-action='view-poi-11']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-panel='poi-detail']"));
            Assert.Contains("POI detail", cut.Markup);
            Assert.Contains("Quầy bún mắm đông khách về đêm.", cut.Markup);
            Assert.Contains("Bún mắm Vĩnh Khánh là điểm dừng nổi bật của tuyến ẩm thực đêm.", cut.Markup);
        });

        cut.Find("button[data-action='approve-poi-11']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Đã duyệt POI", cut.Markup);
            Assert.Contains("Đã xuất bản", cut.Markup);
            Assert.DoesNotContain("approve-poi-11", cut.Markup);
            Assert.Contains("data-action=\"view-poi-11\"", cut.Markup);
        });

        cut.Find("button[data-action='delete-poi-12']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("data-action=\"view-poi-12\"", cut.Markup);
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
        });

        Assert.Equal(new[] { 101 }, adminService.ApprovedRequestIds);
        Assert.Equal(new[] { 12 }, poiOperationsService.DeletedPoiIds);
    }

    private sealed class TestAdminPortalService : IAdminPortalService
    {
        private readonly List<AdminPoiDto> _pois =
        [
            new AdminPoiDto
            {
                Id = 11,
                Name = "Bún mắm Vĩnh Khánh",
                Slug = "bun-mam-vinh-khanh",
                OwnerName = "Owner Một",
                OwnerEmail = "owner-1@narration.app",
                CategoryId = 1,
                CategoryName = "Hải sản",
                Description = "Quầy bún mắm đông khách về đêm.",
                TtsScript = "Bún mắm Vĩnh Khánh là điểm dừng nổi bật của tuyến ẩm thực đêm.",
                Priority = 10,
                Lat = 10.758,
                Lng = 106.701,
                Status = PoiStatus.PendingReview,
                AudioAssetCount = 1,
                TranslationCount = 2,
                GeofenceCount = 1,
                PendingModerationId = 101,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
            },
            new AdminPoiDto
            {
                Id = 12,
                Name = "Ốc đêm Vĩnh Hội",
                Slug = "oc-dem-vinh-hoi",
                OwnerName = "Owner Hai",
                OwnerEmail = "owner-2@narration.app",
                CategoryId = 4,
                CategoryName = "Ăn vặt",
                Description = "Điểm dừng ăn vặt chuyên món ốc đêm.",
                TtsScript = "Ốc đêm Vĩnh Hội nổi bật với nhịp phục vụ sau giờ tan ca.",
                Priority = 14,
                Lat = 10.759,
                Lng = 106.703,
                Status = PoiStatus.Published,
                AudioAssetCount = 3,
                TranslationCount = 4,
                GeofenceCount = 1,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
            },
            new AdminPoiDto
            {
                Id = 13,
                Name = "Hủ tiếu Nam Vang",
                Slug = "hu-tieu-nam-vang",
                OwnerName = "Owner Ba",
                OwnerEmail = "owner-3@narration.app",
                CategoryId = 2,
                CategoryName = "Mì nước",
                Description = "Quầy hủ tiếu lưu trữ để rà soát lại sau.",
                TtsScript = "Hủ tiếu Nam Vang đang ở trạng thái lưu trữ.",
                Priority = 8,
                Lat = 10.754,
                Lng = 106.702,
                Status = PoiStatus.Archived,
                AudioAssetCount = 0,
                TranslationCount = 1,
                GeofenceCount = 0,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-4)
            }
        ];

        public List<int> ApprovedRequestIds { get; } = [];

        public Task<DashboardDto> GetOverviewAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<AdminPoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AdminPoiDto>>(_pois.ToArray());
        }

        public Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<ModerationRequestDto>> GetPendingModerationAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ModerationRequestDto> ApproveModerationAsync(int requestId, ReviewModerationRequest request, CancellationToken cancellationToken = default)
        {
            ApprovedRequestIds.Add(requestId);
            var index = _pois.FindIndex(item => item.PendingModerationId == requestId);
            var poi = _pois[index];
            _pois[index] = new AdminPoiDto
            {
                Id = poi.Id,
                Name = poi.Name,
                Slug = poi.Slug,
                OwnerName = poi.OwnerName,
                OwnerEmail = poi.OwnerEmail,
                CategoryId = poi.CategoryId,
                CategoryName = poi.CategoryName,
                Description = poi.Description,
                TtsScript = poi.TtsScript,
                Priority = poi.Priority,
                Lat = poi.Lat,
                Lng = poi.Lng,
                Status = PoiStatus.Published,
                AudioAssetCount = poi.AudioAssetCount,
                TranslationCount = poi.TranslationCount,
                GeofenceCount = poi.GeofenceCount,
                PendingModerationId = null,
                CreatedAtUtc = poi.CreatedAtUtc
            };

            return Task.FromResult(new ModerationRequestDto
            {
                Id = requestId,
                EntityType = "poi",
                EntityId = poi.Id.ToString(),
                RequestedBy = Guid.NewGuid(),
                ReviewedBy = Guid.NewGuid(),
                Status = ModerationStatus.Approved,
                ReviewNote = request.ReviewNote,
                CreatedAtUtc = DateTime.UtcNow.AddHours(-1)
            });
        }

        public Task<ModerationRequestDto> RejectModerationAsync(int requestId, ReviewModerationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
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

        public void DeletePoi(int poiId)
        {
            _pois.RemoveAll(item => item.Id == poiId);
        }
    }

    private sealed class TestAdminPoiOperationsService(TestAdminPortalService adminPortalService) : IAdminPoiOperationsService
    {
        public List<int> DeletedPoiIds { get; } = [];

        public Task DeleteAsync(int poiId, CancellationToken cancellationToken = default)
        {
            DeletedPoiIds.Add(poiId);
            adminPortalService.DeletePoi(poiId);
            return Task.CompletedTask;
        }
    }
}
