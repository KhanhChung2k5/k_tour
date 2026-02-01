using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeriStepAI.Mobile.Models;
using HeriStepAI.Mobile.Services;
using Microsoft.Maui.Devices.Sensors;
using System.Collections.ObjectModel;

namespace HeriStepAI.Mobile.ViewModels;

public partial class MapPageViewModel : ObservableObject
{
    private readonly IPOIService _poiService;
    private readonly INarrationService _narrationService;
    private readonly IApiService _apiService;
    private readonly ILocationService _locationService;

    /// <summary>Danh sách POI hiển thị trên bản đồ. Không dùng [ObservableProperty] để tránh trùng tên.</summary>
    public List<POI> POIs { get; set; } = new();

    [ObservableProperty]
    private ObservableCollection<POI> nearbyPOIs = new();

    [ObservableProperty]
    private POI? selectedPOI;

    [ObservableProperty]
    private bool hasSelectedPOI;

    [ObservableProperty]
    private Location? currentLocation;

    [ObservableProperty]
    private string selectedLanguage = "vi";

    [ObservableProperty]
    private string searchText = string.Empty;

    // Event to notify map update
    public event EventHandler? MapNeedsUpdate;

    public MapPageViewModel(
        IPOIService poiService,
        INarrationService narrationService,
        IApiService apiService,
        ILocationService locationService)
    {
        _poiService = poiService;
        _narrationService = narrationService;
        _apiService = apiService;
        _locationService = locationService;

        // Run initialization in background to avoid blocking
        Task.Run(async () =>
        {
            try
            {
                await LoadPOIsAsync();
                await GetCurrentLocationAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in MapPageViewModel initialization: {ex.Message}");
            }
        });
    }

    partial void OnSelectedPOIChanged(POI? value)
    {
        HasSelectedPOI = value != null;
    }

    private async Task LoadPOIsAsync()
    {
        try
        {
            POIs = await _poiService.GetAllPOIsAsync() ?? new();
            
            // Update nearby POIs with sample images if no ImageUrl
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                NearbyPOIs.Clear();
                var sampleImages = new[]
                {
                    "https://images.unsplash.com/photo-1559592413-7cec4d0cae2b?w=400",
                    "https://images.unsplash.com/photo-1528181304800-259b08848526?w=400",
                    "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=400",
                    "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=400",
                    "https://images.unsplash.com/photo-1523906834658-6e24ef2386f9?w=400"
                };
                
                var index = 0;
                foreach (var poi in POIs.Take(5))
                {
                    if (string.IsNullOrEmpty(poi.ImageUrl))
                    {
                        poi.ImageUrl = sampleImages[index % sampleImages.Length];
                    }
                    if (!poi.Rating.HasValue)
                    {
                        poi.Rating = 4.0 + new Random().NextDouble();
                    }
                    NearbyPOIs.Add(poi);
                    index++;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading POIs: {ex.Message}");
            POIs = new();
        }
    }

    private async Task GetCurrentLocationAsync()
    {
        try
        {
            CurrentLocation = await _locationService.GetCurrentLocationAsync();
            
            // Calculate distances for POIs
            if (CurrentLocation != null)
            {
                foreach (var poi in POIs)
                {
                    poi.DistanceMeters = HaversineDistance(
                        CurrentLocation.Latitude,
                        CurrentLocation.Longitude,
                        poi.Latitude,
                        poi.Longitude);
                }
                
                // Re-sort nearby POIs by distance
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var sorted = NearbyPOIs.OrderBy(p => p.DistanceMeters).ToList();
                    NearbyPOIs.Clear();
                    foreach (var poi in sorted)
                    {
                        NearbyPOIs.Add(poi);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting location: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task POISelected(POI poi)
    {
        if (poi == null) return;

        SelectedPOI = poi;

        // Calculate distance if we have location
        if (CurrentLocation != null && !poi.DistanceMeters.HasValue)
        {
            poi.DistanceMeters = HaversineDistance(
                CurrentLocation.Latitude,
                CurrentLocation.Longitude,
                poi.Latitude,
                poi.Longitude);
        }

        // Log visit
        if (CurrentLocation != null)
        {
            await _apiService.LogVisitAsync(poi.Id, CurrentLocation.Latitude, CurrentLocation.Longitude, Services.VisitType.MapClick);
        }

        // Phát thuyết minh khi click vào POI trên bản đồ
        await _narrationService.PlayNarrationAsync(poi, SelectedLanguage, forcePlay: true);
    }

    [RelayCommand]
    private async Task PlayNarration()
    {
        if (SelectedPOI != null)
        {
            await _narrationService.PlayNarrationAsync(SelectedPOI, SelectedLanguage, forcePlay: true);
        }
    }

    [RelayCommand]
    private async Task Navigate()
    {
        if (SelectedPOI != null)
        {
            try
            {
                var location = new Location(SelectedPOI.Latitude, SelectedPOI.Longitude);
                var options = new MapLaunchOptions { Name = SelectedPOI.Name };
                await Map.Default.OpenAsync(location, options);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening map: {ex.Message}");
                await Shell.Current.DisplayAlert("Lỗi", "Không thể mở ứng dụng bản đồ", "OK");
            }
        }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task ShowList()
    {
        await Shell.Current.GoToAsync("//POIListPage");
    }

    [RelayCommand]
    private void CenterOnLocation()
    {
        // Trigger map update to center on current location
        MapNeedsUpdate?.Invoke(this, EventArgs.Empty);
    }

    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth radius in meters
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}
