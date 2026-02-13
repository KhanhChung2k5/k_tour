using HeriStepAI.Mobile.Models;
using HeriStepAI.Mobile.ViewModels;
using HeriStepAI.Mobile.Helpers;
using System.Text;

namespace HeriStepAI.Mobile.Views;

public partial class MapPage : ContentPage
{
    private readonly MapPageViewModel _viewModel;

    public MapPage() : this(GetViewModel()) { }

    private bool _mapLoaded;

    public MapPage(MapPageViewModel viewModel)
    {
        try
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;

            // Apply responsive padding to top bar
            TopBar.Padding = ResponsiveHelper.HeaderPadding();

            // Subscribe to POI selection from map
            MapWebView.Navigating += OnMapNavigating;

            // Subscribe to map update requests
            _viewModel.MapNeedsUpdate += OnMapNeedsUpdate;

            // Subscribe to real-time location updates during test mode
            _viewModel.SimulatedLocationChanged += OnSimulatedLocationChanged;

            // Subscribe to geofence triggers for map highlighting
            _viewModel.GeofenceTriggered += OnGeofenceTriggered;
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
                var js = $"if(typeof currentMarker !== 'undefined') {{ currentMarker.setLatLng([{location.Latitude}, {location.Longitude}]); map.panTo([{location.Latitude}, {location.Longitude}], {{animate: true, duration: 0.5}}); }}";
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
                        geoCircle = L.circle([{poi.Latitude}, {poi.Longitude}], {{
                            radius: {poi.Radius},
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

    private async void LoadMapAsync()
    {
        await Task.Delay(500); // Wait for view model to load POIs

        var html = GenerateMapHtml(_viewModel.POIs, _viewModel.CurrentLocation);

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
                    webView.LoadDataWithBaseURL(
                        "https://heristepai.app/",
                        html,
                        "text/html",
                        "utf-8",
                        null);
                }
                else
                {
                    // Fallback if handler not ready yet
                    MapWebView.Source = new HtmlWebViewSource { Html = html };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading map: {ex.Message}");
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
        sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'>");
        sb.AppendLine("<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />");
        sb.AppendLine("<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>");
        sb.AppendLine("<style>");
        sb.AppendLine("* { margin: 0; padding: 0; box-sizing: border-box; }");
        sb.AppendLine("body { margin: 0; padding: 0; overflow: hidden; }");
        sb.AppendLine("#map { width: 100vw; height: 100vh; }");
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
                padding: 12px;
                min-width: 220px;
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
                font-size: 13px;
                color: #424242;
                margin-bottom: 10px;
                line-height: 1.4;
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
                border-radius: 16px;
                box-shadow: 0 4px 20px rgba(0,0,0,0.15);
            }
            .leaflet-popup-tip {
                background: white;
            }
        ");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div id='map'></div>");
        sb.AppendLine("<script>");

        var centerLat = currentLocation?.Latitude ?? 16.0544;
        var centerLng = currentLocation?.Longitude ?? 108.2022;

        // Initialize Leaflet map with OpenStreetMap
        sb.AppendLine($"var map = L.map('map', {{ zoomControl: false }}).setView([{centerLat}, {centerLng}], 15);");
        sb.AppendLine("L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {");
        sb.AppendLine("  attribution: '© OpenStreetMap',");
        sb.AppendLine("  maxZoom: 19");
        sb.AppendLine("}).addTo(map);");

        // Global variable for geofence highlight circle
        sb.AppendLine("var geoCircle = null;");

        // Add current location marker (global var so JS can update it)
        if (currentLocation != null)
        {
            sb.AppendLine($"var currentLocationIcon = L.divIcon({{");
            sb.AppendLine("  className: 'current-location',");
            sb.AppendLine("  iconSize: [20, 20],");
            sb.AppendLine("  iconAnchor: [10, 10]");
            sb.AppendLine("});");
            sb.AppendLine($"var currentMarker = L.marker([{currentLocation.Latitude}, {currentLocation.Longitude}], {{ icon: currentLocationIcon }}).addTo(map);");
            sb.AppendLine("currentMarker.bindPopup('<div class=\"popup-content\"><div class=\"popup-title\">📍 Vị trí của bạn</div></div>');");
        }
        else
        {
            // Create marker at default position so test mode can move it
            sb.AppendLine($"var currentLocationIcon = L.divIcon({{");
            sb.AppendLine("  className: 'current-location',");
            sb.AppendLine("  iconSize: [20, 20],");
            sb.AppendLine("  iconAnchor: [10, 10]");
            sb.AppendLine("});");
            sb.AppendLine($"var currentMarker = L.marker([{centerLat}, {centerLng}], {{ icon: currentLocationIcon }}).addTo(map);");
        }

        // Find nearest POI to highlight
        var nearestPoiId = currentLocation != null && pois.Any()
            ? pois.OrderBy(p => HaversineDistance(currentLocation.Latitude, currentLocation.Longitude, p.Latitude, p.Longitude)).First().Id
            : (int?)null;

        // Add POI markers with geofence radius circles
        foreach (var poi in pois)
        {
            var escapedName = EscapeJs(poi.Name);
            var escapedDesc = EscapeJs(poi.Description);
            var escapedAddr = EscapeJs(poi.Address ?? "");
            var isNearest = poi.Id == nearestPoiId;
            var distance = currentLocation != null
                ? HaversineDistance(currentLocation.Latitude, currentLocation.Longitude, poi.Latitude, poi.Longitude)
                : 0;
            var distanceText = distance < 1000 ? $"{distance:F0}m" : $"{distance / 1000:F1}km";
            var rating = poi.Rating?.ToString("F1") ?? "4.5";

            sb.AppendLine($"var poiIcon{poi.Id} = L.divIcon({{");
            sb.AppendLine($"  className: '{(isNearest ? "nearest-marker" : "poi-marker")}',");
            sb.AppendLine($"  iconSize: [{(isNearest ? 44 : 36)}, {(isNearest ? 44 : 36)}],");
            sb.AppendLine($"  iconAnchor: [{(isNearest ? 22 : 18)}, {(isNearest ? 44 : 36)}],");
            sb.AppendLine($"  popupAnchor: [0, {(isNearest ? -44 : -36)}],");
            sb.AppendLine($"  html: '<div class=\"poi-marker-inner\">📍</div>'");
            sb.AppendLine("});");

            sb.AppendLine($"var marker{poi.Id} = L.marker([{poi.Latitude}, {poi.Longitude}], {{ icon: poiIcon{poi.Id} }}).addTo(map);");

            // Add a subtle geofence radius circle for each POI
            if (poi.Radius > 0)
            {
                sb.AppendLine($"L.circle([{poi.Latitude}, {poi.Longitude}], {{");
                sb.AppendLine($"  radius: {poi.Radius},");
                sb.AppendLine("  color: '#E07B4C',");
                sb.AppendLine("  fillColor: '#E07B4C',");
                sb.AppendLine("  fillOpacity: 0.05,");
                sb.AppendLine("  weight: 1,");
                sb.AppendLine("  dashArray: '4 4'");
                sb.AppendLine("}).addTo(map);");
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

            sb.AppendLine($"marker{poi.Id}.bindPopup('{popupHtml}');");
        }

        // Function to handle POI selection
        sb.AppendLine("function selectPOI(poiId) {");
        sb.AppendLine("  window.location.href = 'poi://' + poiId;");
        sb.AppendLine("}");

        sb.AppendLine("</script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

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
