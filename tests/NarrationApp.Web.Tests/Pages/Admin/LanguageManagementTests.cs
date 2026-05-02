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

    [Fact]
    public void Language_management_autofills_language_metadata_from_search_selection()
    {
        var languageService = new TestLanguagePortalService();
        Services.AddSingleton<ILanguagePortalService>(languageService);

        var cut = RenderComponent<LanguageManagement>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Thêm ngôn ngữ mới", cut.Markup);
        });

        cut.Find("button[data-action='open-language-form']").Click();
        cut.Find("input[data-field='language-search']").Input("pháp");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("French", cut.Markup);
            Assert.Contains("Français", cut.Markup);
        });

        cut.Find("button[data-action='language-suggestion-fr']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("fr", cut.Find("input[data-field='language-code']").GetAttribute("value"));
            Assert.Equal("French", cut.Find("input[data-field='language-display-name']").GetAttribute("value"));
            Assert.Equal("Français", cut.Find("input[data-field='language-native-name']").GetAttribute("value"));
            Assert.Equal("FR", cut.Find("input[data-field='language-flag-code']").GetAttribute("value"));
        });

        cut.Find("button[data-action='save-language']").Click();

        Assert.Single(languageService.CreateRequests);
        Assert.Equal("fr", languageService.CreateRequests[0].Code);
        Assert.Equal("French", languageService.CreateRequests[0].DisplayName);
        Assert.Equal("Français", languageService.CreateRequests[0].NativeName);
        Assert.Equal("FR", languageService.CreateRequests[0].FlagCode);
    }

    [Fact]
    public void Language_management_autofills_language_metadata_when_code_is_entered()
    {
        var languageService = new TestLanguagePortalService();
        Services.AddSingleton<ILanguagePortalService>(languageService);

        var cut = RenderComponent<LanguageManagement>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Thêm ngôn ngữ mới", cut.Markup);
        });

        cut.Find("button[data-action='open-language-form']").Click();
        cut.Find("input[data-field='language-code']").Input("JP");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("ja", cut.Find("input[data-field='language-code']").GetAttribute("value"));
            Assert.Equal("Japanese", cut.Find("input[data-field='language-display-name']").GetAttribute("value"));
            Assert.Equal("日本語", cut.Find("input[data-field='language-native-name']").GetAttribute("value"));
            Assert.Equal("JP", cut.Find("input[data-field='language-flag-code']").GetAttribute("value"));
        });
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
