using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.DTOs.QR;
using NarrationApp.Shared.DTOs.Tour;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Pages.Admin;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Admin;

public sealed class QrManagementTests : TestContext
{
    [Fact]
    public void Create_filter_and_delete_qr_code_updates_sample_strict_qr_workspace()
    {
        Services.AddSingleton<ITourPortalService>(new TestTourPortalService());
        Services.AddSingleton<IQrPortalService>(new TestQrPortalService());

        var cut = RenderComponent<QrManagement>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find(".qr-surface"));
            Assert.Contains("QR Codes", cut.Markup);
            Assert.Contains("Quản lý QR Code", cut.Markup);
            Assert.Contains("QR-POI-001", cut.Markup);
            Assert.Contains("Xem", cut.Markup);
            Assert.DoesNotContain("Copy Link", cut.Markup);
            Assert.DoesNotContain("Đang chuyển sang workspace Tour", cut.Markup);
            Assert.Empty(cut.FindAll("select[data-field='qr-target-type']"));
            Assert.Empty(cut.FindAll("[data-panel='qr-composer']"));
        });

        cut.Find("button[data-action='new-qr']").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-panel='qr-composer']"));
            Assert.Single(cut.FindAll("select[data-field='qr-target-type']"));
        });
        cut.Find("select[data-field='qr-target-type']").Change("tour");
        cut.Find("select[data-field='qr-tour-id']").Change("7");
        cut.Find("input[data-field='qr-location-hint']").Change("Cổng tour đêm");
        cut.Find("button[data-action='create-qr']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Đã tạo QR mới", cut.Markup);
            Assert.Contains("QR-TOUR-003", cut.Markup);
            Assert.Empty(cut.FindAll("select[data-field='qr-target-type']"));
        });

        cut.Find("select[data-field='qr-filter']").Change("tour");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("QR-TOUR-003", cut.Markup);
            Assert.DoesNotContain("QR-POI-001", cut.Markup);
            Assert.DoesNotContain("QR-APP-001", cut.Markup);
        });

        cut.Find("button[data-action='view-qr-3']").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Xem mã QR", cut.Markup);
            Assert.Contains("QR-TOUR-003", cut.Markup);
        });

        cut.Find("button[data-action='delete-qr-3']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("QR-TOUR-003", cut.Markup);
            Assert.Contains("Chưa có QR nào trong bộ lọc này.", cut.Markup);
        });
    }

    private sealed class TestTourPortalService : ITourPortalService
    {
        public Task<IReadOnlyList<PoiDto>> GetPoiOptionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PoiDto>>(
            [
                new PoiDto { Id = 1, Name = "Chùa Bà Thiên Hậu", Slug = "chua-ba-thien-hau", Status = PoiStatus.Published },
                new PoiDto { Id = 2, Name = "Nhà cổ Xóm Chiếu", Slug = "nha-co-xom-chieu", Status = PoiStatus.Published }
            ]);
        }

        public Task<IReadOnlyList<TourDto>> GetToursAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TourDto>>(
            [
                new TourDto
                {
                    Id = 7,
                    Title = "Tour đêm Vĩnh Khánh",
                    Description = "Tuyến đêm nổi bật",
                    EstimatedMinutes = 35,
                    Status = TourStatus.Published
                }
            ]);
        }

        public Task<TourDto> CreateTourAsync(CreateTourRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<TourDto> UpdateTourAsync(int id, UpdateTourRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteTourAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestQrPortalService : IQrPortalService
    {
        private readonly List<QrCodeDto> _items =
        [
            new QrCodeDto
            {
                Id = 1,
                Code = "QR-POI-001",
                TargetType = "poi",
                TargetId = 1,
                LocationHint = "Khánh Hội"
            },
            new QrCodeDto
            {
                Id = 2,
                Code = "QR-APP-001",
                TargetType = "open_app",
                TargetId = 0,
                LocationHint = "Bản đồ tổng"
            }
        ];

        public Task<IReadOnlyList<QrCodeDto>> GetAsync(string? targetType = null, CancellationToken cancellationToken = default)
        {
            var filtered = _items
                .Where(item => string.IsNullOrWhiteSpace(targetType) || string.Equals(item.TargetType, targetType, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.Id)
                .ToArray();

            return Task.FromResult<IReadOnlyList<QrCodeDto>>(filtered);
        }

        public Task<QrCodeDto> CreateAsync(CreateQrRequest request, CancellationToken cancellationToken = default)
        {
            var created = new QrCodeDto
            {
                Id = 3,
                Code = "QR-TOUR-003",
                TargetType = request.TargetType,
                TargetId = request.TargetId,
                LocationHint = request.LocationHint,
                ExpiresAtUtc = request.ExpiresAtUtc
            };

            _items.Add(created);
            return Task.FromResult(created);
        }

        public Task DeleteAsync(int qrId, CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(item => item.Id == qrId);
            return Task.CompletedTask;
        }
    }
}
