using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorCategoryPresentationFormatterTests
{
    [Theory]
    [InlineData("di-tich", "Di tích", "🏛️", "history")]
    [InlineData("hai-san", "Hải sản", "custom-pin", "discover")]
    [InlineData("unknown", "Khác", "", "location")]
    public void GetCategoryIcon_normalizes_unsupported_configured_values_to_supported_keys(
        string categoryId,
        string label,
        string markerLabel,
        string expected)
    {
        var categories = new[] { new VisitorCategory(categoryId, label, markerLabel) };

        var icon = VisitorCategoryPresentationFormatter.GetCategoryIcon(categoryId, categories);

        Assert.Equal(expected, icon);
    }

    [Fact]
    public void GetCategoryIcon_uses_a_stable_supported_key_when_category_is_missing()
    {
        var icon = VisitorCategoryPresentationFormatter.GetCategoryIcon("unconfigured", []);

        Assert.Equal("location", icon);
    }

    [Fact]
    public void CreateCategory_stores_a_normalized_supported_key_in_marker_label()
    {
        var category = VisitorCategoryPresentationFormatter.CreateCategory("river", "Ven sông", "🌉");

        Assert.Equal("map", category.MarkerLabel);
    }
}
