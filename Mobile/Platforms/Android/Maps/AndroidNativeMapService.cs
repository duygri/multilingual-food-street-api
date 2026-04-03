using System;
using System.Threading.Tasks;
using System.Threading;
using Android.Content;
using FoodStreet.Client.DTOs;
using FoodStreet.Client.Services;
using Microsoft.Maui.ApplicationModel;

namespace FoodStreet.Mobile.Platforms.Android.Maps;

internal sealed class AndroidNativeMapService : IMobileNativeMapService
{
    private static readonly object PickerSync = new();
    private static TaskCompletionSource<MobileNativeMapResult?>? _pendingPickerRequest;

    public Task OpenBrowseMapAsync(MobileNativeMapRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return LaunchAsync(request, cancellationToken);
    }

    public async Task<MobileNativeMapResult?> OpenPickerAsync(MobileNativeMapRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var completion = new TaskCompletionSource<MobileNativeMapResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (PickerSync)
        {
            if (_pendingPickerRequest is not null)
            {
                throw new InvalidOperationException("A native Android picker request is already in progress.");
            }

            _pendingPickerRequest = completion;
        }

        using var _ = cancellationToken.Register(() => CompletePendingPicker(null));

        try
        {
            await LaunchAsync(request, cancellationToken);
            return await completion.Task;
        }
        catch
        {
            CompletePendingPicker(null);
            throw;
        }
    }

    internal static void CompletePendingPicker(MobileNativeMapResult? result)
    {
        TaskCompletionSource<MobileNativeMapResult?>? pending;
        lock (PickerSync)
        {
            pending = _pendingPickerRequest;
            _pendingPickerRequest = null;
        }

        pending?.TrySetResult(result);
    }

    private static Task LaunchAsync(MobileNativeMapRequest request, CancellationToken cancellationToken)
    {
        return MainThread.InvokeOnMainThreadAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var activity = Platform.CurrentActivity;
            var context = activity ?? Platform.AppContext;
            var intent = new Intent(context, typeof(NativeMapActivity));
            NativeMapContracts.WriteRequest(intent, request);

            if (activity is null)
            {
                intent.AddFlags(ActivityFlags.NewTask);
            }

            context.StartActivity(intent);
        });
    }
}
