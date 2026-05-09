using Microsoft.JSInterop;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorAudioPlaybackSourceResolverTests
{
    [Fact]
    public async Task ResolveAsync_ConvertsLocalFileUrlToWebViewBlobUrl()
    {
        var audioPath = Path.Combine(Path.GetTempPath(), $"visitor-audio-{Guid.NewGuid():N}.mp3");
        await File.WriteAllBytesAsync(audioPath, [1, 2, 3, 4]);
        var sourceUrl = new Uri(audioPath).AbsoluteUri;
        var jsRuntime = new CapturingJsRuntime("blob:visitor-audio");

        try
        {
            var playbackUrl = await VisitorAudioPlaybackSourceResolver.ResolveAsync(sourceUrl, jsRuntime);

            Assert.Equal("blob:visitor-audio", playbackUrl);
            Assert.Equal("visitorAudio.registerLocalSource", jsRuntime.Identifier);
            Assert.NotNull(jsRuntime.Arguments);
            Assert.Equal(sourceUrl, jsRuntime.Arguments[0]);
            Assert.Equal("audio/mpeg", jsRuntime.Arguments[1]);
            Assert.Equal(Convert.ToBase64String([1, 2, 3, 4]), jsRuntime.Arguments[2]);
        }
        finally
        {
            File.Delete(audioPath);
        }
    }

    [Fact]
    public async Task ResolveAsync_KeepsHttpAudioUrlWithoutJsBridge()
    {
        var jsRuntime = new CapturingJsRuntime("blob:unused");

        var playbackUrl = await VisitorAudioPlaybackSourceResolver.ResolveAsync(
            "http://10.0.2.2:5000/api/audio/12/stream",
            jsRuntime);

        Assert.Equal("http://10.0.2.2:5000/api/audio/12/stream", playbackUrl);
        Assert.Null(jsRuntime.Identifier);
    }

    private sealed class CapturingJsRuntime(string returnValue) : IJSRuntime
    {
        public string? Identifier { get; private set; }

        public object?[]? Arguments { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Identifier = identifier;
            Arguments = args;
            return ValueTask.FromResult((TValue)(object)returnValue);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            Identifier = identifier;
            Arguments = args;
            return ValueTask.FromResult((TValue)(object)returnValue);
        }
    }
}
