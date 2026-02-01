using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeriStepAI.Mobile.Models;
using HeriStepAI.Mobile.Services;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;

namespace HeriStepAI.Mobile.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly ILocationService _locationService;
    private readonly IGeofenceService _geofenceService;
    private readonly INarrationService _narrationService;
    private readonly IPOIService _poiService;
    private readonly IApiService _apiService;

    // Location state
    [ObservableProperty]
    private string currentLocationText = "Đang lấy vị trí...";

    [ObservableProperty]
    private POI? currentPOI;

    [ObservableProperty]
    private bool isLocationEnabled;

    [ObservableProperty]
    private bool showLocationStatus = false;

    [ObservableProperty]
    private bool isLoading = false;

    // Voice settings
    [ObservableProperty]
    private string selectedLanguage = "vi";

    [ObservableProperty]
    private string selectedGender = "male";

    [ObservableProperty]
    private string selectedRegion = "central"; // north, central, south

    // Tours
    [ObservableProperty]
    private ObservableCollection<Tour> tours = new();

    [ObservableProperty]
    private ObservableCollection<Tour> recentTours = new();

    [ObservableProperty]
    private Tour? selectedTour;

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
            IsLoading = true;

            // Permission request PHẢI chạy trên main thread (Android)
            IsLocationEnabled = await MainThread.InvokeOnMainThreadAsync(() => _locationService.RequestLocationPermissionAsync());
            if (!IsLocationEnabled)
            {
                CurrentLocationText = "GPS chưa bật. Bật vị trí trong Cài đặt để dùng geofence.";
                ShowLocationStatus = true;
            }

            // Load POIs and Tours
            await LoadPOIsAsync();
            await LoadToursAsync();

            if (IsLocationEnabled)
            {
                _locationService.LocationChanged += OnLocationChanged;
                _geofenceService.POIEntered += OnPOIEntered;
                _locationService.StartLocationUpdates();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in InitializeAsync: {ex.Message}");
            CurrentLocationText = $"Lỗi: {ex.Message}";
            ShowLocationStatus = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadPOIsAsync()
    {
        try
        {
            // Sync từ API (có thể thất bại nếu API chưa chạy)
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
        }
    }

    private async Task LoadToursAsync()
    {
        try
        {
            // For now, create sample tours based on POIs
            // In production, this would come from API
            var pois = await _poiService.GetAllPOIsAsync() ?? new List<POI>();
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Tours.Clear();
                RecentTours.Clear();

                // Create sample tours
                var sampleTours = new List<Tour>
                {
                    new Tour
                    {
                        Id = 1,
                        Name = "Tour Linh Ứng cơ bản",
                        Description = "Khám phá chùa Linh Ứng và các điểm tham quan nổi bật",
                        ImageUrl = "https://images.unsplash.com/photo-1559592413-7cec4d0cae2b?w=400",
                        EstimatedMinutes = 90,
                        POICount = pois.Count > 0 ? pois.Count : 5,
                        Rating = 4.8,
                        ReviewCount = 128
                    },
                    new Tour
                    {
                        Id = 2,
                        Name = "Tour Đạo mới",
                        Description = "Hành trình tâm linh với các điểm đặc sắc",
                        ImageUrl = "https://images.unsplash.com/photo-1528181304800-259b08848526?w=400",
                        EstimatedMinutes = 120,
                        POICount = 8,
                        Rating = 4.6,
                        ReviewCount = 89
                    },
                    new Tour
                    {
                        Id = 3,
                        Name = "Tour Ẩm thực",
                        Description = "Khám phá các món đặc sản địa phương",
                        ImageUrl = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=400",
                        EstimatedMinutes = 60,
                        POICount = 6,
                        Rating = 4.9,
                        ReviewCount = 215
                    }
                };

                foreach (var tour in sampleTours)
                {
                    Tours.Add(tour);
                    RecentTours.Add(tour);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading tours: {ex.Message}");
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
    private void SelectGender(string gender)
    {
        SelectedGender = gender;
        System.Diagnostics.Debug.WriteLine($"Selected gender: {gender}");
    }

    [RelayCommand]
    private void SelectRegion(string region)
    {
        SelectedRegion = region;
        System.Diagnostics.Debug.WriteLine($"Selected region: {region}");
    }

    [RelayCommand]
    private async Task SelectTour(Tour tour)
    {
        SelectedTour = tour;
        System.Diagnostics.Debug.WriteLine($"Selected tour: {tour.Name}");
        
        // Navigate to Map page with selected tour
        await Shell.Current.GoToAsync("//MapPage");
    }

    [RelayCommand]
    private async Task CreateTour()
    {
        // Navigate to create tour page or show dialog
        await Shell.Current.DisplayAlert("Tạo Tour", "Tính năng tạo tour mới sẽ sớm có mặt!", "OK");
    }

    [RelayCommand]
    private async Task RefreshPOIs()
    {
        IsLoading = true;
        try
        {
            await LoadPOIsAsync();
            await LoadToursAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }
}
