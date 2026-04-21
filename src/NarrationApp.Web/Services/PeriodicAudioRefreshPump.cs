namespace NarrationApp.Web.Services;

public sealed class PeriodicAudioRefreshPump(TimeProvider timeProvider) : IAudioRefreshPump
{
    public IAsyncDisposable Start(Func<CancellationToken, Task> onTick, TimeSpan interval)
    {
        return new Subscription(onTick, interval, timeProvider);
    }

    private sealed class Subscription : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _loop;

        public Subscription(Func<CancellationToken, Task> onTick, TimeSpan interval, TimeProvider timeProvider)
        {
            _loop = RunAsync(onTick, interval, timeProvider, _cts.Token);
        }

        public async ValueTask DisposeAsync()
        {
            if (_cts.IsCancellationRequested)
            {
                return;
            }

            await _cts.CancelAsync();

            try
            {
                await _loop;
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _cts.Dispose();
            }
        }

        private static async Task RunAsync(
            Func<CancellationToken, Task> onTick,
            TimeSpan interval,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(interval, timeProvider);

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await onTick(cancellationToken);
            }
        }
    }
}
