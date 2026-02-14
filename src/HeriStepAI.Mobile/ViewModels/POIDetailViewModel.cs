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

    // Localized labels
    public string LblAddress => _localizationService.GetString("Address");
    public string LblFoodType => _localizationService.GetString("FoodType");
    public string LblPrice => _localizationService.GetString("Price");
    public string LblVisitTime => _localizationService.GetString("VisitTime");
    public string LblDescription => _localizationService.GetString("Description");
    public string LblListenNarration => $"🔊 {_localizationService.GetString("ListenNarration")}";
    public string LblGetDirections => $"🗺️ {_localizationService.GetString("GetDirections")}";

    public POIDetailViewModel(
        INarrationService narrationService,
        ILocalizationService localizationService)
    {
        _narrationService = narrationService;
        _localizationService = localizationService;

        _localizationService.LanguageChanged += (_, _) => RefreshTranslations();
    }

    private void RefreshTranslations()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(LblAddress));
            OnPropertyChanged(nameof(LblFoodType));
            OnPropertyChanged(nameof(LblPrice));
            OnPropertyChanged(nameof(LblVisitTime));
            OnPropertyChanged(nameof(LblDescription));
            OnPropertyChanged(nameof(LblListenNarration));
            OnPropertyChanged(nameof(LblGetDirections));
        });
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
                _localizationService.GetString("CannotOpenMap"),
                "OK");
        }
    }
}
