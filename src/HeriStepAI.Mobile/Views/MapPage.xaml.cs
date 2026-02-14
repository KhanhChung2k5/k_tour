using HeriStepAI.Mobile.Models;
using HeriStepAI.Mobile.ViewModels;
using HeriStepAI.Mobile.Helpers;
using System.Globalization;
using System.Text;

namespace HeriStepAI.Mobile.Views;

public partial class MapPage : ContentPage
{
    private readonly MapPageViewModel _viewModel;

    public MapPage() : this(GetViewModel()) { }

    private bool _mapLoaded;
    private bool _isBottomSheetExpanded = true;
    private double _bottomSheetPanStartY;

    public MapPage(MapPageViewModel viewModel)
    {
        try
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;

            // Apply responsive padding to top bar
            TopBar.Padding = ResponsiveHelper.HeaderPadding();

            // Configure WebView for map display
            ConfigureWebView();

            // Subscribe to POI selection from map
            MapWebView.Navigating += OnMapNavigating;

            // Subscribe to map update requests
            _viewModel.MapNeedsUpdate += OnMapNeedsUpdate;

            // Subscribe to real-time location updates during test mode
            _viewModel.SimulatedLocationChanged += OnSimulatedLocationChanged;

            // Subscribe to geofence triggers for map highlighting
            _viewModel.GeofenceTriggered += OnGeofenceTriggered;

            // Auto-expand bottom sheet when a POI is selected
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in MapPage constructor: {ex}");
            _viewModel = viewModel ?? throw new InvalidOperationException("ViewModel cannot be null");
        }
    }

    static MapPageViewModel GetViewModel() =>
        App.Services?.GetService<MapPageViewModel>()
        ?? throw new InvalidOperationException("MapPageViewModel not found. Check DI.");

    private void ConfigureWebView()
    {
#if ANDROID
        MapWebView.HandlerChanged += (s, e) =>
        {
            if (MapWebView.Handler?.PlatformView is Android.Webkit.WebView androidWebView)
            {
                androidWebView.Settings.JavaScriptEnabled = true;
                androidWebView.Settings.DomStorageEnabled = true;
                androidWebView.Settings.AllowFileAccess = true;
                androidWebView.Settings.AllowContentAccess = true;
                androidWebView.Settings.MixedContentMode = Android.Webkit.MixedContentHandling.AlwaysAllow;
                androidWebView.Settings.SetGeolocationEnabled(true);
                // User-Agent giống Chrome để tránh OSM chặn WebView
                androidWebView.Settings.UserAgentString = "Mozilla/5.0 (Linux; Android 10) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36";
                androidWebView.Settings.CacheMode = Android.Webkit.CacheModes.Normal;

                // Enable remote debugging for troubleshooting
                Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);

                // Set custom WebViewClient to prevent external navigation and handle poi:// scheme
                androidWebView.SetWebViewClient(new MapWebViewClient(poiId =>
                {
                    if (int.TryParse(poiId, out var id))
                    {
                        var poi = _viewModel.POIs.FirstOrDefault(p => p.Id == id);
                        if (poi != null)
                            _viewModel.POISelectedCommand.ExecuteAsync(poi);
                    }
                }));

                // Set custom WebChromeClient to capture JavaScript console messages
                androidWebView.SetWebChromeClient(new MapWebChromeClient());

                System.Diagnostics.Debug.WriteLine("[MapPage] ✅ WebView configured for Android");
            }
        };
#endif
    }

