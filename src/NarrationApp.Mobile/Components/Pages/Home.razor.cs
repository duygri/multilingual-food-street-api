using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NarrationApp.Mobile.Components.Pages.Sections;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home : IAsyncDisposable
{
    private readonly VisitorShellState _state = VisitorShellState.CreateRuntimeDefault();
    private readonly VisitorMapRenderState _mapRenderState = new();
    private DotNetObjectReference<Home>? _mapBridge;
    private DotNetObjectReference<Home>? _audioBridge;
    private string? _lastAutoPlayedPoiId;
    private bool _isAutoPlayingFromProximity;
    private bool _isContentLoading;
    private bool _isHandlingPendingDeepLink;
    private bool _pendingSelectedPoiAudioPreparationRequested;
    private bool _pendingSelectedPoiAutoPlay;
    private string? _discoverPoiDetailId;
    private string? _tourDetailId;
    private bool _isSearchOverlayOpen;
    private bool _isFullPlayerOpen;
    private bool _showFullPlayerTranscript;
    private bool _startupWorkQueued;
    private CancellationTokenSource? _foregroundLocationLoopCts;
    private Task? _foregroundLocationLoopTask;
    private int _audioSpeedIndex = 1;
    private string? _aboutStatusMessage;
    private static readonly double[] AudioSpeedOptions = [0.75d, 1d, 1.25d, 1.5d, 2d];

    private bool IsDiscoverPoiDetailVisible =>
        _state.CurrentTab == VisitorTab.Discover
        && !string.IsNullOrWhiteSpace(_discoverPoiDetailId)
        && string.Equals(_state.SelectedPoi?.Id, _discoverPoiDetailId, StringComparison.OrdinalIgnoreCase);

    private bool IsTourDetailVisible =>
        _state.CurrentTab == VisitorTab.Tours
        && !string.IsNullOrWhiteSpace(_tourDetailId)
        && string.Equals(_state.SelectedTour?.Id, _tourDetailId, StringComparison.OrdinalIgnoreCase);
}
