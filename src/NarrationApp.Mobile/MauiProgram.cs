using Microsoft.Extensions.Logging;
using NarrationApp.Mobile.Features.Home;
using NarrationApp.Mobile.Services;
using Microsoft.Maui.Storage;

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
        builder.Services.AddSingleton<IVisitorDeviceIdentityProvider, DeviceVisitorIdentityProvider>();
        builder.Services.AddScoped<IVisitorContentService, VisitorContentService>();
        builder.Services.AddScoped<IVisitorAudioCatalogService, VisitorAudioCatalogService>();
        builder.Services.AddScoped<IVisitorQrApiService, VisitorQrApiService>();
        builder.Services.AddScoped<IVisitorQrDeepLinkService, VisitorQrDeepLinkService>();
        builder.Services.AddScoped<IVisitorLocationService, DeviceVisitorLocationService>();
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
#if ANDROID
        const bool IsAndroid = true;
#else
        const bool IsAndroid = false;
#endif

        return VisitorApiEndpointResolver.Resolve(options, environment, IsAndroid);
    }
}
