using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.DTOs.QR;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed class QrService(AppDbContext dbContext) : IQrService
{
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

        return qrCode.ToDto();
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

        return items.Select(item => item.ToDto()).ToArray();
    }

    public async Task<QrCodeDto> ResolveAsync(string code, CancellationToken cancellationToken = default)
    {
        var qrCode = await FindActiveCodeAsync(code, cancellationToken);
        return qrCode.ToDto();
    }

    public async Task<QrCodeDto> ScanAsync(string code, string deviceId, CancellationToken cancellationToken = default)
    {
        var qrCode = await FindActiveCodeAsync(code, cancellationToken);

        if (string.Equals(qrCode.TargetType, "poi", StringComparison.OrdinalIgnoreCase))
        {
            dbContext.VisitEvents.Add(new VisitEvent
            {
                DeviceId = deviceId,
                PoiId = qrCode.TargetId,
                EventType = EventType.QrScan,
                Source = "qr",
                ListenDurationSeconds = 0,
                CreatedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return qrCode.ToDto();
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
            case "tour":
                _ = await dbContext.Tours.AsNoTracking().SingleOrDefaultAsync(item => item.Id == targetId, cancellationToken)
                    ?? throw new KeyNotFoundException("Tour target was not found.");
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
            "tour" => normalized,
            _ => throw new ArgumentException("Unsupported QR target type.", nameof(targetType))
        };
    }
}
