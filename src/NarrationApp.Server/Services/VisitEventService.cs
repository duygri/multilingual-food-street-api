using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed class VisitEventService(AppDbContext dbContext) : IVisitEventService
{
    public async Task CreateAsync(CreateVisitEventRequest request, CancellationToken cancellationToken = default)
    {
        var visitEvent = new VisitEvent
        {
            UserId = request.UserId,
            DeviceId = request.DeviceId,
            PoiId = request.PoiId,
            EventType = request.EventType,
            Source = request.Source,
            ListenDurationSeconds = request.ListenDurationSeconds,
            Lat = request.Lat,
            Lng = request.Lng,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.VisitEvents.Add(visitEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<object>> GetByPoiAsync(int poiId, CancellationToken cancellationToken = default)
    {
        return await dbContext.VisitEvents
            .AsNoTracking()
            .Where(item => item.PoiId == poiId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new
            {
                item.Id,
                item.UserId,
                item.DeviceId,
                item.PoiId,
                item.EventType,
                item.Source,
                item.ListenDurationSeconds,
                item.Lat,
                item.Lng,
                item.CreatedAt
            })
            .Cast<object>()
            .ToListAsync(cancellationToken);
    }

    public sealed class CreateVisitEventRequest
    {
        public Guid? UserId { get; init; }

        public string DeviceId { get; init; } = string.Empty;

        public int PoiId { get; init; }

        public EventType EventType { get; init; }

        public string Source { get; init; } = string.Empty;

        public int ListenDurationSeconds { get; init; }

        public double? Lat { get; init; }

        public double? Lng { get; init; }
    }
}
