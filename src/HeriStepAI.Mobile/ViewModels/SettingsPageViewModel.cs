using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeriStepAI.Mobile.Services;
using System.Collections.ObjectModel;

namespace HeriStepAI.Mobile.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
    private readonly IPOIService _poiService;
    private readonly ILocationService _locationService;
    private readonly ILocalizationService _localizationService;
    private readonly IVoicePreferenceService _voicePreference;
    private readonly IAnalyticsService _analytics;

    [ObservableProperty]
    private string selectedLanguage = "Tiếng Việt";

    [ObservableProperty]
    private string selectedVoiceGender = "Nữ";

    [ObservableProperty]
    private bool isLocationEnabled;

    [ObservableProperty]
    private string gpsStatusText = "";

    [ObservableProperty]
    private bool isSyncing = false;

    // Localized labels
    public string LblSettingsTitle => _localizationService.GetString("SettingsTitle");
    public string LblSettingsSubtitle => _localizationService.GetString("SettingsSubtitle");
    public string LblNarration => _localizationService.GetString("Narration");
    public string LblNarrationLanguage => _localizationService.GetString("NarrationLanguage");
    public string LblNarrationLanguageHint => _localizationService.GetString("NarrationLanguageHint");
    public string LblVoiceGender => _localizationService.GetString("VoiceGender");
    public string LblVoiceGenderHint => _localizationService.GetString("VoiceGenderHint");
    public string LblLocation => _localizationService.GetString("Location");
    public string LblGpsStatus => _localizationService.GetString("GpsStatus");
    public string LblInfo => _localizationService.GetString("Info");
    public string LblVersion => _localizationService.GetString("Version");
    public string LblContactSupport => _localizationService.GetString("ContactSupport");
    public string LblSyncData => _localizationService.GetString("SyncData");
    public string LblAccount => _localizationService.GetString("Account");

    // ── Profile labels ──
    public string LblProfileTitle => _localizationService.GetString("ProfileTitle");
    public string LblGuestName => _localizationService.GetString("GuestName");
    public string LblGuestTitle => _localizationService.GetString("GuestTitle");
    public string LblLevelProgress => _localizationService.GetString("LevelProgress");
    public string LblBadgesEarned => _localizationService.GetString("BadgesEarned");
    public string LblBadgeChampion => _localizationService.GetString("BadgeChampion");
    public string LblBadgeExplorer => _localizationService.GetString("BadgeExplorer");
    public string LblBadgeLegend => _localizationService.GetString("BadgeLegend");
    public string LblNarrationNotification => _localizationService.GetString("NarrationNotification");
    public string LblVolumeVoice => _localizationService.GetString("VolumeVoice");

    // ── Stats section labels ──
    public string LblStatsSection => _localizationService.GetString("StatsSection");
    public string LblShopsVisitedLabel => _localizationService.GetString("LblShopsVisited");
    public string LblDistanceLabel => _localizationService.GetString("LblDistance");
    public string LblToursCompletedLabel => _localizationService.GetString("LblToursCompleted");
    public string LblNarrationCountLabel => _localizationService.GetString("LblNarrationCount");
    public string LblWeeklyActivityTitle => _localizationService.GetString("WeeklyActivityTitle");
    public string LblSevenDays => _localizationService.GetString("SevenDays");
    public string LblTopPlacesTitle => _localizationService.GetString("TopPlacesTitle");
    public string LblNoDataYet => _localizationService.GetString("NoDataYet");

    // ── Week day abbreviations ──
    public string LblDayMon => _localizationService.GetString("DayMon");
    public string LblDayTue => _localizationService.GetString("DayTue");
    public string LblDayWed => _localizationService.GetString("DayWed");
    public string LblDayThu => _localizationService.GetString("DayThu");
    public string LblDayFri => _localizationService.GetString("DayFri");
    public string LblDaySat => _localizationService.GetString("DaySat");
    public string LblDaySun => _localizationService.GetString("DaySun");

    // Analytics stats — from LocalAnalyticsService (Preferences-backed)
    public string ShopsVisited => _analytics.ShopsVisited.ToString();
    public string TotalDistanceText => _analytics.TotalDistanceMeters >= 1000
        ? $"{_analytics.TotalDistanceMeters / 1000:F1} km"
        : $"{(int)_analytics.TotalDistanceMeters} m";
    public string ToursCompleted => _analytics.ToursCompleted.ToString();
    public string NarrationCount => _analytics.NarrationCount.ToString();

    // Weekly bar chart heights (0–55 px, proportional to max day)
    public double BarMonHeight => GetBarHeight(0);
    public double BarTueHeight => GetBarHeight(1);
    public double BarWedHeight => GetBarHeight(2);
    public double BarThuHeight => GetBarHeight(3);
    public double BarFriHeight => GetBarHeight(4);
    public double BarSatHeight => GetBarHeight(5);
    public double BarSunHeight => GetBarHeight(6);

    // Today's column uses Accent color, others use Primary
    public Color BarMonColor => GetBarColor(0);
    public Color BarTueColor => GetBarColor(1);
    public Color BarWedColor => GetBarColor(2);
    public Color BarThuColor => GetBarColor(3);
    public Color BarFriColor => GetBarColor(4);
    public Color BarSatColor => GetBarColor(5);
    public Color BarSunColor => GetBarColor(6);

    // Top POIs — display wrapper để format VisitCountText theo ngôn ngữ
    public ObservableCollection<POIVisitDisplayItem> TopPOIsList { get; } = new();

    private double GetBarHeight(int dayIndex)
    {
        var weekly = _analytics.WeeklyActivity;
        int max = weekly.Max();
        if (max == 0) return 5;
        double height = (weekly[dayIndex] / (double)max) * 55;
        return Math.Max(5, height);
    }

    private Color GetBarColor(int dayIndex)
    {
        int todayIndex = ((int)DateTime.Now.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return dayIndex == todayIndex
            ? Color.FromArgb("#C4A24A")  // Accent
            : Color.FromArgb("#2C2416"); // Primary
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public SettingsPageViewModel(
        IPOIService poiService,
        ILocationService locationService,
        ILocalizationService localizationService,
        IVoicePreferenceService voicePreference,
        IAnalyticsService analytics)
    {
        _poiService = poiService;
        _locationService = locationService;
        _localizationService = localizationService;
        _voicePreference = voicePreference;
        _analytics = analytics;
        RefreshTopPOIs();

        _localizationService.LanguageChanged += (_, _) => RefreshTranslations();

        SelectedLanguage = LanguageCodeToDisplay(_localizationService.CurrentLanguage);
        SelectedVoiceGender = _voicePreference.VoiceGender == VoiceGender.Male
            ? _localizationService.GetString("Male")
            : _localizationService.GetString("Female");
        UpdateGpsStatus();
    }

    /// <summary>
    /// Cập nhật các thuộc tính của Analytics.
    /// </summary>
    public void RefreshAnalytics()
    {
        OnPropertyChanged(nameof(ShopsVisited));
        OnPropertyChanged(nameof(TotalDistanceText));
        OnPropertyChanged(nameof(ToursCompleted));
        OnPropertyChanged(nameof(NarrationCount));
        OnPropertyChanged(nameof(BarMonHeight)); OnPropertyChanged(nameof(BarMonColor));
        OnPropertyChanged(nameof(BarTueHeight)); OnPropertyChanged(nameof(BarTueColor));
        OnPropertyChanged(nameof(BarWedHeight)); OnPropertyChanged(nameof(BarWedColor));
        OnPropertyChanged(nameof(BarThuHeight)); OnPropertyChanged(nameof(BarThuColor));
        OnPropertyChanged(nameof(BarFriHeight)); OnPropertyChanged(nameof(BarFriColor));
        OnPropertyChanged(nameof(BarSatHeight)); OnPropertyChanged(nameof(BarSatColor));
        OnPropertyChanged(nameof(BarSunHeight)); OnPropertyChanged(nameof(BarSunColor));
        RefreshTopPOIs();
    }

    /// <summary>
    /// Cập nhật danh sách TOP 10 POI được ghé nhiều nhất.
    /// </summary>
    private void RefreshTopPOIs()
    {
        var fmt = _localizationService.GetString("VisitCountFmt");
        var top = _analytics.TopPOIs;
        TopPOIsList.Clear();
        foreach (var item in top)
            TopPOIsList.Add(new POIVisitDisplayItem(
                item.Name,
                string.Format(fmt, item.VisitCount),
                item.Rating));
    }

    /// <summary>
    /// Cập nhật các thuộc tính khi ngôn ngữ thay đổi.
    /// </summary>
    private void RefreshTranslations()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(LblSettingsTitle));
            OnPropertyChanged(nameof(LblSettingsSubtitle));
            OnPropertyChanged(nameof(LblNarration));
            OnPropertyChanged(nameof(LblNarrationLanguage));
            OnPropertyChanged(nameof(LblNarrationLanguageHint));
            OnPropertyChanged(nameof(LblVoiceGender));
            OnPropertyChanged(nameof(LblVoiceGenderHint));
            OnPropertyChanged(nameof(LblLocation));
            OnPropertyChanged(nameof(LblGpsStatus));
            OnPropertyChanged(nameof(LblInfo));
            OnPropertyChanged(nameof(LblVersion));
            OnPropertyChanged(nameof(LblContactSupport));
            OnPropertyChanged(nameof(LblSyncData));
            OnPropertyChanged(nameof(LblAccount));
            // Profile section
            OnPropertyChanged(nameof(LblProfileTitle));
            OnPropertyChanged(nameof(LblGuestName));
            OnPropertyChanged(nameof(LblGuestTitle));
            OnPropertyChanged(nameof(LblLevelProgress));
            OnPropertyChanged(nameof(LblBadgesEarned));
            OnPropertyChanged(nameof(LblBadgeChampion));
            OnPropertyChanged(nameof(LblBadgeExplorer));
            OnPropertyChanged(nameof(LblBadgeLegend));
            OnPropertyChanged(nameof(LblNarrationNotification));
            OnPropertyChanged(nameof(LblVolumeVoice));
            // Stats section
            OnPropertyChanged(nameof(LblStatsSection));
            OnPropertyChanged(nameof(LblShopsVisitedLabel));
            OnPropertyChanged(nameof(LblDistanceLabel));
            OnPropertyChanged(nameof(LblToursCompletedLabel));
            OnPropertyChanged(nameof(LblNarrationCountLabel));
            OnPropertyChanged(nameof(LblWeeklyActivityTitle));
            OnPropertyChanged(nameof(LblSevenDays));
            OnPropertyChanged(nameof(LblTopPlacesTitle));
            OnPropertyChanged(nameof(LblNoDataYet));
            // Day abbreviations
            OnPropertyChanged(nameof(LblDayMon));
            OnPropertyChanged(nameof(LblDayTue));
            OnPropertyChanged(nameof(LblDayWed));
            OnPropertyChanged(nameof(LblDayThu));
            OnPropertyChanged(nameof(LblDayFri));
            OnPropertyChanged(nameof(LblDaySat));
            OnPropertyChanged(nameof(LblDaySun));
            // Refresh voice gender picker + top POIs (format changes with lang)
            OnPropertyChanged(nameof(AvailableVoiceGenders));
            RefreshTopPOIs();
            UpdateGpsStatus();
        });
    }

    /// <summary>
    /// Cập nhật ngôn ngữ hiện tại.
    /// </summary>
    partial void OnSelectedLanguageChanged(string value)
    {
        _localizationService.SetLanguage(GetLanguageCode());
    }

    /// <summary>
    /// Cập nhật giọng đọc hiện tại.
    /// </summary>
    partial void OnSelectedVoiceGenderChanged(string value)
    {
        var maleText = _localizationService.GetString("Male");
        var gender = value == maleText ? VoiceGender.Male : VoiceGender.Female;
        _voicePreference.SaveVoiceGender(gender);
    }

    /// <summary>
    /// Cập nhật trạng thái GPS hiện tại.
    /// </summary>
    private void UpdateGpsStatus()
    {
        IsLocationEnabled = _locationService.IsLocationEnabled;
        GpsStatusText = IsLocationEnabled
            ? _localizationService.GetString("LocationActive")
            : _localizationService.GetString("LocationOff");
    }

    /// <summary>
    /// Danh sách ngôn ngữ có sẵn.
    /// </summary>
    public List<string> AvailableLanguages { get; } = new()
    {
        "Tiếng Việt", "English", "한국어", "中文", "日本語", "ภาษาไทย", "Français"
    };

    /// <summary>
    /// Danh sách giọng đọc có sẵn.
    /// </summary>
    public List<string> AvailableVoiceGenders => new()
    {
        _localizationService.GetString("Male"),
        _localizationService.GetString("Female")
    };

    /// <summary>
    /// Chuyển đổi từ ngôn ngữ hiện tại sang mã ngôn ngữ.
    /// </summary>
    private static readonly Dictionary<string, string> DisplayToCode = new()
    {
        ["Tiếng Việt"] = "vi",
        ["English"] = "en",
        ["한국어"] = "ko",
        ["中文"] = "zh",
        ["日本語"] = "ja",
        ["ภาษาไทย"] = "th",
        ["Français"] = "fr",
    };

    public string GetLanguageCode() => DisplayToCode.GetValueOrDefault(SelectedLanguage, "vi");

    private static string LanguageCodeToDisplay(string code) => code switch
    {
        "en" => "English",
        "ko" => "한국어",
        "zh" => "中文",
        "ja" => "日本語",
        "th" => "ภาษาไทย",
        "fr" => "Français",
        _ => "Tiếng Việt",
    };

    [RelayCommand]

    /// <summary>
    /// Đồng bộ dữ liệu từ server.
    /// </summary>
    private async Task SyncData()
    {
        if (IsSyncing) return;

        try
        {
            IsSyncing = true;
            await _poiService.SyncPOIsFromServerAsync();
            await Shell.Current.DisplayAlert(_localizationService.GetString("Success"), _localizationService.GetString("SyncSuccess"), "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Sync error: {ex.Message}");
            await Shell.Current.DisplayAlert(_localizationService.GetString("Error"), $"{_localizationService.GetString("SyncError")}: {ex.Message}", "OK");
        }
        finally
        {
            IsSyncing = false;
        }
    }
}

/// <summary>Display wrapper for a POI visit record with localized visit count text.</summary>
public class POIVisitDisplayItem
{
    public string Name { get; init; } = "";
    public string VisitCountText { get; init; } = "";
    public double Rating { get; init; }

    public POIVisitDisplayItem(string name, string visitCountText, double rating)
    {
        Name = name;
        VisitCountText = visitCountText;
        Rating = rating;
    }
}
