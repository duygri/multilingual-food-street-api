using NarrationApp.Shared.DTOs.QR;

namespace NarrationApp.Web.Services;

public interface IQrPortalService
{
    Task<IReadOnlyList<QrCodeDto>> GetAsync(string? targetType = null, CancellationToken cancellationToken = default);

    Task<QrCodeDto> CreateAsync(CreateQrRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int qrId, CancellationToken cancellationToken = default);
}
