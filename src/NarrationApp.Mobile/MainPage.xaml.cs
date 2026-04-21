#if ANDROID
using AndroidWebView = Android.Webkit.WebView;
using Microsoft.Maui.Handlers;
#endif

namespace NarrationApp.Mobile;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

#if ANDROID
        blazorWebView.HandlerChanged += HandleBlazorWebViewHandlerChanged;
#endif
    }

#if ANDROID
    private void HandleBlazorWebViewHandlerChanged(object? sender, EventArgs e)
    {
        if (blazorWebView.Handler is IPlatformViewHandler platformViewHandler
            && platformViewHandler.PlatformView is AndroidWebView webView)
        {
            webView.Settings.MediaPlaybackRequiresUserGesture = false;
        }
    }
#endif
}
