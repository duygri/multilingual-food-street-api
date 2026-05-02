using NarrationApp.Web.Features.Languages;

namespace NarrationApp.Web.Tests.Features.Languages;

public sealed class LanguageCatalogTests
{
    [Theory]
    [InlineData("fr", "fr")]
    [InlineData("jp", "ja")]
    [InlineData("kr", "ko")]
    public void Search_supports_common_shortcuts_for_language_codes_and_flag_codes(string query, string expectedCode)
    {
        var result = LanguageCatalog.Search(query);

        Assert.NotEmpty(result);
        Assert.Equal(expectedCode, result[0].Code);
    }
}