#if ANDROID
    /// <summary>
    /// Custom WebChromeClient - captures JavaScript console messages for debugging
    /// </summary>
    private class MapWebChromeClient : Android.Webkit.WebChromeClient
    {
        public override bool OnConsoleMessage(Android.Webkit.ConsoleMessage? consoleMessage)
        {
            if (consoleMessage != null)
            {
                var messageLevel = consoleMessage.InvokeMessageLevel();
                string level;

                if (messageLevel == Android.Webkit.ConsoleMessage.MessageLevel.Error)
                    level = "❌ ERROR";
                else if (messageLevel == Android.Webkit.ConsoleMessage.MessageLevel.Warning)
                    level = "⚠️ WARN";
                else if (messageLevel == Android.Webkit.ConsoleMessage.MessageLevel.Log)
                    level = "ℹ️ LOG";
                else
                    level = "📝 DEBUG";

                System.Diagnostics.Debug.WriteLine($"[JS Console] {level}: {consoleMessage.Message()} at {consoleMessage.SourceId()}:{consoleMessage.LineNumber()}");
            }
            return true;
        }
    }

    /// <summary>
    /// Custom WebViewClient - chỉ chặn main-frame navigation ra ngoài, CHO PHÉP load script/tile
    /// </summary>
    private class MapWebViewClient : Android.Webkit.WebViewClient
    {
        private readonly Action<string>? _onPoiSelected;

        public MapWebViewClient(Action<string>? onPoiSelected = null)
        {
            _onPoiSelected = onPoiSelected;
        }

        public override bool ShouldOverrideUrlLoading(Android.Webkit.WebView? view, Android.Webkit.IWebResourceRequest? request)
        {
            if (request == null) return false;
            var url = request.Url?.ToString() ?? "";

            if (url.StartsWith("poi://"))
            {
                var poiId = url.Replace("poi://", "");
                System.Diagnostics.Debug.WriteLine($"[MapWebViewClient] POI selected: {poiId}");
                MainThread.BeginInvokeOnMainThread(() => _onPoiSelected?.Invoke(poiId));
                return true; // Intercept - don't let WebView navigate to poi://
            }

            // Chỉ chặn khi là main-frame (user click link) - CHO PHÉP script, CSS, tile load
            if (request.IsForMainFrame && (url.StartsWith("http://") || url.StartsWith("https://")))
            {
                System.Diagnostics.Debug.WriteLine($"[MapWebViewClient] Blocked main-frame nav to: {url}");
                return true;
            }
            return false;
        }

        public override void OnPageFinished(Android.Webkit.WebView? view, string? url)
        {
            base.OnPageFinished(view, url);
            System.Diagnostics.Debug.WriteLine($"[MapWebViewClient] ✅ Page loaded: {url}");
        }

        public override void OnReceivedError(Android.Webkit.WebView? view, Android.Webkit.IWebResourceRequest? request, Android.Webkit.WebResourceError? error)
        {
            base.OnReceivedError(view, request, error);
            System.Diagnostics.Debug.WriteLine($"[MapWebViewClient] ❌ Error loading resource: {request?.Url} - {error?.Description}");
        }
    }
