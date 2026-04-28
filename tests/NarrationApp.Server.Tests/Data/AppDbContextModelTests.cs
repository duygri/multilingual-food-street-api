using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Server.Tests.Support;

namespace NarrationApp.Server.Tests.Data;

public sealed class AppDbContextModelTests
{
    [Fact]
    public async Task VisitEvents_have_analytics_query_indexes()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var entityType = dbContext.Model.FindEntityType(typeof(VisitEvent));

        Assert.NotNull(entityType);
        Assert.Contains(entityType!.GetIndexes(), index => HasProperties(index, nameof(VisitEvent.CreatedAt)));
        Assert.Contains(entityType.GetIndexes(), index => HasProperties(index, nameof(VisitEvent.EventType), nameof(VisitEvent.CreatedAt)));
        Assert.Contains(entityType.GetIndexes(), index => HasProperties(index, nameof(VisitEvent.DeviceId), nameof(VisitEvent.CreatedAt)));
        Assert.Contains(entityType.GetIndexes(), index => HasProperties(index, nameof(VisitEvent.PoiId), nameof(VisitEvent.EventType), nameof(VisitEvent.CreatedAt)));
    }

    private static bool HasProperties(IIndex index, params string[] propertyNames)
    {
        return index.Properties.Select(property => property.Name).SequenceEqual(propertyNames);
    }
}
