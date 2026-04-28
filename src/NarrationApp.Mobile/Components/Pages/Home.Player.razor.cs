using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NarrationApp.Mobile.Components.Pages.Sections;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private void OpenFullPlayer()
    {
        if (_state.SelectedPoi is null || _state.CurrentAudioCue is null)
        {
            return;
        }

        _isQrModalOpen = false;
        _isSearchOverlayOpen = false;
        _isFullPlayerOpen = true;
    }

    private void CloseFullPlayer()
    {
        _isFullPlayerOpen = false;
        _showFullPlayerTranscript = false;
    }

    private void ToggleFullPlayerTranscript()
    {
        _showFullPlayerTranscript = !_showFullPlayerTranscript;
    }
}
