using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorDeviceIdFormatterTests
{
    [Fact]
    public void Build_includes_platform_kind_model_and_suffix_for_emulator()
    {
        var result = VisitorDeviceIdFormatter.Build(
            platform: "Android",
            manufacturer: "Google",
            model: "sdk_gphone64_arm64",
            isEmulator: true,
            suffix: "a1b2c3");

        Assert.Equal("android-emulator-google-sdk-gphone64-arm64-a1b2c3", result);
    }

    [Fact]
    public void LooksLikeLegacyOpaqueDeviceId_detects_old_android_guid_format()
    {
        Assert.True(VisitorDeviceIdFormatter.LooksLikeLegacyOpaqueDeviceId("android-1234567890abcdef1234567890abcdef"));
        Assert.False(VisitorDeviceIdFormatter.LooksLikeLegacyOpaqueDeviceId("android-emulator-pixel-a1b2c3"));
    }

    [Fact]
    public void ExtractLegacySuffix_returns_last_six_characters()
    {
        var result = VisitorDeviceIdFormatter.ExtractLegacySuffix("android-1234567890abcdef1234567890abcdef");

        Assert.Equal("abcdef", result);
    }
}
