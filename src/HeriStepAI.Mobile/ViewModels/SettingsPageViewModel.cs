using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeriStepAI.Mobile.Services;
using HeriStepAI.Mobile.Views;

namespace HeriStepAI.Mobile.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
    private readonly IPOIService _poiService;
    private readonly ILocationService _locationService;
    private readonly ILocalizationService _localizationService;
    private readonly IVoicePreferenceService _voicePreference;
    private readonly IAuthService _authService;

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
    public string LblLogout => _localizationService.GetString("Logout");

    [ObservableProperty]
    private string accountDisplayName = "";

    [ObservableProperty]
    private string accountEmail = "";

    public SettingsPageViewModel(
        IPOIService poiService,
        ILocationService locationService,
        ILocalizationService localizationService,
        IVoicePreferenceService voicePreference,
        IAuthService authService)
    {
        _poiService = poiService;
        _locationService = locationService;
        _localizationService = localizationService;
        _voicePreference = voicePreference;
        _authService = authService;

        _localizationService.LanguageChanged += (_, _) => RefreshTranslations();
        _authService.UserProfileUpdated += (_, _) => MainThread.BeginInvokeOnMainThread(RefreshAccountInfo);

        SelectedLanguage = LanguageCodeToDisplay(_localizationService.CurrentLanguage);
        SelectedVoiceGender = _voicePreference.VoiceGender == VoiceGender.Male
            ? _localizationService.GetString("Male")
            : _localizationService.GetString("Female");
        UpdateGpsStatus();
        RefreshAccountInfo();
    }

    /// <summary>Gọi khi màn Cài đặt hiển thị để load lại tên/email từ session.</summary>
    public void RefreshAccountInfo()
    {
        var user = _authService.CurrentUser;
        if (user != null)
        {
            AccountDisplayName = !string.IsNullOrWhiteSpace(user.FullName)
                ? user.FullName
                : !string.IsNullOrWhiteSpace(user.Username)
                    ? user.Username
                    : _localizationService.GetString("LoggedIn"); // "Đã đăng nhập"
            AccountEmail = !string.IsNullOrWhiteSpace(user.Email) ? user.Email : "—";
        }
        else
        {
            AccountDisplayName = "—";
            AccountEmail = "—";
        }
    }

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
            OnPropertyChanged(nameof(LblLogout));
            RefreshAccountInfo();
            // Refresh voice gender picker
            OnPropertyChanged(nameof(AvailableVoiceGenders));
            UpdateGpsStatus();
        });
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        _localizationService.SetLanguage(GetLanguageCode());
    }

    partial void OnSelectedVoiceGenderChanged(string value)
    {
        var maleText = _localizationService.GetString("Male");
        var gender = value == maleText ? VoiceGender.Male : VoiceGender.Female;
        _voicePreference.SaveVoiceGender(gender);
    }

    private void UpdateGpsStatus()
    {
        IsLocationEnabled = _locationService.IsLocationEnabled;
        GpsStatusText = IsLocationEnabled
            ? _localizationService.GetString("LocationActive")
            : _localizationService.GetString("LocationOff");
    }

    public List<string> AvailableLanguages { get; } = new()
    {
        "Tiếng Việt", "English", "한국어", "中文", "日本語", "ภาษาไทย", "Français"
    };
    public List<string> AvailableVoiceGenders => new()
    {
        _localizationService.GetString("Male"),
        _localizationService.GetString("Female")
    };

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
    private async Task Logout()
    {
        await _authService.LogoutAsync();
        if (Application.Current != null && Application.Current.MainPage != null)
        {
            var authPage = IPlatformApplication.Current?.Services?.GetRequiredService<AuthPage>();
            if (authPage != null)
                Application.Current.MainPage = authPage;
        }
    }

    [RelayCommand]
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
