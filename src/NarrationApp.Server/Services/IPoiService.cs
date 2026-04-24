using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public interface IPoiService
{
    Task<IReadOnlyList<PoiDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PoiDto?> GetByIdAsync(int poiId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PoiDto>> GetNearbyAsync(PoiNearRequest request, CancellationToken cancellationToken = default);

    Task<PoiDto> CreateAsync(Guid ownerId, CreatePoiRequest request, CancellationToken cancellationToken = default);

    Task<PoiDto> UpdateAsync(Guid actorUserId, UserRole actorRole, int poiId, UpdatePoiRequest request, CancellationToken cancellationToken = default);

    Task<PoiDto> UploadImageAsync(
        Guid actorUserId,
        UserRole actorRole,
        int poiId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<PoiDto> DeleteImageAsync(Guid actorUserId, UserRole actorRole, int poiId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid actorUserId, UserRole actorRole, int poiId, CancellationToken cancellationToken = default);
}
