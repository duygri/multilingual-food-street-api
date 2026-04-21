namespace NarrationApp.Web.Services;

public interface IAdminPoiOperationsService
{
    Task DeleteAsync(int poiId, CancellationToken cancellationToken = default);
}
