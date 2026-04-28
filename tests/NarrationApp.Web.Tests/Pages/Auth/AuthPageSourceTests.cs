namespace NarrationApp.Web.Tests.Pages.Auth;

public sealed class AuthPageSourceTests
{
    [Theory]
    [InlineData("Login.razor", "Login.razor.cs", "partial class Login")]
    [InlineData("Register.razor", "Register.razor.cs", "partial class Register")]
    public void Auth_pages_use_code_behind(string markupFile, string codeBehindFile, string marker)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var pageRoot = Path.Combine(projectRoot, "src", "NarrationApp.Web", "Pages", "Auth");
        var markupPath = Path.Combine(pageRoot, markupFile);
        var codeBehindPath = Path.Combine(pageRoot, codeBehindFile);

        Assert.True(File.Exists(markupPath));
        Assert.True(File.Exists(codeBehindPath));
        Assert.DoesNotContain("@code", File.ReadAllText(markupPath), StringComparison.Ordinal);
        Assert.Contains(marker, File.ReadAllText(codeBehindPath), StringComparison.Ordinal);
    }
}
