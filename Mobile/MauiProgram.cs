using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Authorization;
using FoodStreet.Client;
using FoodStreet.Client.Services;
using FoodStreet.Client.Layout;
using FoodStreet.Mobile.Services;
using Plugin.Maui.Audio;

namespace FoodStreet.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

        // ========================================
        // CORE SERVICES
        // ========================================
        // Use JS-based Storage (works in WebView)
        builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
        builder.Services.AddScoped<ISessionStorageService, SessionStorageService>();

        // ========================================
        // HTTP CLIENT
        // ========================================
        // Note: 10.0.2.2 is required for Android Emulator to access host localhost
        // For iOS Simulator, use "http://localhost:5053"
        // For physical device, use your PC's IP address
        // Using HTTP for development ease, adjust Port if needed
        string baseAddress = DeviceInfo.Platform == DevicePlatform.Android ? "https://10.0.2.2:7214" : "https://localhost:7214";
        
        builder.Services.AddScoped<AuthorizingMessageHandler>();
        builder.Services.AddScoped(sp =>
        {
            var handler = sp.GetRequiredService<AuthorizingMessageHandler>();
#if DEBUG
            // SECURITY: Bypass SSL only in DEBUG (self-signed dev certs).
            // In Release/Production, remove this so real cert validation applies.
            handler.InnerHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
#endif
            return new HttpClient(handler)
            {
                BaseAddress = new Uri(baseAddress)
            };
        });

        // ========================================
        // AUTHENTICATION
        // ========================================
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<JwtAuthStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(sp => 
            sp.GetRequiredService<JwtAuthStateProvider>());
        builder.Services.AddAuthorizationCore();

        // ========================================
        // APPLICATION SERVICES
        // ========================================
        builder.Services.AddScoped<IFoodClientService, FoodClientService>();
        builder.Services.AddScoped<ILocationClientService, LocationClientService>();
        builder.Services.AddScoped<IAudioService, AudioService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IGpsTrackingService, NativeGpsTrackingService>();
        builder.Services.AddScoped<ITtsService, NativeTtsService>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddScoped<IFavoritesService, FavoritesService>();
        
        // Audio Plugin
        builder.Services.AddSingleton(AudioManager.Current);

        builder.Services.AddLocalization();
        builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
        builder.Services.AddSingleton<IPlatformDetector, MobilePlatformDetector>();

		return builder.Build();
	}
}
