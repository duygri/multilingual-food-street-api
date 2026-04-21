using Microsoft.Extensions.Options;
using NarrationApp.Server.Configuration;
using NarrationApp.Server.Services;

namespace NarrationApp.Server.Tests.Services.Integrations;

public sealed class CloudflareR2StorageServiceTests
{
    [Fact]
    public async Task SaveAsync_uploads_object_under_audio_prefix_and_builds_public_url()
    {
        var client = new FakeR2ObjectClient();
        var sut = new CloudflareR2StorageService(
            client,
            Options.Create(new CloudflareR2Options
            {
                BucketName = "foodstreet",
                PublicBaseUrl = "https://cdn.foodstreet.test"
            }));

        await using var content = new MemoryStream([1, 2, 3, 4]);
        var result = await sut.SaveAsync("clip.mp3", content);

        Assert.Equal("foodstreet", client.LastBucketName);
        Assert.NotNull(client.LastObjectKey);
        Assert.StartsWith("audio/", client.LastObjectKey);
        Assert.EndsWith("_clip.mp3", client.LastObjectKey, StringComparison.Ordinal);
        Assert.Equal(client.LastObjectKey, result.StoragePath);
        Assert.Equal($"https://cdn.foodstreet.test/{client.LastObjectKey}", result.Url);
    }

    [Fact]
    public async Task SaveAsync_returns_empty_url_when_public_base_url_is_missing()
    {
        var client = new FakeR2ObjectClient();
        var sut = new CloudflareR2StorageService(
            client,
            Options.Create(new CloudflareR2Options
            {
                BucketName = "foodstreet"
            }));

        await using var content = new MemoryStream([1, 2, 3, 4]);
        var result = await sut.SaveAsync("clip.mp3", content);

        Assert.Equal(string.Empty, result.Url);
    }

    [Fact]
    public async Task SaveAsync_trims_public_base_url_before_building_public_url()
    {
        var client = new FakeR2ObjectClient();
        var sut = new CloudflareR2StorageService(
            client,
            Options.Create(new CloudflareR2Options
            {
                BucketName = "foodstreet",
                PublicBaseUrl = " https://cdn.foodstreet.test/ "
            }));

        await using var content = new MemoryStream([1, 2, 3, 4]);
        var result = await sut.SaveAsync("clip.mp3", content);

        Assert.Equal($"https://cdn.foodstreet.test/{client.LastObjectKey}", result.Url);
    }

    private sealed class FakeR2ObjectClient : IR2ObjectClient
    {
        public string? LastBucketName { get; private set; }

        public string? LastObjectKey { get; private set; }

        public byte[] LastPayload { get; private set; } = [];

        public Task DeleteAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<Stream> OpenReadAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
        {
            Stream stream = new MemoryStream(LastPayload, writable: false);
            return Task.FromResult(stream);
        }

        public async Task SaveAsync(string bucketName, string objectKey, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            LastBucketName = bucketName;
            LastObjectKey = objectKey;

            await using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            LastPayload = buffer.ToArray();
        }
    }
}
