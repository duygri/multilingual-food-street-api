using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Services.Pois;

public sealed class PoiServiceTests
{
    [Fact]
    public async Task GetNearbyAsync_returns_pois_sorted_by_distance()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var sut = new PoiService(dbContext);

        var result = await sut.GetNearbyAsync(new PoiNearRequest
        {
            Lat = 10.75986,
            Lng = 106.70188,
            RadiusMeters = 1200
        });

        Assert.NotEmpty(result);
        Assert.Equal("pho-am-thuc-vinh-khanh", result[0].Slug);
        Assert.True(result.Count >= 2);
    }

    [Fact]
    public async Task UpdateAsync_rejects_owner_attempting_to_edit_foreign_poi()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var foreignOwner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "owner2@narration.app");
        var poi = await dbContext.Pois.AsNoTracking().FirstAsync();
        var sut = new PoiService(dbContext);

        var action = async () => await sut.UpdateAsync(
            foreignOwner.Id,
            UserRole.PoiOwner,
            poi.Id,
            new UpdatePoiRequest
            {
                Name = poi.Name + " updated",
                Slug = poi.Slug,
                Lat = poi.Lat,
                Lng = poi.Lng,
                Priority = poi.Priority,
                NarrationMode = poi.NarrationMode,
                MapLink = poi.MapLink,
                ImageUrl = poi.ImageUrl,
                Status = PoiStatus.Published
            });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(action);
    }

    [Fact]
    public async Task UploadImageAsync_updates_owner_poi_image_url()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var owner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "owner-image-upload@narration.app");
        var poi = new Poi
        {
            Name = "POI upload ảnh",
            Slug = "poi-upload-anh",
            OwnerId = owner.Id,
            Lat = 10.758,
            Lng = 106.701,
            Priority = 5,
            NarrationMode = NarrationMode.Both,
            Description = "POI description",
            TtsScript = "POI script",
            Status = PoiStatus.Draft,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        dbContext.Pois.Add(poi);
        await dbContext.SaveChangesAsync();

        var storage = new StubStorageService(("images/poi-upload.png", "https://cdn.test/images/poi-upload.png"));
        var sut = new PoiService(dbContext, storage);
        await using var content = new MemoryStream([1, 2, 3, 4]);

        var result = await sut.UploadImageAsync(
            owner.Id,
            UserRole.PoiOwner,
            poi.Id,
            "poi-upload.png",
            "image/png",
            content);

        var persistedPoi = await dbContext.Pois.SingleAsync(item => item.Id == poi.Id);

        Assert.Equal("https://cdn.test/images/poi-upload.png", result.ImageUrl);
        Assert.Equal("https://cdn.test/images/poi-upload.png", persistedPoi.ImageUrl);
        Assert.Equal(["poi-upload.png"], storage.SavedFileNames);
    }

    [Fact]
    public async Task UploadImageAsync_rejects_owner_attempting_to_upload_for_foreign_poi()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var owner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "owner-image-foreign@narration.app");
        var foreignOwner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "owner-image-original@narration.app");
        var poi = new Poi
        {
            Name = "POI không thuộc owner",
            Slug = "poi-khong-thuoc-owner",
            OwnerId = foreignOwner.Id,
            Lat = 10.758,
            Lng = 106.701,
            Priority = 5,
            NarrationMode = NarrationMode.Both,
            Description = "POI description",
            TtsScript = "POI script",
            Status = PoiStatus.Draft,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        dbContext.Pois.Add(poi);
        await dbContext.SaveChangesAsync();

        var sut = new PoiService(dbContext, new StubStorageService(("images/foreign.png", "https://cdn.test/images/foreign.png")));
        await using var content = new MemoryStream([1, 2, 3, 4]);

        Func<Task> action = async () => await sut.UploadImageAsync(
            owner.Id,
            UserRole.PoiOwner,
            poi.Id,
            "foreign.png",
            "image/png",
            content);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(action);
    }

    [Fact]
    public async Task UploadImageAsync_replaces_existing_image_url()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var owner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "owner-image-replace@narration.app");
        var poi = new Poi
        {
            Name = "POI thay ảnh",
            Slug = "poi-thay-anh",
            OwnerId = owner.Id,
            Lat = 10.758,
            Lng = 106.701,
            Priority = 5,
            NarrationMode = NarrationMode.Both,
            Description = "POI description",
            TtsScript = "POI script",
            ImageUrl = "https://cdn.test/images/original.png",
            Status = PoiStatus.Published,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        dbContext.Pois.Add(poi);
        await dbContext.SaveChangesAsync();

        var sut = new PoiService(dbContext, new StubStorageService(("images/replaced.png", "https://cdn.test/images/replaced.png")));
        await using var content = new MemoryStream([5, 6, 7, 8]);

        var result = await sut.UploadImageAsync(
            owner.Id,
            UserRole.PoiOwner,
            poi.Id,
            "replaced.png",
            "image/png",
            content);

        var persistedPoi = await dbContext.Pois.SingleAsync(item => item.Id == poi.Id);

        Assert.Equal("https://cdn.test/images/replaced.png", result.ImageUrl);
        Assert.Equal("https://cdn.test/images/replaced.png", persistedPoi.ImageUrl);
        Assert.Equal(PoiStatus.Updated, persistedPoi.Status);
    }

    [Fact]
    public async Task DeleteImageAsync_clears_poi_image_url()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var owner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "owner-image-delete@narration.app");
        var poi = new Poi
        {
            Name = "POI xóa ảnh",
            Slug = "poi-xoa-anh",
            OwnerId = owner.Id,
            Lat = 10.758,
            Lng = 106.701,
            Priority = 5,
            NarrationMode = NarrationMode.Both,
            Description = "POI description",
            TtsScript = "POI script",
            ImageUrl = "https://cdn.test/images/to-delete.png",
            Status = PoiStatus.Draft,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        dbContext.Pois.Add(poi);
        await dbContext.SaveChangesAsync();

        var sut = new PoiService(dbContext, new StubStorageService(("images/unused.png", "https://cdn.test/images/unused.png")));

        var result = await sut.DeleteImageAsync(owner.Id, UserRole.PoiOwner, poi.Id);
        var persistedPoi = await dbContext.Pois.SingleAsync(item => item.Id == poi.Id);

        Assert.Null(result.ImageUrl);
        Assert.Null(persistedPoi.ImageUrl);
    }

    private sealed class StubStorageService((string StoragePath, string Url) saveResult) : IStorageService
    {
        public List<string> SavedFileNames { get; } = [];

        public string ProviderName => "stub-storage";

        public Task<(string StoragePath, string Url)> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
        {
            SavedFileNames.Add(fileName);
            return Task.FromResult(saveResult);
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            Stream stream = new MemoryStream();
            return Task.FromResult(stream);
        }

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
