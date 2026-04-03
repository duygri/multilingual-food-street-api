using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using FoodStreet.Client.DTOs;
using Xamarin.GoogleAndroid.Libraries.Places.Api;
using Xamarin.GoogleAndroid.Libraries.Places.Api.Model;
using Xamarin.GoogleAndroid.Libraries.Places.Widget;
using Xamarin.GoogleAndroid.Libraries.Places.Widget.Model;
using AndroidButton = Android.Widget.Button;
using AndroidImageButton = Android.Widget.ImageButton;
using AndroidLinearLayout = Android.Widget.LinearLayout;
using AndroidTextView = Android.Widget.TextView;
using AndroidView = Android.Views.View;

namespace FoodStreet.Mobile.Platforms.Android.Maps;

[Activity(
    Exported = false,
    Theme = "@style/Maui.MainTheme.NoActionBar",
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
internal class NativeMapActivity : AppCompatActivity, IOnMapReadyCallback
{
    private const int AutocompleteRequestCode = 22041;
    private GoogleMap? _map;
    private MobileNativeMapRequest _request = new();
    private AndroidTextView? _titleView;
    private AndroidLinearLayout? _pickerActions;
    private AndroidButton? _confirmButton;
    private readonly Dictionary<string, MobileNativeMapPoiMarker> _markerLookup = new(StringComparer.Ordinal);
    private MobileNativeMapPoiMarker? _focusedPoi;
    private LatLng? _userPosition;
    private Marker? _pickerMarker;
    private LatLng? _selectedPickerPosition;
    private bool _pickerCompleted;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_native_map);

        _request = NativeMapContracts.ReadRequest(Intent) ?? new MobileNativeMapRequest();
        _titleView = FindViewById<AndroidTextView>(Resource.Id.native_map_title);
        _pickerActions = FindViewById<AndroidLinearLayout>(Resource.Id.native_map_picker_actions);
        _confirmButton = FindViewById<AndroidButton>(Resource.Id.native_map_confirm_button);

        if (_titleView is not null)
        {
            _titleView.Text = string.IsNullOrWhiteSpace(_request.ScreenTitle)
                ? (_request.Mode == MobileNativeMapMode.Picker ? "Chọn vị trí POI" : "Bản đồ POI")
                : _request.ScreenTitle;
        }

        if (_pickerActions is not null)
        {
            _pickerActions.Visibility = _request.Mode == MobileNativeMapMode.Picker
                ? ViewStates.Visible
                : ViewStates.Gone;
        }

        if (_confirmButton is not null)
        {
            _confirmButton.Enabled = false;
            _confirmButton.SetOnClickListener(new ActionClickListener(ConfirmPickerSelection));
        }

        FindViewById<AndroidImageButton>(Resource.Id.native_map_close_button)?.SetOnClickListener(
            new ActionClickListener(ClosePickerOrFinish));

        FindViewById<AndroidImageButton>(Resource.Id.native_map_recenter_button)?.SetOnClickListener(
            new ActionClickListener(RecenterMap));

        FindViewById<AndroidButton>(Resource.Id.native_map_cancel_button)?.SetOnClickListener(
            new ActionClickListener(ClosePickerOrFinish));

        FindViewById<AndroidButton>(Resource.Id.native_map_search_button)?.SetOnClickListener(
            new ActionClickListener(OpenAutocompleteSearch));

        var mapFragment = SupportFragmentManager.FindFragmentById(Resource.Id.native_map_fragment_host) as SupportMapFragment;
        if (mapFragment is null)
        {
            mapFragment = SupportMapFragment.NewInstance();
            SupportFragmentManager
                .BeginTransaction()
                .Replace(Resource.Id.native_map_fragment_host, mapFragment)
                .CommitNow();
        }

        mapFragment.GetMapAsync(this);
    }

    public void OnMapReady(GoogleMap googleMap)
    {
        _map = googleMap;
        _map.UiSettings.ZoomControlsEnabled = true;
        _map.UiSettings.CompassEnabled = true;
        _map.UiSettings.MyLocationButtonEnabled = false;
        _map.MapType = GoogleMap.MapTypeNormal;
        _map.InfoWindowClick += HandleInfoWindowClick;

        if (_request.Mode == MobileNativeMapMode.Picker)
        {
            _map.MapClick += HandlePickerMapClick;
            _map.MarkerDragEnd += HandlePickerMarkerDragEnd;
        }

        RenderMapContent();
    }

    private void RenderMapContent()
    {
        if (_map is null)
        {
            return;
        }

        _map.Clear();
        _markerLookup.Clear();
        _focusedPoi = null;
        _userPosition = null;
        _pickerMarker = null;
        _selectedPickerPosition = null;

        var center = new LatLng(_request.CenterLatitude, _request.CenterLongitude);
        _map.MoveCamera(CameraUpdateFactory.NewLatLngZoom(center, _request.Zoom <= 0 ? 15f : _request.Zoom));

        if (_request.HasUserLocation && _request.UserLatitude.HasValue && _request.UserLongitude.HasValue)
        {
            var userPosition = new LatLng(_request.UserLatitude.Value, _request.UserLongitude.Value);
            _userPosition = userPosition;
            _map.AddMarker(new MarkerOptions()
                .SetPosition(userPosition)
                .SetTitle("Vị trí của bạn")
                .SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueAzure)));
        }

        foreach (var poi in _request.Pois)
        {
            var markerOptions = new MarkerOptions()
                .SetPosition(new LatLng(poi.Latitude, poi.Longitude))
                .SetTitle(poi.Name)
                .SetSnippet(poi.Address ?? poi.Description ?? string.Empty);

            if (poi.Id == _request.FocusedPoiId)
            {
                markerOptions.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueOrange));
            }

            var marker = _map.AddMarker(markerOptions);

            if (marker is null)
            {
                continue;
            }

            _markerLookup[marker.Id] = poi;

            if (poi.Id == _request.FocusedPoiId)
            {
                _focusedPoi = poi;
                marker.ShowInfoWindow();
            }

            if (_request.Mode == MobileNativeMapMode.Browse && poi.RadiusMeters is > 0)
            {
                AddGeofenceCircle(poi);
            }
        }

        if (_request.Mode == MobileNativeMapMode.Picker)
        {
            SetPickerSelection(center, moveCamera: false);
            _map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(center, Math.Max(_request.Zoom, 17f)));
            return;
        }

        if (_focusedPoi is not null)
        {
            var focusedPosition = new LatLng(_focusedPoi.Latitude, _focusedPoi.Longitude);
            _map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(focusedPosition, Math.Max(_request.Zoom, 16f)));
            return;
        }

        FitCameraToMarkers();
    }

    private void FitCameraToMarkers()
    {
        if (_map is null)
        {
            return;
        }

        var boundsBuilder = new LatLngBounds.Builder();
        var hasPoints = false;

        if (_request.HasUserLocation && _request.UserLatitude.HasValue && _request.UserLongitude.HasValue)
        {
            boundsBuilder.Include(new LatLng(_request.UserLatitude.Value, _request.UserLongitude.Value));
            hasPoints = true;
        }

        foreach (var poi in _request.Pois)
        {
            boundsBuilder.Include(new LatLng(poi.Latitude, poi.Longitude));
            hasPoints = true;
        }

        if (!hasPoints)
        {
            return;
        }

        _map.AnimateCamera(CameraUpdateFactory.NewLatLngBounds(boundsBuilder.Build(), 120));
    }

    private void RecenterMap()
    {
        if (_map is null)
        {
            return;
        }

        if (_request.Mode == MobileNativeMapMode.Picker && _selectedPickerPosition is not null)
        {
            _map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(_selectedPickerPosition, Math.Max(_request.Zoom, 17f)));
            return;
        }

        if (_focusedPoi is not null)
        {
            _map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(
                new LatLng(_focusedPoi.Latitude, _focusedPoi.Longitude),
                Math.Max(_request.Zoom, 16f)));
            return;
        }

        if (_userPosition is not null)
        {
            _map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(_userPosition, Math.Max(_request.Zoom, 16f)));
            return;
        }

        FitCameraToMarkers();
    }

    private void HandleInfoWindowClick(object? sender, GoogleMap.InfoWindowClickEventArgs e)
    {
        if (_request.Mode != MobileNativeMapMode.Browse || e.Marker is null)
        {
            return;
        }

        if (!_markerLookup.TryGetValue(e.Marker.Id, out var poi))
        {
            return;
        }

        var directionsUrl = $"https://www.google.com/maps/dir/?api=1&destination={poi.Latitude},{poi.Longitude}&travelmode=walking&dir_action=navigate";
        var intent = new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(directionsUrl));
        StartActivity(intent);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        if (requestCode != AutocompleteRequestCode || _request.Mode != MobileNativeMapMode.Picker)
        {
            return;
        }

        if (resultCode == Result.Ok && data is not null)
        {
#pragma warning disable CS0618
            // The current Xamarin Places binding still routes picker intents through the deprecated widget API.
            var place = Autocomplete.GetPlaceFromIntent(data);
#pragma warning restore CS0618
            if (place?.Location is not null)
            {
                SetPickerSelection(place.Location, moveCamera: true);
            }
        }
    }

    private void HandlePickerMapClick(object? sender, GoogleMap.MapClickEventArgs e)
    {
        SetPickerSelection(e.Point, moveCamera: false);
    }

    private void HandlePickerMarkerDragEnd(object? sender, GoogleMap.MarkerDragEndEventArgs e)
    {
        if (_request.Mode != MobileNativeMapMode.Picker || e.Marker is null)
        {
            return;
        }

        SetPickerSelection(e.Marker.Position, moveCamera: false);
    }

    private void SetPickerSelection(LatLng position, bool moveCamera)
    {
        if (_map is null || _request.Mode != MobileNativeMapMode.Picker)
        {
            return;
        }

        _selectedPickerPosition = position;

        if (_pickerMarker is null)
        {
            _pickerMarker = _map.AddMarker(new MarkerOptions()
                .SetPosition(position)
                .SetTitle("Vị trí đã chọn")
                .Draggable(true)
                .SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueRed)));
        }
        else
        {
            _pickerMarker.Position = position;
        }

        if (_confirmButton is not null)
        {
            _confirmButton.Enabled = true;
        }

        if (moveCamera)
        {
            _map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(position, Math.Max(_request.Zoom, 17f)));
        }
    }

    private void OpenAutocompleteSearch()
    {
        if (_request.Mode != MobileNativeMapMode.Picker)
        {
            return;
        }

        var apiKey = ResolveGoogleMapsApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Toast.MakeText(this, "Chưa cấu hình Android Maps key cho Places.", ToastLength.Short)?.Show();
            return;
        }

