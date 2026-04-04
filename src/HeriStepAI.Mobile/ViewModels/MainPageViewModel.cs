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
    private readonly ITourSelectionService _tourSelectionService;
    private readonly ILocalizationService _localizationService;
    private readonly ITourGeneratorService _tourGenerator;
    private readonly IAnalyticsService _analytics;
    private Location? _lastLocation;

    // Location state
    [ObservableProperty]
    private string currentLocationText = "";

    [ObservableProperty]
    private POI? currentPOI;

    [ObservableProperty]
    private bool isLocationEnabled;

    [ObservableProperty]
    private bool showLocationStatus = false;

    [ObservableProperty]
    private bool isLoading = false;

    // Voice & narration dùng ngôn ngữ app
    private string NarrationLanguage => _localizationService.CurrentLanguage;

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

    [ObservableProperty]
    private Tour? featuredTour;

    public bool HasFeaturedTour => FeaturedTour != null;

    public MainPageViewModel(
        ILocationService locationService,
        IGeofenceService geofenceService,
        INarrationService narrationService,
        IPOIService poiService,
        IApiService apiService,
        ITourSelectionService tourSelectionService,
        ILocalizationService localizationService,
        ITourGeneratorService tourGenerator,
        IAnalyticsService analytics)
    {
        _locationService = locationService;
        _geofenceService = geofenceService;
        _narrationService = narrationService;
        _poiService = poiService;
        _apiService = apiService;
        _tourSelectionService = tourSelectionService;
        _localizationService = localizationService;
        _tourGenerator = tourGenerator;
        _analytics = analytics;

        _localizationService.LanguageChanged += (_, _) => RefreshTranslations();

        // Don't await here - run in background
        Task.Run(async () => await InitializeAsync());
    }

    private void RefreshTranslations()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(LblWelcome));
            OnPropertyChanged(nameof(LblAppName));
            OnPropertyChanged(nameof(LblAppTagline));
            OnPropertyChanged(nameof(LblVoiceNarration));
            OnPropertyChanged(nameof(LblMale));
            OnPropertyChanged(nameof(LblFemale));
            OnPropertyChanged(nameof(LblRegionNorth));
            OnPropertyChanged(nameof(LblRegionCentral));
            OnPropertyChanged(nameof(LblRegionSouth));
            OnPropertyChanged(nameof(LblChooseTour));
            OnPropertyChanged(nameof(LblCreateNewTour));
            OnPropertyChanged(nameof(LblRecentTours));
            OnPropertyChanged(nameof(LblSettings));
            OnPropertyChanged(nameof(LblRefresh));
            OnPropertyChanged(nameof(LblLanguageSwitch));
        });
    }

    public string LblWelcome => _localizationService.GetString("Welcome");
    public string LblAppName => _localizationService.GetString("AppName");
    public string LblAppTagline => _localizationService.GetString("AppTagline");
    public string LblVoiceNarration => _localizationService.GetString("VoiceNarration");
    public string LblMale => _localizationService.GetString("Male");
    public string LblFemale => _localizationService.GetString("Female");
    public string LblRegionNorth => _localizationService.GetString("RegionNorth");
    public string LblRegionCentral => _localizationService.GetString("RegionCentral");
    public string LblRegionSouth => _localizationService.GetString("RegionSouth");
    public string LblChooseTour => _localizationService.GetString("ChooseTour");
    public string LblCreateNewTour => _localizationService.GetString("CreateNewTour");
    public string LblRecentTours => _localizationService.GetString("RecentTours");
    public string LblSettings => _localizationService.GetString("Settings");
    public string LblRefresh => _localizationService.GetString("Refresh");
    private static readonly string[] _langCycle = { "vi", "en", "ko", "zh", "ja", "th", "fr" };
    private static readonly Dictionary<string, string> _langLabels = new()
    {
        ["vi"] = "VI", ["en"] = "EN", ["ko"] = "KO", ["zh"] = "ZH",
        ["ja"] = "JA", ["th"] = "TH", ["fr"] = "FR",
    };
    public string LblLanguageSwitch => _langLabels.GetValueOrDefault(_localizationService.CurrentLanguage, "VI");

    private async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            CurrentLocationText = _localizationService.GetString("GettingLocation");

            // Permission request PHẢI chạy trên main thread (Android)
            IsLocationEnabled = await MainThread.InvokeOnMainThreadAsync(() => _locationService.RequestLocationPermissionAsync());
            if (!IsLocationEnabled)
            {
                CurrentLocationText = _localizationService.GetString("GpsNotEnabled");
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
            CurrentLocationText = $"{_localizationService.GetString("Error")}: {ex.Message}";
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
            var allPois = await _poiService.GetAllPOIsAsync() ?? new List<POI>();

            // Generate smart tours using TourGeneratorService
            var builtTours = _tourGenerator.GenerateSmartTours(allPois);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Tours.Clear();
                RecentTours.Clear();
                foreach (var tour in builtTours)
                {
                    Tours.Add(tour);
                    RecentTours.Add(tour);
                }
                FeaturedTour = Tours.FirstOrDefault();
                OnPropertyChanged(nameof(HasFeaturedTour));
            });

            AppLog.Info($"Loaded {builtTours.Count} smart tours");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading tours: {ex.Message}");
            AppLog.Error($"Error loading tours: {ex.Message}");
        }
    }

    private void OnLocationChanged(object? sender, Location location)
    {
        CurrentLocationText = $"Lat: {location.Latitude:F6}, Lng: {location.Longitude:F6}";

        // Track distance
        if (_lastLocation != null)
        {
            double meters = Location.CalculateDistance(_lastLocation, location, DistanceUnits.Kilometers) * 1000;
            _analytics.AddDistance(meters);
        }
        _lastLocation = location;

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
        _analytics.RecordPOIVisit(poi);
        // Note: LogVisitAsync is handled by MapPageViewModel.OnGeofencePOIEntered (avoids duplicate log)

        // Play narration - dùng ngôn ngữ app
        await _narrationService.PlayNarrationAsync(poi, NarrationLanguage);
        _analytics.RecordNarration();
    }

    [RelayCommand]
    private void SwitchLanguage()
    {
        var current = _localizationService.CurrentLanguage;
        var idx = Array.IndexOf(_langCycle, current);
        var next = _langCycle[(idx + 1) % _langCycle.Length];
        _localizationService.SetLanguage(next);
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

        // Navigate to TourDetailPage with tour data
        await Shell.Current.GoToAsync("TourDetailPage", new Dictionary<string, object>
        {
            { "Tour", tour }
        });
    }

    [RelayCommand]
    private async Task CreateTour()
    {
        await Shell.Current.DisplayAlert(
            _localizationService.GetString("CreateTour"),
            _localizationService.GetString("CreateTourComingSoon"),
            "OK");
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
