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
        var touristApiOptions = LoadTouristApiOptions();
        var touristApiEnvironment = GetTouristApiEnvironment();
        var touristApiBaseAddress = ResolveTouristApiBaseAddress(touristApiOptions, touristApiEnvironment);

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton(new TouristMapOptions());
        builder.Services.AddSingleton(touristApiOptions);
        builder.Services.AddSingleton<ITouristAuthSessionStore, SecureTouristAuthSessionStore>();
        builder.Services.AddSingleton<ITouristDeviceIdentityProvider, DeviceTouristIdentityProvider>();
        builder.Services.AddScoped<ITouristAuthApiService, TouristAuthApiService>();
        builder.Services.AddScoped<ITouristContentService, TouristContentService>();
        builder.Services.AddScoped<ITouristAudioCatalogService, TouristAudioCatalogService>();
        builder.Services.AddScoped<ITouristQrApiService, TouristQrApiService>();
        builder.Services.AddScoped<ITouristTourSessionApiService, TouristTourSessionApiService>();
        builder.Services.AddScoped<ITouristLocationService, DeviceTouristLocationService>();
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
                BaseAddress = touristApiBaseAddress,
                Timeout = TimeSpan.FromSeconds(12)
            };
        });

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static TouristApiOptions LoadTouristApiOptions()
    {
        using var stream = FileSystem.OpenAppPackageFileAsync("tourist-api.json").GetAwaiter().GetResult();
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return TouristApiOptions.Parse(json);
    }

    private static TouristApiDeploymentEnvironment GetTouristApiEnvironment()
    {
#if STAGING
        return TouristApiDeploymentEnvironment.Staging;
#elif DEBUG || SMOKE
        return TouristApiDeploymentEnvironment.Development;
#else
        return TouristApiDeploymentEnvironment.Production;
#endif
    }

    private static Uri ResolveTouristApiBaseAddress(TouristApiOptions options, TouristApiDeploymentEnvironment environment)
    {
#if ANDROID
        const bool IsAndroid = true;
#else
        const bool IsAndroid = false;
#endif

        return TouristApiEndpointResolver.Resolve(options, environment, IsAndroid);
    }
}
