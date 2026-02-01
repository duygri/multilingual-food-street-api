using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using FoodStreet.Client;
using FoodStreet.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ========================================
// CORE SERVICES
// ========================================

// LocalStorage (must be singleton for JSInterop)
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();

// ========================================
// HTTP CLIENT with Auth Handler
// ========================================
builder.Services.AddScoped<AuthorizingMessageHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthorizingMessageHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler)
    {
        BaseAddress = new Uri("https://localhost:7214")
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
builder.Services.AddScoped<IAudioService, AudioService>();
builder.Services.AddLocalization();
builder.Services.AddSingleton<ILocalizationService, LocalizationService>();

await builder.Build().RunAsync();
