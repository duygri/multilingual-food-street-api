using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FoodStreet.Client;
using FoodStreet.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7214") }); // Hardcoded for now to ensure it works, better to read from config later
builder.Services.AddScoped<IFoodClientService, FoodClientService>();
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
