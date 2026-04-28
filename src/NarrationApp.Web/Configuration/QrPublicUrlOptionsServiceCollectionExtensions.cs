using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NarrationApp.Web.Configuration;

public static class QrPublicUrlOptionsServiceCollectionExtensions
{
    public static IServiceCollection AddQrPublicUrlOptions(
        this IServiceCollection services,
        IConfiguration configuration,
        string hostBaseAddress)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var baseAddress = QrPublicBaseAddressResolver.Resolve(
            configuration["QrPublicBaseUrl"],
            hostBaseAddress);

        services.AddSingleton(new QrPublicUrlOptions
        {
            BaseAddress = baseAddress
        });

        return services;
    }
}
