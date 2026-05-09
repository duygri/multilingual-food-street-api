using Microsoft.JSInterop;

namespace NarrationApp.Mobile.Features.Home;

public static class VisitorAudioPlaybackSourceResolver
{
    public static async Task<string> ResolveAsync(
        string streamUrl,
        IJSRuntime jsRuntime,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveLocalFilePath(streamUrl, out var localFilePath))
        {
            return streamUrl;
        }

        var audioBytes = await File.ReadAllBytesAsync(localFilePath, cancellationToken);
        var contentType = ResolveContentType(localFilePath);
        var base64Audio = Convert.ToBase64String(audioBytes);
        var playbackUrl = await jsRuntime.InvokeAsync<string>(
            "visitorAudio.registerLocalSource",
            cancellationToken,
            [streamUrl, contentType, base64Audio]);

        return string.IsNullOrWhiteSpace(playbackUrl)
            ? throw new InvalidOperationException("WebView did not create a playable audio source.")
            : playbackUrl;
    }

    private static bool TryResolveLocalFilePath(string streamUrl, out string localFilePath)
    {
        localFilePath = string.Empty;
        if (!Uri.TryCreate(streamUrl, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        localFilePath = uri.LocalPath;
        return true;
    }

    private static string ResolveContentType(string localFilePath)
    {
        return Path.GetExtension(localFilePath).ToLowerInvariant() switch
        {
            ".aac" => "audio/aac",
            ".m4a" => "audio/mp4",
            ".ogg" => "audio/ogg",
            ".wav" => "audio/wav",
            _ => "audio/mpeg"
        };
    }
}
