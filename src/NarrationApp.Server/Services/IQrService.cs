using NarrationApp.Shared.DTOs.QR;

namespace NarrationApp.Server.Services;

public interface IQrService
{
    Task<QrCodeDto> CreateAsync(CreateQrRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QrCodeDto>> GetAsync(string? targetType = null, CancellationToken cancellationToken = default);

    Task<QrCodeDto> ResolveAsync(string code, CancellationToken cancellationToken = default);

    Task<QrCodeDto> ScanAsync(string code, string deviceId, CancellationToken cancellationToken = default);

    Task DeleteAsync(int qrId, CancellationToken cancellationToken = default);
}
