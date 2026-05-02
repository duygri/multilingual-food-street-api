using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Seed;
using NarrationApp.Server.Services;

namespace NarrationApp.Server.Tests.Support;

internal sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString("N");
    private readonly IReadOnlyDictionary<string, string?> _additionalConfiguration;

    public TestWebApplicationFactory(IReadOnlyDictionary<string, string?>? additionalConfiguration = null)
    {
        _additionalConfiguration = additionalConfiguration ?? new Dictionary<string, string?>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["RateLimiting:Auth:PermitLimit"] = "2",
                ["RateLimiting:Auth:WindowSeconds"] = "60",
                ["RateLimiting:Mutation:PermitLimit"] = "3",
                ["RateLimiting:Mutation:WindowSeconds"] = "60",
                ["RateLimiting:Generation:PermitLimit"] = "2",
                ["RateLimiting:Generation:WindowSeconds"] = "60",
                ["GoogleCloud:CredentialsFilePath"] = string.Empty,
                ["GoogleCloud:ProjectId"] = string.Empty,
                ["CloudflareR2:AccountId"] = string.Empty,
                ["CloudflareR2:AccessKeyId"] = string.Empty,
                ["CloudflareR2:SecretAccessKey"] = string.Empty,
                ["CloudflareR2:BucketName"] = string.Empty,
                ["CloudflareR2:PublicBaseUrl"] = string.Empty,
                ["PublicQr:BaseUrl"] = "https://public.foodstreet.test/",
                ["MobileAppLinks:Android:0:PackageName"] = "com.foodstreet.tourist.dev",
                ["MobileAppLinks:Android:0:Sha256CertFingerprints:0"] = "11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF:00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF:00"
            };

            foreach (var item in _additionalConfiguration)
            {
                settings[item.Key] = item.Value;
            }

            configurationBuilder.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(AppDbContext));
            services.RemoveAll(typeof(IGoogleAccessTokenProvider));
            services.RemoveAll(typeof(IGoogleTranslationService));
            services.RemoveAll(typeof(IGoogleTtsService));
            services.RemoveAll(typeof(GoogleCloudTranslationService));
            services.RemoveAll(typeof(GoogleCloudTtsService));

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
            services.AddSingleton<IGoogleTranslationService, MockGoogleTranslationService>();
            services.AddSingleton<IGoogleTtsService, MockGoogleTtsService>();
        });
    }

    public async Task SeedAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        await seeder.SeedAsync();
    }
}
