using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NarrationApp.Web.Configuration;

public static class AnalyticsMapOptionsServiceCollectionExtensions
{
    public static IServiceCollection AddAnalyticsMapOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton(new AnalyticsMapOptions
        {
            AccessToken = configuration["Mapbox:AccessToken"] ?? string.Empty,
            StyleUrl = configuration["Mapbox:StyleUrl"] ?? "mapbox://styles/mapbox/dark-v11"
        });

        return services;
    }
}
