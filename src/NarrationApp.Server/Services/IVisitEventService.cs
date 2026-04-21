using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public interface IVisitEventService
{
    Task CreateAsync(VisitEventService.CreateVisitEventRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<object>> GetByPoiAsync(int poiId, CancellationToken cancellationToken = default);
}