#pragma warning disable CS0618
        // The current binding package exposes Places autocomplete via the deprecated widget types.
        if (!Places.IsInitialized)
        {
            Places.Initialize(ApplicationContext!, apiKey);
        }

        var fields = new List<Place.Field>
        {
            Place.Field.Id,
            Place.Field.DisplayName,
            Place.Field.FormattedAddress,
            Place.Field.Location
        };

        var intent = new Autocomplete.IntentBuilder(AutocompleteActivityMode.Fullscreen, fields)
            .Build(this);
#pragma warning restore CS0618
        StartActivityForResult(intent, AutocompleteRequestCode);
    }

    private string? ResolveGoogleMapsApiKey()
    {
        try
        {
            var applicationInfo = PackageManager?.GetApplicationInfo(PackageName ?? string.Empty, PackageInfoFlags.MetaData);
            return applicationInfo?.MetaData?.GetString("com.google.android.geo.API_KEY");
        }
        catch
        {
            return null;
        }
    }

    private void AddGeofenceCircle(MobileNativeMapPoiMarker poi)
    {
        if (_map is null || poi.RadiusMeters is not > 0)
        {
            return;
        }

        var isFocused = poi.Id == _request.FocusedPoiId;
        var strokeColor = isFocused ? unchecked((int)0xFFE67E22) : unchecked((int)0xFF3B82F6);
        var fillColor = isFocused ? unchecked((int)0x33F97316) : unchecked((int)0x223B82F6);

        _map.AddCircle(new CircleOptions()
            .InvokeCenter(new LatLng(poi.Latitude, poi.Longitude))
            .InvokeRadius(poi.RadiusMeters.Value)
            .InvokeStrokeWidth(isFocused ? 4f : 2f)
            .InvokeStrokeColor(strokeColor)
            .InvokeFillColor(fillColor));
    }

    private void ConfirmPickerSelection()
    {
        if (_request.Mode != MobileNativeMapMode.Picker || _selectedPickerPosition is null)
        {
            return;
        }

        _pickerCompleted = true;
        AndroidNativeMapService.CompletePendingPicker(new MobileNativeMapResult
        {
            Confirmed = true,
            SelectedLatitude = _selectedPickerPosition.Latitude,
            SelectedLongitude = _selectedPickerPosition.Longitude,
            SelectedLabel = $"Lat {_selectedPickerPosition.Latitude:F6}, Lng {_selectedPickerPosition.Longitude:F6}"
        });

        Finish();
    }

    private void ClosePickerOrFinish()
    {
        if (_request.Mode == MobileNativeMapMode.Picker && !_pickerCompleted)
        {
            AndroidNativeMapService.CompletePendingPicker(null);
        }

        Finish();
    }

    protected override void OnDestroy()
    {
        if (_request.Mode == MobileNativeMapMode.Picker && !_pickerCompleted)
        {
            AndroidNativeMapService.CompletePendingPicker(null);
        }

        base.OnDestroy();
    }

    private sealed class ActionClickListener : Java.Lang.Object, AndroidView.IOnClickListener
    {
        private readonly Action _action;

        public ActionClickListener(Action action)
        {
            _action = action;
        }

        public void OnClick(AndroidView? v)
        {
            _action();
        }
    }
}
