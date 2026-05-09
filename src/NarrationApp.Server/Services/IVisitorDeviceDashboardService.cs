using NarrationApp.Shared.DTOs.Admin;

namespace NarrationApp.Server.Services;

public interface IVisitorDeviceDashboardService
{
    Task<IReadOnlyList<VisitorDeviceSummaryDto>> GetAsync(CancellationToken cancellationToken = default);
}
