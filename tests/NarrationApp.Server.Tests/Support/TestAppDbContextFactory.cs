using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Server.Data.Seed;
using NarrationApp.Shared.Constants;

namespace NarrationApp.Server.Tests.Support;

internal static class TestAppDbContextFactory
{
    public static async Task<AppDbContext> CreateSeededAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var dbContext = new AppDbContext(options);
        var seeder = new DataSeeder(dbContext, NullLogger<DataSeeder>.Instance);
        await seeder.SeedAsync();
        return dbContext;
    }

    public static async Task<AppUser> AddOwnerAsync(AppDbContext dbContext, string email)
    {
        var ownerRole = await dbContext.Roles.SingleAsync(role => role.Name == "poi_owner");
        var owner = new AppUser
        {
            Id = Guid.NewGuid(),
            FullName = "Test Owner",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Owner@123"),
            PreferredLanguage = AppConstants.DefaultLanguage,
            RoleId = ownerRole.Id,
            IsActive = true
        };

        dbContext.AppUsers.Add(owner);
        await dbContext.SaveChangesAsync();
        return owner;
    }

    public static async Task<AppUser> AddTouristAsync(AppDbContext dbContext, string email)
    {
        var touristRole = await dbContext.Roles.SingleAsync(role => role.Name == "tourist");
        var tourist = new AppUser
        {
            Id = Guid.NewGuid(),
            FullName = "Test Tourist",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tourist@123"),
            PreferredLanguage = AppConstants.DefaultLanguage,
            RoleId = touristRole.Id,
            IsActive = true
        };

        dbContext.AppUsers.Add(tourist);
        await dbContext.SaveChangesAsync();
        return tourist;
    }
}

internal static class TestStorageRoot
{
    public static string Create()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "test-storage", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
