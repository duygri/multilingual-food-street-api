using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Web.Configuration;
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
    public void Qr_management_behavior_is_split_into_focused_partials()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var pageRoot = Path.Combine(projectRoot, "src", "NarrationApp.Web", "Pages", "Admin");
        var markupPath = Path.Combine(pageRoot, "QrManagement.razor");
        var expectedPartials = new[]
        {
            ("QrManagement.razor.cs", "OnInitializedAsync"),
            ("QrManagement.Composer.razor.cs", "ShowComposer"),
            ("QrManagement.Actions.razor.cs", "CreateQrAsync"),
            ("QrManagement.Presentation.razor.cs", "GetQrTypeLabel")
        };

        var markup = File.ReadAllText(markupPath);
        Assert.DoesNotContain("@code", markup, StringComparison.Ordinal);

        foreach (var (fileName, marker) in expectedPartials)
        {
            var path = Path.Combine(pageRoot, fileName);
            Assert.True(File.Exists(path), $"{fileName} should exist.");
            var source = File.ReadAllText(path);
            Assert.Contains("partial class QrManagement", source, StringComparison.Ordinal);
            Assert.Contains(marker, source, StringComparison.Ordinal);
        }

        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "QrManagement.razor.cs")).Length <= 80);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "QrManagement.Composer.razor.cs")).Length <= 90);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "QrManagement.Actions.razor.cs")).Length <= 90);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "QrManagement.Presentation.razor.cs")).Length <= 90);
    }

    [Fact]
    public void Create_filter_and_delete_qr_code_updates_sample_strict_qr_workspace()
    {
        Services.AddSingleton<ITourPortalService>(new TestTourPortalService());
        Services.AddSingleton<IQrPortalService>(new TestQrPortalService());
        Services.AddSingleton(new QrPublicUrlOptions { BaseAddress = new Uri("https://narration.app/") });

        var cut = RenderComponent<QrManagement>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find(".qr-surface"));
            Assert.Contains("QR Codes", cut.Markup);
            Assert.Contains("Quản lý QR Code", cut.Markup);
            Assert.Contains("QR-POI-001", cut.Markup);
            Assert.Contains(">12<", cut.Markup);
            Assert.Contains("Chưa track", cut.Markup);
            Assert.Contains("Xem", cut.Markup);
            Assert.Contains("Link đến POI", cut.Markup);
            Assert.Contains("Mở App", cut.Markup);
            Assert.DoesNotContain("Link đến Tour", cut.Markup);
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
            Assert.DoesNotContain("option value=\"tour\"", cut.Markup, StringComparison.Ordinal);
            Assert.Empty(cut.FindAll("select[data-field='qr-tour-id']"));
        });

        cut.Find("select[data-field='qr-target-type']").Change("open_app");
        cut.Find("input[data-field='qr-location-hint']").Change("Cổng tour đêm");
        cut.Find("button[data-action='create-qr']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Đã tạo QR mới", cut.Markup);
            Assert.Contains("QR-APP-003", cut.Markup);
            Assert.Empty(cut.FindAll("select[data-field='qr-target-type']"));
        });

        cut.Find("select[data-field='qr-filter']").Change("open_app");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("QR-APP-003", cut.Markup);
            Assert.DoesNotContain("QR-POI-001", cut.Markup);
            Assert.Contains("QR-APP-001", cut.Markup);
        });

        cut.Find("button[data-action='view-qr-3']").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Xem mã QR", cut.Markup);
            Assert.Contains("QR-APP-003", cut.Markup);
            var qrImage = cut.Find("img[data-role='qr-image']");
            var qrImageSource = qrImage.GetAttribute("src");
            Assert.False(string.IsNullOrWhiteSpace(qrImageSource));
            Assert.StartsWith("data:image/png;base64,", qrImageSource, StringComparison.Ordinal);
            Assert.Contains("https://narration.app/qr/QR-APP-003", cut.Markup);
        });

        cut.Find("button[data-action='delete-qr-3']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("QR-APP-003", cut.Markup);
        });
    }

    [Fact]
    public void Preview_warning_uses_the_actual_qr_url_instead_of_loopback_fallback_config()
    {
        Services.AddSingleton<ITourPortalService>(new TestTourPortalService());
        Services.AddSingleton<IQrPortalService>(new TestQrPortalService());
        Services.AddSingleton(new QrPublicUrlOptions { BaseAddress = new Uri("https://localhost:5001/") });

        var cut = RenderComponent<QrManagement>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("QR-APP-002", cut.Markup);
        });

        cut.Find("button[data-action='view-qr-4']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("http://192.168.98.219:5000/qr/QR-APP-002", cut.Markup);
            Assert.DoesNotContain("điện thoại khác sẽ không mở được", cut.Markup, StringComparison.OrdinalIgnoreCase);
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
            return Task.FromResult<IReadOnlyList<TourDto>>([]);
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
                LocationHint = "Khánh Hội",
                ScanCount = 12
            },
            new QrCodeDto
            {
                Id = 2,
                Code = "QR-APP-001",
                TargetType = "open_app",
                TargetId = 0,
                LocationHint = "Bản đồ tổng",
                ScanCount = null
            },
            new QrCodeDto
            {
                Id = 4,
                Code = "QR-APP-002",
                TargetType = "open_app",
                TargetId = 0,
                LocationHint = "Mở app qua LAN",
                PublicUrl = "http://192.168.98.219:5000/qr/QR-APP-002",
                ScanCount = null
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
                Code = request.TargetType == "open_app" ? "QR-APP-003" : "QR-POI-003",
                TargetType = request.TargetType,
                TargetId = request.TargetId,
                LocationHint = request.LocationHint,
                ExpiresAtUtc = request.ExpiresAtUtc,
                ScanCount = request.TargetType == "poi" ? 0 : null
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
