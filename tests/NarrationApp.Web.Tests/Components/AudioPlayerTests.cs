using Bunit;
using NarrationApp.SharedUI.Components;

namespace NarrationApp.Web.Tests.Components;

public sealed class AudioPlayerTests : TestContext
{
    [Fact]
    public void Renders_audio_metadata_and_stream_source()
    {
        var cut = RenderComponent<AudioPlayer>(parameters => parameters
            .Add(component => component.Title, "Audio tiếng Việt")
            .Add(component => component.LanguageCode, "vi")
            .Add(component => component.Source, "https://localhost:5001/api/audio/12/stream")
            .Add(component => component.Caption, "Bản kể chuyện đã sẵn sàng"));

        Assert.Contains("Audio tiếng Việt", cut.Markup);
        Assert.Contains("Bản kể chuyện đã sẵn sàng", cut.Markup);
        Assert.Equal("https://localhost:5001/api/audio/12/stream", cut.Find("audio").GetAttribute("src"));
    }
}
