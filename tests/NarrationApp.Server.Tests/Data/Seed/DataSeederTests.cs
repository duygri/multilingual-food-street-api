using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Seed;
using NarrationApp.Shared.Constants;

namespace NarrationApp.Server.Tests.Data.Seed;

public sealed class DataSeederTests
{
    [Fact]
    public async Task SeedAsync_populates_the_expected_baseline_records()
    {
        await using var dbContext = CreateDbContext();
        var sut = new DataSeeder(dbContext, NullLogger<DataSeeder>.Instance);

        await sut.SeedAsync();

        Assert.Equal(3, await dbContext.Roles.CountAsync());
        Assert.Equal(2, await dbContext.AppUsers.CountAsync());
        Assert.Equal(5, await dbContext.Pois.CountAsync());
        Assert.Equal(5, await dbContext.Geofences.CountAsync());
        Assert.Equal(5, await dbContext.PoiTranslations.CountAsync());
        Assert.Equal(5, await dbContext.ManagedLanguages.CountAsync());

        var admin = await dbContext.AppUsers.SingleAsync(user => user.Email == AppConstants.DefaultAdminEmail);
        var owner = await dbContext.AppUsers.SingleAsync(user => user.Email == AppConstants.DefaultOwnerEmail);

        Assert.True(BCrypt.Net.BCrypt.Verify(AppConstants.DefaultAdminPassword, admin.PasswordHash));
        Assert.True(BCrypt.Net.BCrypt.Verify(AppConstants.DefaultOwnerPassword, owner.PasswordHash));
        Assert.Equal(AppConstants.DefaultLanguage, admin.PreferredLanguage);
        Assert.Equal(AppConstants.DefaultLanguage, owner.PreferredLanguage);
    }

    [Fact]
    public async Task SeedAsync_is_idempotent_when_called_multiple_times()
    {
        await using var dbContext = CreateDbContext();
        var sut = new DataSeeder(dbContext, NullLogger<DataSeeder>.Instance);

        await sut.SeedAsync();
        await sut.SeedAsync();

        Assert.Equal(3, await dbContext.Roles.CountAsync());
        Assert.Equal(2, await dbContext.AppUsers.CountAsync());
        Assert.Equal(5, await dbContext.Pois.CountAsync());
        Assert.Equal(5, await dbContext.Geofences.CountAsync());
        Assert.Equal(5, await dbContext.PoiTranslations.CountAsync());
        Assert.Equal(5, await dbContext.ManagedLanguages.CountAsync());
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }
}