#endif

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!_mapLoaded)
        {
            _mapLoaded = true;
            LoadMapAsync();
        }
    }

    private void OnMapNeedsUpdate(object? sender, EventArgs e)
    {
        LoadMapAsync();
    }

    /// <summary>
    /// Move the current location marker on the map without full reload.
    /// Uses JavaScript evaluation to update the Leaflet marker position.
    /// </summary>
    private void OnSimulatedLocationChanged(object? sender, Location location)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var js = $"if(typeof currentMarker !== 'undefined') {{ currentMarker.setLatLng([{C(location.Latitude)}, {C(location.Longitude)}]); map.panTo([{C(location.Latitude)}, {C(location.Longitude)}], {{animate: true, duration: 0.5}}); }}";
                await MapWebView.EvaluateJavaScriptAsync(js);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating map marker: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Highlight a POI on the map when geofence is triggered.
    /// Shows a pulse effect and opens the popup.
    /// </summary>
    private void OnGeofenceTriggered(object? sender, POI poi)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // Open the POI popup and add a highlight circle
                var js = $@"
                    if(typeof marker{poi.Id} !== 'undefined') {{
                        marker{poi.Id}.openPopup();
                        if(typeof geoCircle !== 'undefined') {{ map.removeLayer(geoCircle); }}
                        geoCircle = L.circle([{C(poi.Latitude)}, {C(poi.Longitude)}], {{
                            radius: {C(poi.Radius)},
                            color: '#4CAF50',
                            fillColor: '#4CAF50',
                            fillOpacity: 0.15,
                            weight: 2
                        }}).addTo(map);
                    }}";
                await MapWebView.EvaluateJavaScriptAsync(js);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error highlighting POI: {ex.Message}");
            }
        });
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MapPageViewModel.HasSelectedPOI) && _viewModel.HasSelectedPOI)
            ExpandBottomSheet();
    }

    private void OnBottomSheetPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _bottomSheetPanStartY = BottomSheet.TranslationY;
                break;
            case GestureStatus.Running:
                var newY = _bottomSheetPanStartY + e.TotalY;
                BottomSheet.TranslationY = Math.Max(0, newY);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (BottomSheet.TranslationY > BottomSheet.Height * 0.3)
                    CollapseBottomSheet();
                else
                    ExpandBottomSheet();
                break;
        }
    }

    private void OnHandleBarTapped(object? sender, TappedEventArgs e)
    {
        if (_isBottomSheetExpanded)
            CollapseBottomSheet();
        else
            ExpandBottomSheet();
    }

    private async void CollapseBottomSheet()
    {
        _isBottomSheetExpanded = false;
        var target = BottomSheet.Height - 30;
        await BottomSheet.TranslateTo(0, target, 250, Easing.CubicOut);
    }

    private async void ExpandBottomSheet()
    {
        _isBottomSheetExpanded = true;
        await BottomSheet.TranslateTo(0, 0, 250, Easing.CubicOut);
    }

    private async void LoadMapAsync()
    {
        await Task.Delay(1000); // Wait for WebView handler and POIs to load

        System.Diagnostics.Debug.WriteLine($"[MapPage] 🗺️ Generating map HTML for {_viewModel.POIs.Count} POIs");
        var html = GenerateMapHtml(_viewModel.POIs, _viewModel.CurrentLocation);
        System.Diagnostics.Debug.WriteLine($"[MapPage] ✅ HTML generated ({html.Length} chars)");

        // Log first 500 chars of HTML for debugging
        var preview = html.Length > 500 ? html.Substring(0, 500) + "..." : html;
        System.Diagnostics.Debug.WriteLine($"[MapPage] HTML Preview: {preview}");

#if ANDROID
        // Use loadDataWithBaseURL to give the HTML an HTTPS origin.
        // This avoids ERR_ACCESS_DENIED (file:// approach) and null-origin
        // cross-origin blocks (HtmlWebViewSource approach) when loading map tiles.
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (MapWebView.Handler is Microsoft.Maui.Handlers.WebViewHandler handler
                    && handler.PlatformView is Android.Webkit.WebView webView)
                {
                    System.Diagnostics.Debug.WriteLine("[MapPage] ✅ Loading map HTML with BaseURL");
                    webView.LoadDataWithBaseURL(
                        "https://heristepai.app/",
                        html,
                        "text/html",
                        "UTF-8",
                        null);
                }
                else
                {
                    // Fallback if handler not ready yet - wait and retry
                    System.Diagnostics.Debug.WriteLine("[MapPage] ⚠️ Handler not ready, retrying in 500ms...");
                    Task.Delay(500).ContinueWith(_ => LoadMapAsync());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapPage] ❌ Error loading map: {ex.Message}");
                MapWebView.Source = new HtmlWebViewSource { Html = html };
            }
        });
#else
        MapWebView.Source = new HtmlWebViewSource { Html = html };
