using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorAndroidRuntimeDetectorTests
{
    [Fact]
    public void LooksLikeEmulator_returns_true_for_typical_android_emulator_build()
    {
        var result = VisitorAndroidRuntimeDetector.LooksLikeEmulator(
            fingerprint: "generic/sdk_gphone64_arm64/generic:14/UE1A.230829.036/1234567:userdebug/dev-keys",
            model: "sdk_gphone64_arm64",
            manufacturer: "Google",
            brand: "generic",
            device: "generic_x86_64",
            product: "sdk_gphone64_arm64",
            hardware: "ranchu");

        Assert.True(result);
    }

    [Fact]
    public void LooksLikeEmulator_returns_false_for_typical_physical_android_build()
    {
        var result = VisitorAndroidRuntimeDetector.LooksLikeEmulator(
            fingerprint: "samsung/a54xx/a54x:14/UP1A.231005.007/A546EXXU9CXB2:user/release-keys",
            model: "SM-A546E",
            manufacturer: "samsung",
            brand: "samsung",
            device: "a54x",
            product: "a54xnnxx",
            hardware: "exynos1380");

        Assert.False(result);
    }
}
