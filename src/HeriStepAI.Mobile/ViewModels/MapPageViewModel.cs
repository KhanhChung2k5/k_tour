using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeriStepAI.Mobile.Models;
using HeriStepAI.Mobile.Services;
using Microsoft.Maui.Devices.Sensors;

namespace HeriStepAI.Mobile.ViewModels;

public partial class MapPageViewModel : ObservableObject
{
    private readonly IPOIService _poiService;
    private readonly INarrationService _narrationService;
    private readonly IApiService _apiService;
    private readonly ILocationService _locationService;

    [ObservableProperty]
    public List<POI> pois = new();

    public List<POI> POIs { get; set; } = new();

    [ObservableProperty]
    private POI? selectedPOI;

    [ObservableProperty]
    private Location? currentLocation;

    [ObservableProperty]
    private string selectedLanguage = "vi";

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

    private async Task LoadPOIsAsync()
    {
        try
        {
            POIs = await _poiService.GetAllPOIsAsync() ?? new();
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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting location: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task POISelected(POI poi)
    {
        SelectedPOI = poi;

        // Log visit
        if (CurrentLocation != null)
        {
            await _apiService.LogVisitAsync(poi.Id, CurrentLocation.Latitude, CurrentLocation.Longitude, Services.VisitType.MapClick);
        }

        // Play narration
        await _narrationService.PlayNarrationAsync(poi, SelectedLanguage);
    }
}
