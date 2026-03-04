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
    private readonly ITourSelectionService _tourSelectionService;
    private readonly ILocalizationService _localizationService;
    private readonly ILocationSimulatorService _simulator;
    private readonly IGeofenceService _geofenceService;

    /// <summary>Danh sach POI hien thi tren ban do. Khong dung [ObservableProperty] de tranh trung ten.</summary>
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
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isTestMode = false;

    [ObservableProperty]
    private string testModeButtonText = "🧪 Test Mode";

    // Test mode status
    [ObservableProperty]
    private string testModeStatus = "";

    [ObservableProperty]
    private bool showTestModeStatus = false;

    [ObservableProperty]
    private int testModeTotalPOIs = 0;

    [ObservableProperty]
    private string testModeCurrentPOIName = "";

    // Localized labels
    public string LblSearch => _localizationService.GetString("Search");
    public string LblNearbyPlaces => _localizationService.GetString("NearbyPlaces");
    public string LblListenNarration => $"🔊 {_localizationService.GetString("ListenNarration")}";
    public string LblGetDirections => $"🗺️ {_localizationService.GetString("GetDirections")}";

    // Event to notify map update
    public event EventHandler? MapNeedsUpdate;

    // Event to notify map to move current location marker (without full reload)
    public event EventHandler<Location>? SimulatedLocationChanged;

    // Event to notify map that a POI was triggered by geofence
    public event EventHandler<POI>? GeofenceTriggered;

    public MapPageViewModel(
        IPOIService poiService,
        INarrationService narrationService,
        IApiService apiService,
        ILocationService locationService,
        ITourSelectionService tourSelectionService,
        ILocalizationService localizationService,
        ILocationSimulatorService simulator,
        IGeofenceService geofenceService)
    {
        _poiService = poiService;
        _narrationService = narrationService;
        _apiService = apiService;
        _locationService = locationService;
        _tourSelectionService = tourSelectionService;
        _localizationService = localizationService;
        _simulator = simulator;
        _geofenceService = geofenceService;

        _localizationService.LanguageChanged += (_, _) => RefreshTranslations();

        // Subscribe to location changes for geofence checking
        _locationService.LocationChanged += OnLocationChanged;

        // Subscribe to geofence triggers for auto-narration
        _geofenceService.POIEntered += OnGeofencePOIEntered;

        // Khi narration hoàn tất → chuyển simulator sang POI tiếp theo
        _narrationService.NarrationCompleted += OnNarrationCompleted;

        // Khi simulator đã chạy hết tất cả POI → tự dừng test mode
        _simulator.SimulationCompleted += OnSimulationCompleted;

        // Run initialization in background to avoid blocking
        Task.Run(async () =>
        {
            try
            {
                await LoadPOIsAsync();
                await GetCurrentLocationAsync();

                // Đảm bảo GPS polling chạy liên tục (5s/lần)
                // để geofence check hoạt động với real GPS
                _locationService.StartLocationUpdates();

                // Start foreground service to keep location tracking active in background
                LocationForegroundServiceManager.Start();
                AppLog.Info("Location foreground service started for background tracking");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in MapPageViewModel initialization: {ex.Message}");
            }
        });
    }

    private void RefreshTranslations()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(LblSearch));
            OnPropertyChanged(nameof(LblNearbyPlaces));
            OnPropertyChanged(nameof(LblListenNarration));
            OnPropertyChanged(nameof(LblGetDirections));
        });
    }

    partial void OnSelectedPOIChanged(POI? value)
    {
        HasSelectedPOI = value != null;
    }

    /// <summary>
    /// Called when location changes (both real GPS and simulated).
    /// Marshal to main thread: UI binding + geofence + map update.
    /// </summary>
    private void OnLocationChanged(object? sender, Location location)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentLocation = location;
            _geofenceService.CheckGeofence(location);
            if (IsTestMode)
                SimulatedLocationChanged?.Invoke(this, location);
        });
    }

    /// <summary>
    /// Called when geofence detects user entered a POI zone.
    /// UI + map update ngay; log + narration chạy nền để tránh block main thread (ANR).
    /// </summary>
    private void OnGeofencePOIEntered(object? sender, POI poi)
    {
        AppLog.Info($"📍 Geofence triggered: {poi.Name} (ID={poi.Id})");

        if (IsTestMode)
        {
            TestModeStatus = $"🔊 {poi.Name}";
            TestModeCurrentPOIName = poi.Name;
        }

        GeofenceTriggered?.Invoke(this, poi);

        // Chạy log + narration nền, không await — tránh ANR
        var lang = _localizationService.CurrentLanguage;
        _ = Task.Run(async () =>
        {
            try
            {
                await _apiService.LogVisitAsync(poi.Id, poi.Latitude, poi.Longitude, VisitType.Geofence);
                AppLog.Info($"✅ Logged geofence visit for {poi.Name}");
            }
            catch (Exception ex)
            {
                AppLog.Error($"Failed to log geofence visit: {ex.Message}");
            }

            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    _narrationService.PlayNarrationAsync(poi, lang, forcePlay: false));
                AppLog.Info($"🔊 Auto-narration started for {poi.Name}");
            }
            catch (Exception ex)
            {
                AppLog.Error($"Failed to play narration: {ex.Message}");
            }
        });
    }

    private async Task LoadPOIsAsync()
    {
        try
        {
            var allPois = await _poiService.GetAllPOIsAsync() ?? new();
            var selectedTour = _tourSelectionService.SelectedTour;

            if (selectedTour != null && selectedTour.POIs != null && selectedTour.POIs.Any())
            {
                POIs = selectedTour.POIs.ToList();
            }
            else
            {
                POIs = allPois;
            }

            // Initialize geofence with all POIs
            _geofenceService.Initialize(POIs);
            AppLog.Info($"🎯 GeofenceService initialized with {POIs.Count} POIs");

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
            await _apiService.LogVisitAsync(poi.Id, CurrentLocation.Latitude, CurrentLocation.Longitude, VisitType.MapClick);
        }

        await _narrationService.PlayNarrationAsync(poi, _localizationService.CurrentLanguage, forcePlay: true);
    }

    [RelayCommand]
    private async Task PlayNarration()
    {
        if (SelectedPOI != null)
        {
            await _narrationService.PlayNarrationAsync(SelectedPOI, _localizationService.CurrentLanguage, forcePlay: true);
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
                await Shell.Current.DisplayAlert(
                    _localizationService.GetString("Error"),
                    _localizationService.GetString("MapError"),
                    "OK");
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
        MapNeedsUpdate?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Khi narration xong 1 POI → báo simulator advance sang POI kế tiếp.
    /// </summary>
    private void OnNarrationCompleted(object? sender, EventArgs e)
    {
        if (IsTestMode)
        {
            _simulator.AdvanceToNext();
        }
    }

    /// <summary>
    /// Khi simulator đã đi hết tất cả POI → tự dừng test mode.
    /// </summary>
    private void OnSimulationCompleted(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AppLog.Info("✅ Simulation completed all POIs, stopping test mode");
            StopTestMode();
        });
    }

    [RelayCommand]
    private void ToggleTestMode()
    {
        if (!IsTestMode)
        {
            var route = POIs.ToList();
            if (route.Count == 0)
            {
                AppLog.Info("Cannot start test mode: no POIs available");
                return;
            }

            // Re-initialize geofence with the route POIs
            _geofenceService.Initialize(route);

            // Start event-driven simulation (90s max timeout per POI)
            // Simulator chờ narration xong (AdvanceToNext) rồi mới chuyển POI
            _simulator.StartSimulation(route, maxSecondsPerPOI: 90);

            // Update UI state
            IsTestMode = true;
            TestModeButtonText = "🛑 Stop";
            ShowTestModeStatus = true;
            TestModeTotalPOIs = route.Count;
            TestModeStatus = $"🧪 Simulating {route.Count} POI...";

            AppLog.Info($"🧪 Test Mode started with {route.Count} POIs, event-driven");
        }
        else
        {
            StopTestMode();
        }
    }

    private void StopTestMode()
    {
        _simulator.StopSimulation();
        _narrationService.StopNarration();

        IsTestMode = false;
        TestModeButtonText = "🧪 Test Mode";
        ShowTestModeStatus = false;
        TestModeStatus = "";
        TestModeCurrentPOIName = "";

        AppLog.Info("🛑 Test Mode stopped");
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
