using NarrationApp.Shared.DTOs.Visitor;

namespace NarrationApp.Server.Services;

public interface IVisitorNotificationService
{
    Task<IReadOnlyList<VisitorNotificationDto>> GetAsync(
        VisitorNotificationsQuery query,
        CancellationToken cancellationToken = default);
}