#endif
    }

    private void OnMapNavigating(object? sender, WebNavigatingEventArgs e)
    {
        // Handle POI click from map
        if (e.Url.StartsWith("poi://"))
        {
            e.Cancel = true;
            var poiId = int.Parse(e.Url.Replace("poi://", ""));
            var poi = _viewModel.POIs.FirstOrDefault(p => p.Id == poiId);
            if (poi != null)
            {
                _viewModel.POISelectedCommand.ExecuteAsync(poi);
            }
        }
    }

    private string GenerateMapHtml(List<POI> pois, Microsoft.Maui.Devices.Sensors.Location? currentLocation)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset='UTF-8'>");
        sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'>");
        sb.AppendLine("<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />");
        sb.AppendLine("<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>");
        sb.AppendLine("<style>");
        sb.AppendLine("* { margin: 0; padding: 0; box-sizing: border-box; }");
        sb.AppendLine("html, body { margin: 0; padding: 0; overflow: hidden; width: 100%; height: 100%; }");
        sb.AppendLine("#map { width: 100%; height: 100%; min-height: 300px; position: absolute; top:0; left:0; right:0; bottom:0; background: #e0e0e0; }");
        sb.AppendLine(@"
            .poi-marker {
                background: linear-gradient(135deg, #E07B4C 0%, #C96A3E 100%);
                width: 36px;
                height: 36px;
                border-radius: 50% 50% 50% 0;
                transform: rotate(-45deg);
                border: 3px solid white;
                box-shadow: 0 3px 10px rgba(0,0,0,0.3);
                display: flex;
                align-items: center;
                justify-content: center;
            }
            .poi-marker-inner {
                transform: rotate(45deg);
                font-size: 16px;
            }
            .nearest-marker {
                background: linear-gradient(135deg, #4CAF50 0%, #388E3C 100%);
                width: 44px;
                height: 44px;
                border-radius: 50% 50% 50% 0;
                transform: rotate(-45deg);
                border: 4px solid #FFD700;
                box-shadow: 0 0 20px rgba(76, 175, 80, 0.6);
                animation: pulse 2s infinite;
            }
            @keyframes pulse {
                0% { box-shadow: 0 0 10px rgba(76, 175, 80, 0.6); }
                50% { box-shadow: 0 0 25px rgba(76, 175, 80, 0.8); }
                100% { box-shadow: 0 0 10px rgba(76, 175, 80, 0.6); }
            }
            .current-location {
                background: #2196F3;
                width: 20px;
                height: 20px;
                border-radius: 50%;
                border: 4px solid white;
                box-shadow: 0 0 15px rgba(33, 150, 243, 0.7);
            }
            .popup-content {
                padding: 10px;
                min-width: 160px;
                max-width: 220px;
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            }
            .popup-title {
                font-size: 16px;
                font-weight: 700;
                color: #212121;
                margin-bottom: 6px;
            }
            .popup-address {
                font-size: 12px;
                color: #757575;
                margin-bottom: 8px;
                display: flex;
                align-items: center;
                gap: 4px;
            }
            .popup-desc {
                font-size: 12px;
                color: #424242;
                margin-bottom: 8px;
                line-height: 1.3;
                max-height: 52px;
                overflow: hidden;
                display: -webkit-box;
                -webkit-line-clamp: 3;
                -webkit-box-orient: vertical;
            }
            .popup-meta {
                display: flex;
                gap: 12px;
                margin-bottom: 12px;
                font-size: 12px;
                color: #757575;
            }
            .popup-btn {
                width: 100%;
                padding: 12px;
                background: linear-gradient(135deg, #E07B4C 0%, #C96A3E 100%);
                color: white;
                border: none;
                border-radius: 25px;
                font-size: 14px;
                font-weight: 600;
                cursor: pointer;
                display: flex;
                align-items: center;
                justify-content: center;
                gap: 6px;
            }
            .popup-btn:active {
                transform: scale(0.98);
            }
            .leaflet-popup-content-wrapper {
                border-radius: 12px;
                box-shadow: 0 4px 20px rgba(0,0,0,0.15);
            }
            .leaflet-popup-content {
                margin: 8px !important;
            }
            .leaflet-popup-tip {
                background: white;
            }
            .leaflet-popup-close-button {
                width: 28px !important;
                height: 28px !important;
                font-size: 22px !important;
                line-height: 26px !important;
                right: 6px !important;
                top: 6px !important;
                color: #666 !important;
                background: rgba(255,255,255,0.9) !important;
                border-radius: 50% !important;
                text-align: center !important;
                z-index: 100;
            }
        ");
        sb.AppendLine(@"
            #loading-overlay {
                position: absolute;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background: white;
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                z-index: 9999;
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            }
            .spinner {
                width: 50px;
                height: 50px;
                border: 4px solid #f3f3f3;
                border-top: 4px solid #E07B4C;
                border-radius: 50%;
                animation: spin 1s linear infinite;
            }
            @keyframes spin {
                0% { transform: rotate(0deg); }
                100% { transform: rotate(360deg); }
            }
            .loading-text {
                margin-top: 16px;
                color: #666;
                font-size: 14px;
            }
        ");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div id='loading-overlay'>");
        sb.AppendLine("  <div class='spinner'></div>");
        sb.AppendLine("  <div class='loading-text'>Đang tải bản đồ...</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div id='map'></div>");
        sb.AppendLine("<script>");
        sb.AppendLine("try {");

        var centerLat = currentLocation?.Latitude ?? 16.0544;
        var centerLng = currentLocation?.Longitude ?? 108.2022;

        // Initialize Leaflet map - OSM chính, CartoDB dự phòng
        sb.AppendLine("  console.log('[Map] Starting map initialization...');");
        sb.AppendLine("  console.log('[Map] Leaflet version: ' + (typeof L !== 'undefined' ? L.version : 'NOT LOADED'));");
        sb.AppendLine($"  console.log('[Map] Center: [{C(centerLat)}, {C(centerLng)}]');");
        sb.AppendLine($"  var map = L.map('map', {{ zoomControl: false }}).setView([{C(centerLat)}, {C(centerLng)}], 15);");
        sb.AppendLine("  console.log('[Map] ✅ Map object created');");
        sb.AppendLine("  var osm = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { attribution: '© OSM', maxZoom: 19 });");
        sb.AppendLine("  var carto = L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}.png', { attribution: '© CartoDB', maxZoom: 19 });");
        sb.AppendLine("  console.log('[Map] ✅ Tile layers created');");
        sb.AppendLine("  osm.addTo(map);");
        sb.AppendLine("  console.log('[Map] ✅ OSM tiles added to map');");
        sb.AppendLine("  osm.on('tileerror', function(e){ console.error('[Map] ❌ OSM tile error:', e); console.log('[Map] Switching to CartoDB...'); map.removeLayer(osm); carto.addTo(map); });");
        sb.AppendLine("  osm.on('tileload', function(){ console.log('[Map] ✅ OSM tile loaded successfully'); });");
        sb.AppendLine("  setTimeout(function(){ map.invalidateSize(); console.log('[Map] Map resized (100ms)'); }, 100);");
        sb.AppendLine("  setTimeout(function(){ map.invalidateSize(); console.log('[Map] Map resized (500ms)'); }, 500);");
        sb.AppendLine("  window.addEventListener('resize', function(){ map.invalidateSize(); console.log('[Map] Map resized (window resize)'); });");

        // Global variable for geofence highlight circle
        sb.AppendLine("  var geoCircle = null;");

        // Add current location marker (global var so JS can update it)
        if (currentLocation != null)
        {
            sb.AppendLine("  console.log('[Map] Adding current location marker...');");
            sb.AppendLine($"  var currentLocationIcon = L.divIcon({{");
            sb.AppendLine("    className: 'current-location',");
            sb.AppendLine("    iconSize: [20, 20],");
            sb.AppendLine("    iconAnchor: [10, 10]");
            sb.AppendLine("  });");
            sb.AppendLine($"  var currentMarker = L.marker([{C(currentLocation.Latitude)}, {C(currentLocation.Longitude)}], {{ icon: currentLocationIcon }}).addTo(map);");
            sb.AppendLine("  currentMarker.bindPopup('<div class=\"popup-content\"><div class=\"popup-title\">📍 Vị trí của bạn</div></div>');");
            sb.AppendLine("  console.log('[Map] ✅ Current location marker added');");
        }
        else
        {
            // Create marker at default position so test mode can move it
            sb.AppendLine("  console.log('[Map] Adding default location marker (no GPS)...');");
            sb.AppendLine($"  var currentLocationIcon = L.divIcon({{");
            sb.AppendLine("    className: 'current-location',");
            sb.AppendLine("    iconSize: [20, 20],");
            sb.AppendLine("    iconAnchor: [10, 10]");
            sb.AppendLine("  });");
            sb.AppendLine($"  var currentMarker = L.marker([{C(centerLat)}, {C(centerLng)}], {{ icon: currentLocationIcon }}).addTo(map);");
            sb.AppendLine("  console.log('[Map] ✅ Default location marker added');");
        }

        // Find nearest POI to highlight (only from valid POIs)
        var validPois = pois.Where(p => p.Latitude != 0 && p.Longitude != 0).ToList();
        var nearestPoiId = currentLocation != null && validPois.Any()
            ? validPois.OrderBy(p => HaversineDistance(currentLocation.Latitude, currentLocation.Longitude, p.Latitude, p.Longitude)).First().Id
            : (int?)null;

        // Add POI markers with geofence radius circles
        sb.AppendLine($"  console.log('[Map] Adding {pois.Count} POI markers...');");
        foreach (var poi in pois)
        {
            // Skip POIs with invalid coordinates (0 indicates unset location)
            if (poi.Latitude == 0 || poi.Longitude == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[MapPage] ⚠️ Skipping POI #{poi.Id} '{poi.Name}' - invalid coordinates (Lat: {poi.Latitude}, Lng: {poi.Longitude})");
                continue;
            }

            var escapedName = EscapeJs(poi.Name);
            var escapedDesc = EscapeJs(poi.Description);
            var escapedAddr = EscapeJs(poi.Address ?? "");
            var isNearest = poi.Id == nearestPoiId;
            var distance = currentLocation != null
                ? HaversineDistance(currentLocation.Latitude, currentLocation.Longitude, poi.Latitude, poi.Longitude)
                : 0;
            var distanceText = distance < 1000 ? $"{distance:F0}m" : $"{distance / 1000:F1}km";
            var rating = poi.Rating?.ToString("F1") ?? "4.5";

            sb.AppendLine($"  var poiIcon{poi.Id} = L.divIcon({{");
            sb.AppendLine($"    className: '{(isNearest ? "nearest-marker" : "poi-marker")}',");
            sb.AppendLine($"    iconSize: [{(isNearest ? 44 : 36)}, {(isNearest ? 44 : 36)}],");
            sb.AppendLine($"    iconAnchor: [{(isNearest ? 22 : 18)}, {(isNearest ? 44 : 36)}],");
            sb.AppendLine($"    popupAnchor: [0, {(isNearest ? -44 : -36)}],");
            sb.AppendLine($"    html: '<div class=\"poi-marker-inner\">📍</div>'");
            sb.AppendLine("  });");

            sb.AppendLine($"  var marker{poi.Id} = L.marker([{C(poi.Latitude)}, {C(poi.Longitude)}], {{ icon: poiIcon{poi.Id} }}).addTo(map);");

            // Add a subtle geofence radius circle for each POI
            if (poi.Radius > 0)
            {
                sb.AppendLine($"  L.circle([{C(poi.Latitude)}, {C(poi.Longitude)}], {{");
                sb.AppendLine($"    radius: {C(poi.Radius)},");
                sb.AppendLine("    color: '#E07B4C',");
                sb.AppendLine("    fillColor: '#E07B4C',");
                sb.AppendLine("    fillOpacity: 0.05,");
                sb.AppendLine("    weight: 1,");
                sb.AppendLine("    dashArray: '4 4'");
                sb.AppendLine("  }).addTo(map);");
            }

            var addrHtml = string.IsNullOrEmpty(poi.Address) ? "" : $"<div class=\"popup-address\">📍 {escapedAddr}</div>";
            var popupHtml = $@"
                <div class=\""popup-content\"">
                    <div class=\""popup-title\"">{escapedName}</div>
                    {addrHtml}
                    <div class=\""popup-desc\"">{escapedDesc}</div>
                    <div class=\""popup-meta\"">
                        <span>⭐ {rating}</span>
                        <span>📍 {distanceText}</span>
                        <span>⏱ {poi.EstimatedMinutes} phút</span>
                    </div>
                    <button class=\""popup-btn\"" onclick=\""selectPOI({poi.Id})\"">🔊 Nghe thuyết minh</button>
                </div>
            ".Replace("\n", "").Replace("\r", "");

            sb.AppendLine($"  marker{poi.Id}.bindPopup('{popupHtml}', {{maxWidth: 240, autoPanPadding: [20, 20]}});");
        }
        sb.AppendLine("  console.log('[Map] ✅ All POI markers added');");

        // Hide loading overlay once map is fully initialized
        sb.AppendLine("  setTimeout(function() {");
        sb.AppendLine("    var overlay = document.getElementById('loading-overlay');");
        sb.AppendLine("    if (overlay) {");
        sb.AppendLine("      overlay.style.display = 'none';");
        sb.AppendLine("      console.log('[Map] ✅ Loading overlay hidden - map ready!');");
        sb.AppendLine("    }");
        sb.AppendLine("  }, 1000);");

        // Function to handle POI selection
        sb.AppendLine("  function selectPOI(poiId) {");
        sb.AppendLine("    console.log('[Map] POI selected: ' + poiId);");
        sb.AppendLine("    window.location.href = 'poi://' + poiId;");
        sb.AppendLine("  }");

        // Close try-catch block
        sb.AppendLine("} catch (error) {");
        sb.AppendLine("  console.error('[Map] ❌ CRITICAL ERROR during initialization:', error);");
        sb.AppendLine("  console.error('[Map] Error message:', error.message);");
        sb.AppendLine("  console.error('[Map] Error stack:', error.stack);");
        sb.AppendLine("  var overlay = document.getElementById('loading-overlay');");
        sb.AppendLine("  if (overlay) {");
        sb.AppendLine("    overlay.innerHTML = '<div style=\"text-align:center;padding:20px;\"><div style=\"font-size:48px;\">❌</div><div style=\"color:#f44336;font-weight:bold;margin-top:12px;\">Lỗi tải bản đồ</div><div style=\"color:#666;margin-top:8px;font-size:13px;\">' + error.message + '</div></div>';");
        sb.AppendLine("  }");
        sb.AppendLine("}");

        sb.AppendLine("</script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    /// <summary>Format a double for JavaScript (always uses '.' as decimal separator)</summary>
    private static string C(double value) => value.ToString(CultureInfo.InvariantCulture);

    private static string EscapeJs(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\"", "\\\"")
            .Replace("\r", "")
            .Replace("\n", " ");
    }

    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}
