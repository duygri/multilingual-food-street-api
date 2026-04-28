using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Languages;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Pages.Admin;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Admin;

public sealed class LanguageManagementTests : TestContext
{
    [Fact]
    public void Language_management_behavior_is_split_into_focused_partials()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var pageRoot = Path.Combine(projectRoot, "src", "NarrationApp.Web", "Pages", "Admin");
        var markupPath = Path.Combine(pageRoot, "LanguageManagement.razor");
        var expectedPartials = new[]
        {
            ("LanguageManagement.razor.cs", "OnInitializedAsync"),
            ("LanguageManagement.Actions.razor.cs", "SaveAsync"),
            ("LanguageManagement.Presentation.razor.cs", "FormatCoverage")
        };

        var markup = File.ReadAllText(markupPath);
        Assert.DoesNotContain("@code", markup, StringComparison.Ordinal);

        foreach (var (fileName, marker) in expectedPartials)
        {
            var path = Path.Combine(pageRoot, fileName);
            Assert.True(File.Exists(path), $"{fileName} should exist.");
            var source = File.ReadAllText(path);
            Assert.Contains("partial class LanguageManagement", source, StringComparison.Ordinal);
            Assert.Contains(marker, source, StringComparison.Ordinal);
        }

        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "LanguageManagement.razor.cs")).Length <= 80);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "LanguageManagement.Actions.razor.cs")).Length <= 70);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "LanguageManagement.Presentation.razor.cs")).Length <= 70);
    }

    [Fact]
    public void Language_management_lists_languages_and_adds_new_one()
    {
        var languageService = new TestLanguagePortalService();
        Services.AddSingleton<ILanguagePortalService>(languageService);

        var cut = RenderComponent<LanguageManagement>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Quản lý ngôn ngữ", cut.Markup);
            Assert.Contains("Tiếng Việt", cut.Markup);
            Assert.Contains("Nguồn chuẩn", cut.Markup);
            Assert.Contains("TTS + Dịch", cut.Markup);
            Assert.Contains("Thêm ngôn ngữ mới", cut.Markup);
            Assert.Contains("Ngôn ngữ active", cut.Markup);
            Assert.Contains("Tổng audio", cut.Markup);
            Assert.Contains("Coverage trung bình", cut.Markup);
            Assert.Equal(4, cut.FindAll(".language-admin__summary-card").Count);
            Assert.True(cut.FindAll(".language-admin__coverage-bar").Count >= 2);
        });

        cut.Find("button[data-action='open-language-form']").Click();
        cut.Find("input[data-field='language-code']").Change("fr");
        cut.Find("input[data-field='language-display-name']").Change("French");
        cut.Find("input[data-field='language-native-name']").Change("Francais");
        cut.Find("input[data-field='language-flag-code']").Change("FR");
        cut.Find("button[data-action='save-language']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("French", cut.Markup);
            Assert.Contains("Đã thêm ngôn ngữ fr.", cut.Markup);
        });

        Assert.Single(languageService.CreateRequests);
        Assert.Equal("fr", languageService.CreateRequests[0].Code);
    }

    private sealed class TestLanguagePortalService : ILanguagePortalService
    {
        private readonly List<ManagedLanguageDto> _items =
        [
            new ManagedLanguageDto
            {
                Code = "vi",
                DisplayName = "Tiếng Việt",
                NativeName = "Tiếng Việt",
                FlagCode = "VN",
                Role = ManagedLanguageRole.Source,
                IsActive = true,
                TranslationCoverageCount = 42,
                TranslationCoverageTotal = 42,
                AudioCount = 42
            },
            new ManagedLanguageDto
            {
                Code = "en",
                DisplayName = "English",
                NativeName = "English",
                FlagCode = "GB",
                Role = ManagedLanguageRole.TranslationAudio,
                IsActive = true,
                TranslationCoverageCount = 38,
                TranslationCoverageTotal = 42,
                AudioCount = 38
            }
        ];

        public List<CreateManagedLanguageRequest> CreateRequests { get; } = [];

        public Task<IReadOnlyList<ManagedLanguageDto>> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ManagedLanguageDto>>(_items.ToArray());
        }

        public Task<ManagedLanguageDto> CreateAsync(CreateManagedLanguageRequest request, CancellationToken cancellationToken = default)
        {
            CreateRequests.Add(request);

            var created = new ManagedLanguageDto
            {
                Code = request.Code,
                DisplayName = request.DisplayName,
                NativeName = request.NativeName,
                FlagCode = request.FlagCode,
                Role = ManagedLanguageRole.TranslationAudio,
                IsActive = true,
                TranslationCoverageCount = 0,
                TranslationCoverageTotal = 42,
                AudioCount = 0
            };

            _items.Add(created);
            return Task.FromResult(created);
        }

        public Task DeleteAsync(string code, CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(item => item.Code == code);
            return Task.CompletedTask;
        }
    }
}
