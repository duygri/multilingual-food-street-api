namespace NarrationApp.Web.Tests.Components;

public sealed class RedirectToLoginSourceTests
{
    [Fact]
    public void Redirect_component_uses_code_behind()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var componentRoot = Path.Combine(projectRoot, "src", "NarrationApp.Web", "Components");
        var markupPath = Path.Combine(componentRoot, "RedirectToLogin.razor");
        var codeBehindPath = Path.Combine(componentRoot, "RedirectToLogin.razor.cs");

        Assert.True(File.Exists(markupPath));
        Assert.True(File.Exists(codeBehindPath));
        Assert.DoesNotContain("@code", File.ReadAllText(markupPath), StringComparison.Ordinal);
        Assert.Contains("partial class RedirectToLogin", File.ReadAllText(codeBehindPath), StringComparison.Ordinal);
    }
}
