using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeriStepAI.Mobile.Models;
using HeriStepAI.Mobile.Services;

namespace HeriStepAI.Mobile.ViewModels;

[QueryProperty(nameof(Tour), "Tour")]
public partial class TourDetailViewModel : ObservableObject
{
    private readonly ITourSelectionService _tourSelectionService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private Tour tour = new();

    // Localized labels
    public string LblPOIList => _localizationService.GetString("POIList");
    public string LblStartTour => $"🗺️ {_localizationService.GetString("StartTour")}";

    public string TotalPriceRange
    {
        get
        {
            if (Tour?.POIs == null || Tour.POIs.Count == 0)
                return "N/A";

            var totalMin = Tour.POIs.Sum(p => p.PriceMin);
            var totalMax = Tour.POIs.Sum(p => p.PriceMax);

            if (totalMin == 0 && totalMax == 0)
                return "N/A";

            return $"{totalMin:N0} - {totalMax:N0}đ";
        }
    }

    public TourDetailViewModel(
        ITourSelectionService tourSelectionService,
        ILocalizationService localizationService)
    {
        _tourSelectionService = tourSelectionService;
        _localizationService = localizationService;

        _localizationService.LanguageChanged += (_, _) => RefreshTranslations();
    }

    private void RefreshTranslations()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(LblPOIList));
            OnPropertyChanged(nameof(LblStartTour));
        });
    }

    partial void OnTourChanged(Tour value)
    {
        OnPropertyChanged(nameof(TotalPriceRange));
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task SelectPOI(POI poi)
    {
        // Navigate to POI detail
        await Shell.Current.GoToAsync($"POIDetailPage", new Dictionary<string, object>
        {
            { "POI", poi }
        });
    }

    [RelayCommand]
    private async Task StartTour()
    {
        _tourSelectionService.SelectedTour = Tour;
        await Shell.Current.GoToAsync("//MapPage");
    }
}
