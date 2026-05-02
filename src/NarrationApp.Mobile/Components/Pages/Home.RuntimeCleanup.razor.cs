using Microsoft.JSInterop;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private static async Task AwaitCanceledLoopAsync(Task? loopTask)
    {
        if (loopTask is null)
        {
            return;
        }

        try
        {
            await loopTask;
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
    }

    private async Task DisposeJsResourceAsync(string identifier, params object?[] args)
    {
        try
        {
            await JS.InvokeVoidAsync(identifier, args);
        }
        catch
        {
            // Best-effort cleanup while the page is tearing down.
        }
    }
}
