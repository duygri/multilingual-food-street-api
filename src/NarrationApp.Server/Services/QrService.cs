using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.DTOs.QR;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed class QrService(AppDbContext dbContext) : IQrService
{
    private static readonly TimeSpan DuplicateQrVisitCooldown = TimeSpan.FromMinutes(30);

    public async Task<QrCodeDto> CreateAsync(CreateQrRequest request, CancellationToken cancellationToken = default)
    {
        var targetType = NormalizeTargetType(request.TargetType);
        var targetId = await ValidateTargetAsync(targetType, request.TargetId, cancellationToken);
        var code = await GenerateUniqueCodeAsync(cancellationToken);
        var qrCode = new QrCode
        {
            Code = code,
            TargetType = targetType,
            TargetId = targetId,
            LocationHint = request.LocationHint,
            ExpiresAt = request.ExpiresAtUtc
        };

        dbContext.QrCodes.Add(qrCode);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await BuildDtoAsync(qrCode, cancellationToken);
    }

    public async Task<IReadOnlyList<QrCodeDto>> GetAsync(string? targetType = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.QrCodes
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(targetType))
        {
            var normalizedTargetType = NormalizeTargetType(targetType);
            query = query.Where(item => item.TargetType == normalizedTargetType);
        }

        var items = await query
            .OrderByDescending(item => item.Id)
            .ToListAsync(cancellationToken);

        var poiTargetIds = items
            .Where(item => string.Equals(item.TargetType, "poi", StringComparison.OrdinalIgnoreCase))
            .Select(item => item.TargetId)
            .Distinct()
            .ToArray();

        var qrScanCountsByPoiId = poiTargetIds.Length == 0
            ? new Dictionary<int, int>()
            : await dbContext.VisitEvents
                .AsNoTracking()
                .Where(item => poiTargetIds.Contains(item.PoiId) && item.EventType == EventType.QrScan)
                .GroupBy(item => item.PoiId)
                .Select(group => new
                {
                    PoiId = group.Key,
                    ScanCount = group.Count()
                })
                .ToDictionaryAsync(item => item.PoiId, item => item.ScanCount, cancellationToken);

        return items.Select(item => ToDto(item, qrScanCountsByPoiId)).ToArray();
    }

    public async Task<QrCodeDto> ResolveAsync(string code, CancellationToken cancellationToken = default)
    {
        var qrCode = await FindActiveCodeAsync(code, cancellationToken);
        return await BuildDtoAsync(qrCode, cancellationToken);
    }

    public async Task<QrCodeDto> ScanAsync(string code, string deviceId, CancellationToken cancellationToken = default)
    {
        var qrCode = await FindActiveCodeAsync(code, cancellationToken);
        var normalizedDeviceId = string.IsNullOrWhiteSpace(deviceId) ? "anonymous-device" : deviceId.Trim();

        if (string.Equals(qrCode.TargetType, "poi", StringComparison.OrdinalIgnoreCase))
        {
            var cooldownThresholdUtc = DateTime.UtcNow.Subtract(DuplicateQrVisitCooldown);
            var alreadyTrackedRecently = await dbContext.VisitEvents
                .AsNoTracking()
                .AnyAsync(item =>
                    item.PoiId == qrCode.TargetId
                    && item.EventType == EventType.QrScan
                    && item.DeviceId == normalizedDeviceId
                    && item.CreatedAt >= cooldownThresholdUtc,
                    cancellationToken);

            if (!alreadyTrackedRecently)
            {
                dbContext.VisitEvents.Add(new VisitEvent
                {
                    DeviceId = normalizedDeviceId,
                    PoiId = qrCode.TargetId,
                    EventType = EventType.QrScan,
                    Source = "qr",
                    ListenDurationSeconds = 0,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildDtoAsync(qrCode, cancellationToken);
    }

    public async Task DeleteAsync(int qrId, CancellationToken cancellationToken = default)
    {
        var qrCode = await dbContext.QrCodes.SingleOrDefaultAsync(item => item.Id == qrId, cancellationToken)
            ?? throw new KeyNotFoundException("QR code was not found.");

        dbContext.QrCodes.Remove(qrCode);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<QrCode> FindActiveCodeAsync(string code, CancellationToken cancellationToken)
    {
        var qrCode = await dbContext.QrCodes
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Code == code, cancellationToken)
            ?? throw new KeyNotFoundException("QR code was not found.");

        if (qrCode.ExpiresAt is not null && qrCode.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("QR code has expired.");
        }

        return qrCode;
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var code = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
            var exists = await dbContext.QrCodes.AnyAsync(item => item.Code == code, cancellationToken);
            if (!exists)
            {
                return code;
            }
        }
    }

    private async Task<int> ValidateTargetAsync(string targetType, int targetId, CancellationToken cancellationToken)
    {
        switch (targetType)
        {
            case "open_app":
                return 0;
            case "poi":
                _ = await dbContext.Pois.AsNoTracking().SingleOrDefaultAsync(item => item.Id == targetId, cancellationToken)
                    ?? throw new KeyNotFoundException("POI target was not found.");
                return targetId;
            default:
                throw new ArgumentException("Unsupported QR target type.", nameof(targetType));
        }
    }

    private static string NormalizeTargetType(string targetType)
    {
        var normalized = targetType.Trim().ToLowerInvariant();
        return normalized switch
        {
            "open_app" => normalized,
            "poi" => normalized,
            _ => throw new ArgumentException("Unsupported QR target type.", nameof(targetType))
        };
    }

    private async Task<QrCodeDto> BuildDtoAsync(QrCode qrCode, CancellationToken cancellationToken)
    {
        if (!string.Equals(qrCode.TargetType, "poi", StringComparison.OrdinalIgnoreCase))
        {
            return qrCode.ToDto();
        }

        var scanCount = await dbContext.VisitEvents
            .AsNoTracking()
            .CountAsync(item => item.PoiId == qrCode.TargetId && item.EventType == EventType.QrScan, cancellationToken);

        return CreateDto(qrCode, scanCount);
    }

    private static QrCodeDto ToDto(QrCode qrCode, IReadOnlyDictionary<int, int> qrScanCountsByPoiId)
    {
        var scanCount = string.Equals(qrCode.TargetType, "poi", StringComparison.OrdinalIgnoreCase)
            ? qrScanCountsByPoiId.TryGetValue(qrCode.TargetId, out var count) ? (int?)count : 0
            : null;

        return CreateDto(qrCode, scanCount);
    }

    private static QrCodeDto CreateDto(QrCode qrCode, int? scanCount)
    {
        var dto = qrCode.ToDto();
        return new QrCodeDto
        {
            Id = dto.Id,
            Code = dto.Code,
            TargetType = dto.TargetType,
            TargetId = dto.TargetId,
            LocationHint = dto.LocationHint,
            ExpiresAtUtc = dto.ExpiresAtUtc,
            ScanCount = scanCount,
            PublicUrl = dto.PublicUrl,
            AppDeepLink = dto.AppDeepLink
        };
    }
}
