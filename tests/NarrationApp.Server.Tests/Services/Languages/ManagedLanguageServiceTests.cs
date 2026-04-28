using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.DTOs.Languages;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Services.Languages;

public sealed class ManagedLanguageServiceTests
{
    [Fact]
    public async Task GetAsync_returns_seeded_languages_with_vietnamese_as_source()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var sut = new ManagedLanguageService(dbContext);
        var totalPois = await dbContext.Pois.CountAsync();

        var result = await sut.GetAsync();

        Assert.Equal(5, result.Count);

        var vietnamese = Assert.Single(result.Where(item => item.Code == "vi"));
        Assert.Equal(ManagedLanguageRole.Source, vietnamese.Role);
        Assert.True(vietnamese.IsActive);
        Assert.Equal(totalPois, vietnamese.TranslationCoverageCount);
        Assert.Equal(totalPois, vietnamese.TranslationCoverageTotal);
    }

    [Fact]
    public async Task CreateAsync_adds_new_active_language()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var sut = new ManagedLanguageService(dbContext);

        var created = await sut.CreateAsync(new CreateManagedLanguageRequest
        {
            Code = "fr",
            DisplayName = "French",
            NativeName = "Francais",
            FlagCode = "FR"
        });

        Assert.Equal("fr", created.Code);
        Assert.Equal("French", created.DisplayName);
        Assert.Equal(ManagedLanguageRole.TranslationAudio, created.Role);
        Assert.True(created.IsActive);

        var all = await sut.GetAsync();
        Assert.Contains(all, item => item.Code == "fr");
    }

    [Fact]
    public async Task DeleteAsync_rejects_removing_vietnamese_source_language()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var sut = new ManagedLanguageService(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.DeleteAsync("vi"));
    }
}
