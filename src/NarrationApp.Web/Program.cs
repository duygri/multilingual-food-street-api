using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NarrationApp.SharedUI.Auth;
using NarrationApp.SharedUI.Services;
using NarrationApp.Web;
using NarrationApp.Web.Configuration;
using NarrationApp.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseAddress = ApiBaseAddressResolver.Resolve(
    builder.Configuration["ApiBaseUrl"],
    builder.HostEnvironment.BaseAddress);

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = apiBaseAddress });
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<BrowserAuthSessionStore>();
builder.Services.AddScoped<IAuthSessionStore>(sp => sp.GetRequiredService<BrowserAuthSessionStore>());
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<AuthClientService>();
builder.Services.AddScoped<OwnerPortalService>();
builder.Services.AddScoped<AdminPortalService>();
builder.Services.AddScoped<IOwnerPortalService>(sp => sp.GetRequiredService<OwnerPortalService>());
builder.Services.AddScoped<IAdminPortalService>(sp => sp.GetRequiredService<AdminPortalService>());
builder.Services.AddScoped<IAdminPoiOperationsService, AdminPoiOperationsService>();
builder.Services.AddScoped<IAudioPortalService, AudioPortalService>();
builder.Services.AddScoped<IGeofencePortalService, GeofencePortalService>();
builder.Services.AddScoped<IModerationPortalService, ModerationPortalService>();
builder.Services.AddScoped<IQrPortalService, QrPortalService>();
builder.Services.AddScoped<ITourPortalService, TourPortalService>();
builder.Services.AddScoped<ITranslationPortalService, TranslationPortalService>();
builder.Services.AddScoped<ICategoryPortalService, CategoryPortalService>();
builder.Services.AddScoped<ILanguagePortalService, LanguagePortalService>();
builder.Services.AddScoped<INotificationRealtimeService, SignalRService>();
builder.Services.AddScoped<INotificationCenterService, NotificationCenterApiService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IAudioRefreshPump, PeriodicAudioRefreshPump>();

await builder.Build().RunAsync();
