using System.Net;
using NarrationApp.Shared.DTOs.Notification;
using NarrationApp.SharedUI.Auth;
using NarrationApp.SharedUI.Services;

namespace NarrationApp.Web.Services;

public sealed class NotificationCenterApiService : INotificationCenterService
{
    private readonly ApiClient _apiClient;
    private readonly IAuthSessionStore _sessionStore;
    private readonly INotificationRealtimeService _realtimeService;

    public NotificationCenterApiService(ApiClient apiClient, IAuthSessionStore sessionStore, INotificationRealtimeService realtimeService)
    {
        _apiClient = apiClient;
        _sessionStore = sessionStore;
        _realtimeService = realtimeService;
        _realtimeService.NotificationReceived += HandleRealtimeNotification;
    }

    public event Action? Changed;

    public async ValueTask<IReadOnlyList<NotificationDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        if (await _sessionStore.GetAsync(cancellationToken) is null)
        {
            await _realtimeService.DisconnectAsync(cancellationToken);
            return Array.Empty<NotificationDto>();
        }

        try
        {
            await _realtimeService.EnsureConnectedAsync(cancellationToken);
            if (await _sessionStore.GetAsync(cancellationToken) is null)
            {
                await _realtimeService.DisconnectAsync(cancellationToken);
                return Array.Empty<NotificationDto>();
            }

            return await _apiClient.GetAsync<IReadOnlyList<NotificationDto>>("api/notifications", cancellationToken);
        }
        catch (Exception exception) when (IsUnauthorized(exception))
        {
            await _realtimeService.DisconnectAsync(cancellationToken);
            return Array.Empty<NotificationDto>();
        }
    }

    public async ValueTask<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        if (await _sessionStore.GetAsync(cancellationToken) is null)
        {
            await _realtimeService.DisconnectAsync(cancellationToken);
            return 0;
        }

        try
        {
            await _realtimeService.EnsureConnectedAsync(cancellationToken);
            if (await _sessionStore.GetAsync(cancellationToken) is null)
            {
                await _realtimeService.DisconnectAsync(cancellationToken);
                return 0;
            }

            var response = await _apiClient.GetAsync<UnreadCountDto>("api/notifications/unread-count", cancellationToken);
            return response.Count;
        }
        catch (Exception exception) when (IsUnauthorized(exception))
        {
            await _realtimeService.DisconnectAsync(cancellationToken);
            return 0;
        }
    }

    public async ValueTask MarkAllReadAsync(CancellationToken cancellationToken = default)
    {
        await _apiClient.PutAsync("api/notifications/read-all", cancellationToken);
        Changed?.Invoke();
    }

    public async ValueTask MarkReadAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        await _apiClient.PutAsync($"api/notifications/{notificationId}/read", cancellationToken);
        Changed?.Invoke();
    }

    private void HandleRealtimeNotification(NotificationDto notification)
    {
        Changed?.Invoke();
    }

    private static bool IsUnauthorized(Exception exception)
    {
        return exception is ApiException { StatusCode: HttpStatusCode.Unauthorized }
            or HttpRequestException { StatusCode: HttpStatusCode.Unauthorized };
    }
}
