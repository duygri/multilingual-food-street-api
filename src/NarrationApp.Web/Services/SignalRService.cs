using System.Net;
using Microsoft.AspNetCore.SignalR.Client;
using NarrationApp.Shared.DTOs.Notification;
using NarrationApp.SharedUI.Auth;

namespace NarrationApp.Web.Services;

public sealed class SignalRService(HttpClient httpClient, IAuthSessionStore sessionStore, CustomAuthStateProvider? authStateProvider = null) : INotificationRealtimeService, IAsyncDisposable
{
    private readonly SemaphoreSlim _sync = new(1, 1);
    private HubConnection? _connection;

    public event Action<NotificationDto>? NotificationReceived;

    public async ValueTask EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        var session = await sessionStore.GetAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(session?.Token) || httpClient.BaseAddress is null)
        {
            return;
        }

        HubConnection? failedConnection = null;
        HttpRequestException? unauthorizedException = null;

        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (_connection is not null && _connection.State is HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting)
            {
                return;
            }

            _connection ??= BuildConnection();

            if (_connection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await _connection.StartAsync(cancellationToken);
                }
                catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Unauthorized)
                {
                    failedConnection = _connection;
                    _connection = null;
                    unauthorizedException = exception;
                }
            }
        }
        finally
        {
            _sync.Release();
        }

        if (failedConnection is not null)
        {
            await failedConnection.DisposeAsync();
        }

        if (unauthorizedException is not null)
        {
            await InvalidateSessionAsync(cancellationToken);
            throw unauthorizedException;
        }
    }

    public async ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (_connection is null)
            {
                return;
            }

            if (_connection.State != HubConnectionState.Disconnected)
            {
                await _connection.StopAsync(cancellationToken);
            }

            await _connection.DisposeAsync();
            _connection = null;
        }
        finally
        {
            _sync.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _sync.Dispose();
    }

    private HubConnection BuildConnection()
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(httpClient.BaseAddress!, "hubs/notification"), options =>
            {
                options.AccessTokenProvider = async () => (await sessionStore.GetAsync())?.Token;
            })
            .WithAutomaticReconnect()
            .Build();

        connection.On<NotificationDto>("notificationReceived", notification =>
        {
            NotificationReceived?.Invoke(notification);
        });

        return connection;
    }

    private async Task InvalidateSessionAsync(CancellationToken cancellationToken)
    {
        if (authStateProvider is null || await sessionStore.GetAsync(cancellationToken) is null)
        {
            return;
        }

        await authStateProvider.MarkUserAsLoggedOutAsync();
    }
}
