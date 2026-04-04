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
    private readonly IAnalyticsService _analytics;

    [ObservableProperty]
    private POI selectedPoi = new();

    /// <summary>
    /// Mô tả POI theo ngôn ngữ hiện tại.
    /// Tra trong Contents trước, fallback về Description (tiếng Việt).
    /// </summary>
    public string LocalizedDescription
    {
        get
        {
            var lang = _localizationService.CurrentLanguage;
            var content = SelectedPoi.Contents
                .FirstOrDefault(c => c.Language == lang && !string.IsNullOrWhiteSpace(c.TextContent));
            if (content != null) return content.TextContent!;

            // Fallback: thử "vi" trước
            if (lang != "vi")
            {
                var vi = SelectedPoi.Contents
                    .FirstOrDefault(c => c.Language == "vi" && !string.IsNullOrWhiteSpace(c.TextContent));
                if (vi != null) return vi.TextContent!;
            }

            return SelectedPoi.Description;
        }
    }

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
        ILocalizationService localizationService,
        IAnalyticsService analytics)
    {
        _narrationService = narrationService;
        _localizationService = localizationService;
        _analytics = analytics;

        _localizationService.LanguageChanged += (_, _) => RefreshTranslations();
    }

    partial void OnSelectedPoiChanged(POI value)
    {
        OnPropertyChanged(nameof(LocalizedDescription));
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
            OnPropertyChanged(nameof(LocalizedDescription));
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
        _analytics.RecordPOIVisit(SelectedPoi);
        _analytics.RecordNarration();
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
