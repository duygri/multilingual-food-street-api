using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.DTOs.Tour;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed class TourService(
    AppDbContext dbContext,
    INotificationService notificationService,
    IVisitEventService visitEventService) : ITourService
{
    public async Task<IReadOnlyList<TourDto>> GetAsync(bool includeUnpublished = false, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Tours
            .AsNoTracking()
            .Include(tour => tour.Stops)
            .AsQueryable();

        if (!includeUnpublished)
        {
            query = query.Where(tour => tour.Status == TourStatus.Published);
        }

        var tours = await query
            .OrderBy(tour => tour.Title)
            .ToListAsync(cancellationToken);

        return tours.Select(MapTour).ToArray();
    }

    public async Task<TourDto> GetByIdAsync(int id, bool includeUnpublished = false, CancellationToken cancellationToken = default)
    {
        var tour = await dbContext.Tours
            .AsNoTracking()
            .Include(item => item.Stops)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Tour was not found.");

        if (!includeUnpublished && tour.Status != TourStatus.Published)
        {
            throw new KeyNotFoundException("Tour was not found.");
        }

        return MapTour(tour);
    }

    public async Task<TourDto> CreateAsync(CreateTourRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedStops = await ValidateStopsAsync(request.Stops, cancellationToken);
        var tour = new Tour
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            EstimatedMinutes = request.EstimatedMinutes,
            CoverImage = request.CoverImage?.Trim(),
            Status = TourStatus.Draft,
            Stops = normalizedStops
        };

        dbContext.Tours.Add(tour);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapTour(tour);
    }

    public async Task<TourDto> UpdateAsync(int id, UpdateTourRequest request, CancellationToken cancellationToken = default)
    {
        var tour = await dbContext.Tours
            .Include(item => item.Stops)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Tour was not found.");

        var normalizedStops = await ValidateStopsAsync(request.Stops, cancellationToken);
        var shouldNotifyPublished = tour.Status != TourStatus.Published && request.Status == TourStatus.Published;

        tour.Title = request.Title.Trim();
        tour.Description = request.Description.Trim();
        tour.EstimatedMinutes = request.EstimatedMinutes;
        tour.CoverImage = request.CoverImage?.Trim();
        tour.Status = request.Status;

        dbContext.TourStops.RemoveRange(tour.Stops);
        tour.Stops = normalizedStops;

        await dbContext.SaveChangesAsync(cancellationToken);

        if (shouldNotifyPublished)
        {
            await NotifyTourPublishedAsync(tour, cancellationToken);
        }

        return MapTour(tour);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var tour = await dbContext.Tours
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Tour was not found.");

        dbContext.Tours.Remove(tour);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TourSessionDto> StartAsync(int tourId, Guid userId, string? deviceId = null, CancellationToken cancellationToken = default)
    {
        var tour = await LoadPublishedTourAsync(tourId, cancellationToken);
        await PauseActiveSessionsAsync(userId, null, cancellationToken);

        var now = DateTime.UtcNow;
        var session = new TourSession
        {
            TourId = tour.Id,
            UserId = userId,
            Status = TourSessionStatus.InProgress,
            CurrentStopSequence = 0,
            StartedAt = now,
            UpdatedAt = now
        };

        dbContext.TourSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapSession(session, tour.Stops.Count);
    }

    public async Task<TourSessionDto> ResumeAsync(int tourId, Guid userId, CancellationToken cancellationToken = default)
    {
        var tour = await LoadPublishedTourAsync(tourId, cancellationToken);
        var session = await dbContext.TourSessions
            .Where(item => item.UserId == userId && item.TourId == tourId && item.Status != TourSessionStatus.Completed && item.Status != TourSessionStatus.Abandoned)
            .OrderByDescending(item => item.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("No resumable tour session was found.");

        await PauseActiveSessionsAsync(userId, session.Id, cancellationToken);
        session.Status = TourSessionStatus.InProgress;
        session.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapSession(session, tour.Stops.Count);
    }

    public async Task<TourSessionDto> ProgressAsync(int tourId, Guid userId, UpdateTourProgressRequest request, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.TourSessions
            .Include(item => item.Tour)
            .ThenInclude(tour => tour!.Stops)
            .Where(item => item.UserId == userId && item.TourId == tourId && item.Status == TourSessionStatus.InProgress)
            .OrderByDescending(item => item.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("No active tour session was found.");

        var orderedStops = session.Tour!.Stops
            .OrderBy(item => item.Sequence)
            .ToList();

        if (orderedStops.Count == 0)
        {
            throw new InvalidOperationException("Tour has no stops.");
        }

        if (session.CurrentStopSequence > 0)
        {
            var currentStop = orderedStops.Single(item => item.Sequence == session.CurrentStopSequence);
            if (currentStop.PoiId == request.PoiId)
            {
                return MapSession(session, orderedStops.Count);
            }
        }

        var nextSequence = session.CurrentStopSequence + 1;
        var nextStop = orderedStops.SingleOrDefault(item => item.Sequence == nextSequence)
            ?? throw new InvalidOperationException("Tour session is already complete.");

        if (nextStop.PoiId != request.PoiId)
        {
            throw new InvalidOperationException("The reported POI does not match the next stop in the tour.");
        }

        var now = DateTime.UtcNow;
        session.CurrentStopSequence = nextSequence;
        session.UpdatedAt = now;
        if (nextSequence == orderedStops.Count)
        {
            session.Status = TourSessionStatus.Completed;
            session.CompletedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await visitEventService.CreateAsync(
            new VisitEventService.CreateVisitEventRequest
            {
                UserId = userId,
                DeviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? $"tour-session-{userId:N}" : request.DeviceId,
                PoiId = request.PoiId,
                EventType = EventType.TourProgress,
                Source = "tour",
                ListenDurationSeconds = 0,
                Lat = request.Lat,
                Lng = request.Lng
            },
            cancellationToken);

        return MapSession(session, orderedStops.Count);
    }

    public async Task<TourSessionDto?> GetLatestSessionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.TourSessions
            .AsNoTracking()
            .Include(item => item.Tour)
            .ThenInclude(tour => tour!.Stops)
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.UpdatedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return session is null ? null : MapSession(session, session.Tour!.Stops.Count);
    }

    private async Task<Tour> LoadPublishedTourAsync(int tourId, CancellationToken cancellationToken)
    {
        var tour = await dbContext.Tours
            .Include(item => item.Stops)
            .SingleOrDefaultAsync(item => item.Id == tourId, cancellationToken)
            ?? throw new KeyNotFoundException("Tour was not found.");

        if (tour.Status != TourStatus.Published)
        {
            throw new InvalidOperationException("Only published tours can be started.");
        }

        return tour;
    }

    private async Task<List<TourStop>> ValidateStopsAsync(IReadOnlyList<UpsertTourStopRequest> stops, CancellationToken cancellationToken)
    {
        if (stops.Count == 0)
        {
            throw new InvalidOperationException("Tour must contain at least one stop.");
        }

        var orderedStops = stops
            .OrderBy(item => item.Sequence)
            .ToList();

        if (orderedStops.Select(item => item.Sequence).Distinct().Count() != orderedStops.Count)
        {
            throw new InvalidOperationException("Tour stop sequences must be unique.");
        }

        if (orderedStops.Select(item => item.PoiId).Distinct().Count() != orderedStops.Count)
        {
            throw new InvalidOperationException("Tour stop POIs must be unique.");
        }

        for (var index = 0; index < orderedStops.Count; index++)
        {
            if (orderedStops[index].Sequence != index + 1)
            {
                throw new InvalidOperationException("Tour stop sequences must be contiguous and start at 1.");
            }
        }

        var poiIds = orderedStops.Select(item => item.PoiId).ToArray();
        var existingPoiIds = await dbContext.Pois
            .Where(item => poiIds.Contains(item.Id))
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

        if (existingPoiIds.Count != poiIds.Length)
        {
            throw new KeyNotFoundException("One or more POIs in the tour were not found.");
        }

        return orderedStops
            .Select(item => new TourStop
            {
                PoiId = item.PoiId,
                Sequence = item.Sequence,
                RadiusMeters = item.RadiusMeters
            })
            .ToList();
    }

    private async Task PauseActiveSessionsAsync(Guid userId, int? excludedSessionId, CancellationToken cancellationToken)
    {
        var activeSessions = await dbContext.TourSessions
            .Where(item => item.UserId == userId && item.Status == TourSessionStatus.InProgress && item.Id != excludedSessionId)
            .ToListAsync(cancellationToken);

        if (activeSessions.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var activeSession in activeSessions)
        {
            activeSession.Status = TourSessionStatus.Paused;
            activeSession.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task NotifyTourPublishedAsync(Tour tour, CancellationToken cancellationToken)
    {
        var touristRoleId = await dbContext.Roles
            .Where(role => role.Name == "tourist")
            .Select(role => role.Id)
            .SingleAsync(cancellationToken);

        var touristIds = await dbContext.AppUsers
            .Where(user => user.RoleId == touristRoleId && user.IsActive)
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        foreach (var touristId in touristIds)
        {
            await notificationService.CreateAsync(
                touristId,
                NotificationType.TourPublished,
                "Tour mới đã xuất bản",
                $"Tour \"{tour.Title}\" hiện đã sẵn sàng để bắt đầu.",
                cancellationToken);
        }
    }

    private static TourDto MapTour(Tour tour)
    {
        return new TourDto
        {
            Id = tour.Id,
            Title = tour.Title,
            Description = tour.Description,
            EstimatedMinutes = tour.EstimatedMinutes,
            CoverImage = tour.CoverImage,
            Status = tour.Status,
            Stops = tour.Stops
                .OrderBy(item => item.Sequence)
                .Select(MapStop)
                .ToArray()
        };
    }

    private static TourStopDto MapStop(TourStop stop)
    {
        return new TourStopDto
        {
            Id = stop.Id,
            TourId = stop.TourId,
            PoiId = stop.PoiId,
            Sequence = stop.Sequence,
            RadiusMeters = stop.RadiusMeters
        };
    }

    private static TourSessionDto MapSession(TourSession session, int totalStops)
    {
        return new TourSessionDto
        {
            Id = session.Id,
            TourId = session.TourId,
            UserId = session.UserId,
            Status = session.Status,
            CurrentStopSequence = session.CurrentStopSequence,
            TotalStops = totalStops,
            StartedAtUtc = session.StartedAt,
            UpdatedAtUtc = session.UpdatedAt,
            CompletedAtUtc = session.CompletedAt
        };
    }
}
