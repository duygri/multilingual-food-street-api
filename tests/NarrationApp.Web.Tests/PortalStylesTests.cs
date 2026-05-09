using System.IO;

namespace NarrationApp.Web.Tests;

public sealed class PortalStylesTests
{
    [Fact]
    public void Portal_styles_theme_native_form_controls_dark_by_default()
    {
        var css = ReadPortalCss().ReplaceLineEndings("\n");

        Assert.Contains(".portal-shell :where(input:not([type=\"checkbox\"]):not([type=\"radio\"]):not([type=\"range\"])", css, StringComparison.Ordinal);
        Assert.Contains("background: linear-gradient(180deg, rgba(12, 24, 43, 0.96), rgba(8, 17, 31, 0.96));", css, StringComparison.Ordinal);
        Assert.Contains("border-radius: 1rem;", css, StringComparison.Ordinal);
        Assert.Contains("color: var(--vk-text);", css, StringComparison.Ordinal);
        Assert.Contains("::file-selector-button", css, StringComparison.Ordinal);
        Assert.Contains("background: linear-gradient(135deg, rgba(18, 214, 175, 0.92), rgba(19, 166, 131, 0.92));", css, StringComparison.Ordinal);
        Assert.DoesNotContain("input[type=\"file\"] {\n    color: var(--vk-text);\n}", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Portal_buttons_use_stable_no_overflow_scaling()
    {
        var css = ReadPortalCss().ReplaceLineEndings("\n");
        var buttonRule = ExtractRule(css, ".app-button");
        var hoverRule = ExtractRule(css, ".app-button:hover");

        Assert.Contains("box-sizing: border-box;", buttonRule, StringComparison.Ordinal);
        Assert.Contains("max-width: 100%;", buttonRule, StringComparison.Ordinal);
        Assert.Contains("line-height: 1.1;", buttonRule, StringComparison.Ordinal);
        Assert.Contains("white-space: nowrap;", buttonRule, StringComparison.Ordinal);
        Assert.Contains(".app-button:focus-visible", css, StringComparison.Ordinal);
        Assert.Contains("transform: translateY(-1px);", hoverRule, StringComparison.Ordinal);
        Assert.DoesNotContain("scale(1.03)", css, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Admin", "Analytics.razor.css", ".analytics-filter-chip")]
    [InlineData("Admin", "AudioManagement.razor.css", ".audio-language-chip")]
    [InlineData("Admin", "AudioManagement.razor.css", ".audio-row-button")]
    [InlineData("Admin", "CategoryManagement.razor.css", ".category-action")]
    [InlineData("Admin", "PoiManagement.razor.css", ".admin-poi-filter")]
    [InlineData("Admin", "PoiManagement.razor.css", ".admin-poi-action")]
    [InlineData("Admin", "PoiManagement.razor.css", ".admin-poi-pagination__button")]
    [InlineData("Admin", "TranslationReview.razor.css", ".translation-review__bulk-action")]
    public void Portal_button_like_page_controls_keep_consistent_box_metrics(
        string area,
        string fileName,
        string selector)
    {
        var css = ReadPageCss(area, fileName).ReplaceLineEndings("\n");
        var rule = ExtractRule(css, selector);

        Assert.Contains("display: inline-flex;", rule, StringComparison.Ordinal);
        Assert.Contains("align-items: center;", rule, StringComparison.Ordinal);
        Assert.Contains("justify-content: center;", rule, StringComparison.Ordinal);
        Assert.Contains("max-width: 100%;", rule, StringComparison.Ordinal);
        Assert.Contains("line-height: 1.1;", rule, StringComparison.Ordinal);
        Assert.Contains("white-space: nowrap;", rule, StringComparison.Ordinal);
    }

    private static string ReadPortalCss()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine(projectRoot, "src", "NarrationApp.Web", "wwwroot", "css", "app.css"));
    }

    private static string ReadPageCss(string area, string fileName)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine(projectRoot, "src", "NarrationApp.Web", "Pages", area, fileName));
    }

    private static string ExtractRule(string css, string selector)
    {
        var start = css.IndexOf(selector + " {", StringComparison.Ordinal);
        Assert.True(start >= 0, $"Could not find CSS rule for '{selector}'.");

        var end = css.IndexOf("\n}", start, StringComparison.Ordinal);
        Assert.True(end > start, $"Could not read CSS rule for '{selector}'.");

        return css[start..(end + 2)];
    }
}
