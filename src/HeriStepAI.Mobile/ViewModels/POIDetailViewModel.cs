using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeriStepAI.Mobile.Models;
using HeriStepAI.Mobile.Services;

namespace HeriStepAI.Mobile.ViewModels;

[QueryProperty(nameof(SelectedPoi), "POI")]
public partial class POIDetailViewModel : ObservableObject
{
    private readonly INarrationService _narrationService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private POI selectedPoi = new();

    public POIDetailViewModel(
        INarrationService narrationService,
        ILocalizationService localizationService)
    {
        _narrationService = narrationService;
        _localizationService = localizationService;
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task PlayNarration()
    {
        await _narrationService.PlayNarrationAsync(SelectedPoi, _localizationService.CurrentLanguage, forcePlay: true);
    }

    [RelayCommand]
    private async Task Navigate()
    {
        try
        {
            var location = new Microsoft.Maui.Devices.Sensors.Location(SelectedPoi.Latitude, SelectedPoi.Longitude);
            var options = new Microsoft.Maui.ApplicationModel.MapLaunchOptions { Name = SelectedPoi.Name };
            await Microsoft.Maui.ApplicationModel.Map.Default.OpenAsync(location, options);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening map: {ex.Message}");
            await Shell.Current.DisplayAlert(
                _localizationService.GetString("Error"),
                "Không thể mở bản đồ. Vui lòng thử lại.",
                "OK");
        }
    }
}
