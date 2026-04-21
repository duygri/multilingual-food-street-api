namespace NarrationApp.Web.Services;

public interface IAudioRefreshPump
{
    IAsyncDisposable Start(Func<CancellationToken, Task> onTick, TimeSpan interval);
}
