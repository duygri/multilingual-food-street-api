using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    Exported = true,
    LaunchMode = LaunchMode.SingleTask,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
    DataSchemes = ["https"],
    DataHosts = ["narration.app", "www.narration.app", "staging.narration.app"],
    DataPathPrefixes = ["/qr/"],
    AutoVerify = true)]
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
    DataSchemes = ["foodstreet"],
    DataHosts = ["qr"])]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        VisitorMobileDiagnostics.Log("MainActivity", $"OnCreate action={Intent?.Action ?? "<null>"} data={Intent?.DataString ?? "<null>"}");
        CapturePendingDeepLink(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        VisitorMobileDiagnostics.Log("MainActivity", $"OnNewIntent action={intent?.Action ?? "<null>"} data={intent?.DataString ?? "<null>"}");
        CapturePendingDeepLink(intent);
    }

    private static void CapturePendingDeepLink(Intent? intent)
    {
        VisitorMobileDiagnostics.Log("MainActivity", $"CapturePendingDeepLink data={intent?.DataString ?? "<null>"}");
        VisitorPendingDeepLinkStore.SetPendingUri(intent?.DataString);
    }
}
