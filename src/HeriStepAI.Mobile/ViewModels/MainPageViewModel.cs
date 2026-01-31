using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeriStepAI.Mobile.Models;
using HeriStepAI.Mobile.Services;
using Microsoft.Maui.Devices.Sensors;

namespace HeriStepAI.Mobile.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly ILocationService _locationService;
    private readonly IGeofenceService _geofenceService;
    private readonly INarrationService _narrationService;
    private readonly IPOIService _poiService;
    private readonly IApiService _apiService;

    [ObservableProperty]
    private string currentLocationText = "Đang lấy vị trí...";

    [ObservableProperty]
    private POI? currentPOI;

    [ObservableProperty]
    private bool isLocationEnabled;

    [ObservableProperty]
    private string selectedLanguage = "vi";

    public MainPageViewModel(
        ILocationService locationService,
        IGeofenceService geofenceService,
        INarrationService narrationService,
        IPOIService poiService,
        IApiService apiService)
    {
        _locationService = locationService;
        _geofenceService = geofenceService;
        _narrationService = narrationService;
        _poiService = poiService;
        _apiService = apiService;

        // Don't await here - run in background
        Task.Run(async () => await InitializeAsync());
    }

    private async Task InitializeAsync()
    {
        try
        {
            // Request location permission
            IsLocationEnabled = await _locationService.RequestLocationPermissionAsync();

            if (IsLocationEnabled)
            {
                // Load POIs
                await LoadPOIsAsync();

                // Subscribe to location updates
                _locationService.LocationChanged += OnLocationChanged;
                _geofenceService.POIEntered += OnPOIEntered;

                // Start location tracking
                _locationService.StartLocationUpdates();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in InitializeAsync: {ex.Message}");
            CurrentLocationText = $"Lỗi khởi tạo: {ex.Message}";
        }
    }

    private async Task LoadPOIsAsync()
    {
        try
        {
            await _poiService.SyncPOIsFromServerAsync();
            var pois = await _poiService.GetAllPOIsAsync();
            if (pois != null && pois.Any())
            {
                _geofenceService.Initialize(pois);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading POIs: {ex.Message}");
            CurrentLocationText = $"Lỗi tải POI: {ex.GetType().Name}";
        }
    }

    private void OnLocationChanged(object? sender, Location location)
    {
        CurrentLocationText = $"Lat: {location.Latitude:F6}, Lng: {location.Longitude:F6}";
        
        // Check geofence
        var poi = _geofenceService.CheckGeofence(location);
        if (poi != null)
        {
            CurrentPOI = poi;
        }
    }

    private async void OnPOIEntered(object? sender, POI poi)
    {
        CurrentPOI = poi;
        
        // Log visit
        var location = await _locationService.GetCurrentLocationAsync();
        if (location != null)
        {
            await _apiService.LogVisitAsync(poi.Id, location.Latitude, location.Longitude, Services.VisitType.Geofence);
        }

        // Play narration
        await _narrationService.PlayNarrationAsync(poi, SelectedLanguage);
    }


    [RelayCommand]
    private async Task RefreshPOIs()
    {
        await LoadPOIsAsync();
    }
}
