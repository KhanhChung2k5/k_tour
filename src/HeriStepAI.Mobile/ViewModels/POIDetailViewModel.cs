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
    private readonly IApiService _apiService;

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

    private static readonly Dictionary<string, string> LangBadges = new()
    {
        ["vi"] = "VI", ["en"] = "EN", ["ko"] = "KO", ["zh"] = "ZH",
        ["ja"] = "JA", ["th"] = "TH", ["fr"] = "FR",
    };

    // Localized labels
    public string LblAddress => _localizationService.GetString("Address");
    public string LblFoodType => _localizationService.GetString("FoodType");
    public string LblPrice => _localizationService.GetString("Price");
    public string LblVisitTime => _localizationService.GetString("VisitTime");
    public string LblDescription => _localizationService.GetString("Description");
    public string LblIntro => _localizationService.GetString("IntroTitle");
    public string LblOpenStatus => _localizationService.GetString("OpenNow");
    public string LblAiNarrationTitle => _localizationService.GetString("AiNarrationTitle");
    public string LangBadge => $"🌐 {LangBadges.GetValueOrDefault(_localizationService.CurrentLanguage, "VI")}";

    /// <summary>Chip/heading: cập nhật khi đổi ngôn ngữ (POI không phát INotify).</summary>
    public string PoiCategoryDisplay => SelectedPoi.CategoryText;

    public string PoiFoodTypeDisplay => SelectedPoi.FoodTypeText;
    public string PoiEstimatedTimeDisplay => SelectedPoi.EstimatedTimeText;

    public string ReviewCountText =>
        string.Format(_localizationService.GetString("ReviewCountFormat"), SelectedPoi.ReviewCount);

    public string LblListenNarration => $"🔊 {_localizationService.GetString("ListenNarration")}";
    public string LblGetDirections => $"🗺️ {_localizationService.GetString("GetDirections")}";

    public POIDetailViewModel(
        INarrationService narrationService,
        ILocalizationService localizationService,
        IAnalyticsService analytics,
        IApiService apiService)
    {
        _narrationService = narrationService;
        _localizationService = localizationService;
        _analytics = analytics;
        _apiService = apiService;

        _localizationService.LanguageChanged += (_, _) => RefreshTranslations();
    }

    partial void OnSelectedPoiChanged(POI value)
    {
        OnPropertyChanged(nameof(LocalizedDescription));
        OnPropertyChanged(nameof(ReviewCountText));
        OnPropertyChanged(nameof(PoiCategoryDisplay));
        OnPropertyChanged(nameof(PoiFoodTypeDisplay));
        OnPropertyChanged(nameof(PoiEstimatedTimeDisplay));
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
            OnPropertyChanged(nameof(LblIntro));
            OnPropertyChanged(nameof(LblOpenStatus));
            OnPropertyChanged(nameof(LblAiNarrationTitle));
            OnPropertyChanged(nameof(LangBadge));
            OnPropertyChanged(nameof(LblListenNarration));
            OnPropertyChanged(nameof(LblGetDirections));
            OnPropertyChanged(nameof(LocalizedDescription));
            OnPropertyChanged(nameof(ReviewCountText));
            OnPropertyChanged(nameof(PoiCategoryDisplay));
            OnPropertyChanged(nameof(PoiFoodTypeDisplay));
            OnPropertyChanged(nameof(PoiEstimatedTimeDisplay));
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
        AppLog.Info($"🔵 POIDetail PlayNarration: {SelectedPoi.Name} (id={SelectedPoi.Id})");
        await _apiService.LogVisitAsync(SelectedPoi.Id, null, null, VisitType.MapClick);
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
