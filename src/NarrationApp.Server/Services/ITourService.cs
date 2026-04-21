using NarrationApp.Shared.DTOs.Tour;

namespace NarrationApp.Server.Services;

public interface ITourService
{
    Task<IReadOnlyList<TourDto>> GetAsync(bool includeUnpublished = false, CancellationToken cancellationToken = default);

    Task<TourDto> GetByIdAsync(int id, bool includeUnpublished = false, CancellationToken cancellationToken = default);

    Task<TourDto> CreateAsync(CreateTourRequest request, CancellationToken cancellationToken = default);

    Task<TourDto> UpdateAsync(int id, UpdateTourRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<TourSessionDto> StartAsync(int tourId, Guid userId, string? deviceId = null, CancellationToken cancellationToken = default);

    Task<TourSessionDto> ResumeAsync(int tourId, Guid userId, CancellationToken cancellationToken = default);

    Task<TourSessionDto> ProgressAsync(int tourId, Guid userId, UpdateTourProgressRequest request, CancellationToken cancellationToken = default);

    Task<TourSessionDto?> GetLatestSessionAsync(Guid userId, CancellationToken cancellationToken = default);
}
