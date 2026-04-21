namespace NarrationApp.Web.Tests.Mobile;

public sealed class AndroidWebViewAutoplayTests
{
    [Fact]
    public void MainPage_DisablesUserGestureRequirementForMediaPlayback()
    {
        var filePath = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "NarrationApp.Mobile",
            "MainPage.xaml.cs");

        var source = File.ReadAllText(Path.GetFullPath(filePath));

        Assert.Contains("MediaPlaybackRequiresUserGesture = false", source, StringComparison.Ordinal);
    }
}
