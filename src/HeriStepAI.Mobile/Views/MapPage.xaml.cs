using HeriStepAI.Mobile.Models;
using HeriStepAI.Mobile.ViewModels;
using System.Text;

namespace HeriStepAI.Mobile.Views;

public partial class MapPage : ContentPage
{
    private readonly MapPageViewModel _viewModel;

    public MapPage(MapPageViewModel viewModel)
    {
        try
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;

            LoadMapAsync();
            
            // Subscribe to POI selection from map
            MapWebView.Navigating += OnMapNavigating;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in MapPage constructor: {ex}");
            _viewModel = viewModel ?? throw new InvalidOperationException("ViewModel cannot be null");
        }
    }

    private async void LoadMapAsync()
    {
        await Task.Delay(500); // Wait for view model to load POIs

        var html = GenerateMapHtml(_viewModel.POIs, _viewModel.CurrentLocation);
        var htmlSource = new HtmlWebViewSource { Html = html };
        MapWebView.Source = htmlSource;
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
        sb.AppendLine("body { margin: 0; padding: 0; }");
        sb.AppendLine("#map { width: 100%; height: 100vh; }");
        sb.AppendLine(".custom-marker { background-color: #ff0000; width: 20px; height: 20px; border-radius: 50%; border: 2px solid white; }");
        sb.AppendLine(".current-location-marker { background-color: #0066ff; width: 20px; height: 20px; border-radius: 50%; border: 2px solid white; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div id='map'></div>");
        sb.AppendLine("<script>");
        
        var centerLat = currentLocation?.Latitude ?? 16.0544;
        var centerLng = currentLocation?.Longitude ?? 108.2022;

        // Initialize Leaflet map with OpenStreetMap
        sb.AppendLine($"var map = L.map('map').setView([{centerLat}, {centerLng}], 14);");
        sb.AppendLine("L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {");
        sb.AppendLine("  attribution: '© OpenStreetMap contributors',");
        sb.AppendLine("  maxZoom: 19");
        sb.AppendLine("}).addTo(map);");

        // Add current location marker
        if (currentLocation != null)
        {
            sb.AppendLine($"var currentLocationIcon = L.divIcon({{");
            sb.AppendLine("  className: 'current-location-marker',");
            sb.AppendLine("  iconSize: [20, 20]");
            sb.AppendLine("});");
            sb.AppendLine($"var currentMarker = L.marker([{currentLocation.Latitude}, {currentLocation.Longitude}], {{ icon: currentLocationIcon }}).addTo(map);");
            sb.AppendLine("currentMarker.bindPopup('<b>Vị trí của bạn</b>').openPopup();");
        }

        // Add POI markers
        foreach (var poi in pois)
        {
            var escapedName = poi.Name.Replace("'", "\\'").Replace("\"", "\\\"");
            var escapedDesc = poi.Description.Replace("'", "\\'").Replace("\"", "\\\"");

            sb.AppendLine($"var poiIcon{poi.Id} = L.divIcon({{");
            sb.AppendLine("  className: 'custom-marker',");
            sb.AppendLine("  iconSize: [20, 20]");
            sb.AppendLine("});");
            
            sb.AppendLine($"var marker{poi.Id} = L.marker([{poi.Latitude}, {poi.Longitude}], {{ icon: poiIcon{poi.Id} }}).addTo(map);");
            
            sb.AppendLine($"var popup{poi.Id} = L.popup().setContent('<div style=\"padding: 10px;\"><h3 style=\"margin: 0 0 10px 0;\">{escapedName}</h3><p style=\"margin: 0;\">{escapedDesc}</p><button onclick=\"selectPOI({poi.Id})\" style=\"margin-top: 10px; padding: 5px 10px; background: #007bff; color: white; border: none; border-radius: 4px; cursor: pointer;\">Xem chi tiết</button></div>');");
            sb.AppendLine($"marker{poi.Id}.bindPopup(popup{poi.Id});");

            // Click event to trigger POI selection
            sb.AppendLine($"marker{poi.Id}.on('click', function() {{");
            sb.AppendLine($"  selectPOI({poi.Id});");
            sb.AppendLine($"}});");
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

    private async void OnPOISelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is POI poi)
        {
            await _viewModel.POISelectedCommand.ExecuteAsync(poi);
        }
    }
}
