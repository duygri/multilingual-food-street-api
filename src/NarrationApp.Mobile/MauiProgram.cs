using Microsoft.Extensions.Logging;
using NarrationApp.Mobile.Features.Home;
using NarrationApp.Mobile.Services;
using Microsoft.Maui.Storage;
#if ANDROID
using Android.OS;
using Android.App;
using NarrationApp.Mobile.Platforms.Android.Services;
#endif

namespace NarrationApp.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        var visitorApiOptions = LoadVisitorApiOptions();
        var visitorApiEnvironment = GetVisitorApiEnvironment();
        var visitorApiBaseAddress = ResolveVisitorApiBaseAddress(visitorApiOptions, visitorApiEnvironment);

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton(new VisitorMapOptions());
        builder.Services.AddSingleton(visitorApiOptions);
        builder.Services.AddSingleton<IVisitorOfflineCacheStore>(_ => new VisitorOfflineCacheStore(
            Path.Combine(FileSystem.AppDataDirectory, "visitor-cache.db3"),
            Path.Combine(FileSystem.AppDataDirectory, "audio-cache")));
        builder.Services.AddSingleton<IVisitorDeviceIdentityProvider, DeviceVisitorIdentityProvider>();
        builder.Services.AddScoped<IVisitorContentService, VisitorContentService>();
        builder.Services.AddScoped<IVisitorAudioCatalogService, VisitorAudioCatalogService>();
        builder.Services.AddScoped<IVisitorAudioPreloadService, VisitorAudioPreloadService>();
        builder.Services.AddScoped<IVisitorAudioPlayReporter, VisitorAudioPlayReporter>();
        builder.Services.AddScoped<IVisitorQrApiService, VisitorQrApiService>();
        builder.Services.AddScoped<IVisitorQrDeepLinkService, VisitorQrDeepLinkService>();
        builder.Services.AddScoped<IVisitorPresenceReporter, VisitorPresenceReporter>();
        builder.Services.AddScoped<IVisitorLocationService, DeviceVisitorLocationService>();
#if ANDROID
        builder.Services.AddSingleton<IVisitorBackgroundLocationRuntime>(_ => new AndroidVisitorBackgroundLocationRuntime(Android.App.Application.Context));
#else
        builder.Services.AddSingleton<IVisitorBackgroundLocationRuntime, NoopVisitorBackgroundLocationRuntime>();
#endif
        builder.Services.AddScoped(_ =>
        {
            HttpMessageHandler handler = new HttpClientHandler();

#if DEBUG
            handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
#endif

            return new HttpClient(handler)
            {
                BaseAddress = visitorApiBaseAddress,
                Timeout = TimeSpan.FromSeconds(12)
            };
        });

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static VisitorApiOptions LoadVisitorApiOptions()
    {
        using var stream = FileSystem.OpenAppPackageFileAsync("visitor-api.json").GetAwaiter().GetResult();
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return VisitorApiOptions.Parse(json);
    }

    private static VisitorApiDeploymentEnvironment GetVisitorApiEnvironment()
    {
#if STAGING
        return VisitorApiDeploymentEnvironment.Staging;
#elif DEBUG || SMOKE
        return VisitorApiDeploymentEnvironment.Development;
#else
        return VisitorApiDeploymentEnvironment.Production;
#endif
    }

    private static Uri ResolveVisitorApiBaseAddress(VisitorApiOptions options, VisitorApiDeploymentEnvironment environment)
    {
        return VisitorApiEndpointResolver.Resolve(options, environment, GetClientPlatform());
    }

    private static VisitorApiClientPlatform GetClientPlatform()
    {
#if ANDROID
        return IsAndroidEmulator()
            ? VisitorApiClientPlatform.AndroidEmulator
            : VisitorApiClientPlatform.AndroidDevice;
#else
        return VisitorApiClientPlatform.Default;
#endif
    }

#if ANDROID
    private static bool IsAndroidEmulator()
    {
        return VisitorAndroidRuntimeDetector.LooksLikeEmulator(
            Build.Fingerprint,
            Build.Model,
            Build.Manufacturer,
            Build.Brand,
            Build.Device,
            Build.Product,
            Build.Hardware);
    }
#endif
}
