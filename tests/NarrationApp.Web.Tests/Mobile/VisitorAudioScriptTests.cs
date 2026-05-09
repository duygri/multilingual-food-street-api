using System.IO;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorAudioScriptTests
{
    [Fact]
    public void Visitor_audio_script_reports_precise_duration_and_elapsed_seconds_separately()
    {
        var script = ReadVisitorAudioScript();

        Assert.Contains("function toElapsedSeconds", script, StringComparison.Ordinal);
        Assert.Contains("function toDurationSeconds", script, StringComparison.Ordinal);
        Assert.Contains("Math.floor(value)", script, StringComparison.Ordinal);
        Assert.Contains("Math.ceil(value)", script, StringComparison.Ordinal);
        Assert.Contains("audio.addEventListener(\"durationchange\"", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Visitor_audio_script_restarts_completed_same_source_before_playing_again()
    {
        var script = ReadVisitorAudioScript();

        Assert.Contains("const shouldRestartSameSource", script, StringComparison.Ordinal);
        Assert.Contains("audio.currentTime = 0;", script, StringComparison.Ordinal);
        Assert.Contains("notifyProgress(0, audio.duration);", script, StringComparison.Ordinal);
    }

    private static string ReadVisitorAudioScript()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "wwwroot", "js", "visitorAudio.js"));
    }
}
