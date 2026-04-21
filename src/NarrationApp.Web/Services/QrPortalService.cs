using NarrationApp.Shared.DTOs.QR;

namespace NarrationApp.Web.Services;

public sealed class QrPortalService(ApiClient apiClient) : IQrPortalService
{
    public Task<IReadOnlyList<QrCodeDto>> GetAsync(string? targetType = null, CancellationToken cancellationToken = default)
    {
        var query = "api/qr";
        if (!string.IsNullOrWhiteSpace(targetType))
        {
            query += $"?targetType={Uri.EscapeDataString(targetType)}";
        }

        return apiClient.GetAsync<IReadOnlyList<QrCodeDto>>(query, cancellationToken);
    }

    public Task<QrCodeDto> CreateAsync(CreateQrRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PostAsync<CreateQrRequest, QrCodeDto>("api/qr", request, cancellationToken);
    }

    public Task DeleteAsync(int qrId, CancellationToken cancellationToken = default)
    {
        return apiClient.DeleteAsync($"api/qr/{qrId}", cancellationToken);
    }
}
