using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeriStepAI.Mobile.Services;

namespace HeriStepAI.Mobile.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
    private readonly IPOIService _poiService;
    private readonly ILocationService _locationService;
    private readonly ILocalizationService _localizationService;
    private readonly IVoicePreferenceService _voicePreference;

    [ObservableProperty]
    private string selectedLanguage = "Tiếng Việt";

    [ObservableProperty]
    private string selectedVoiceGender = "Nữ";

    [ObservableProperty]
    private bool isLocationEnabled;

    [ObservableProperty]
    private string gpsStatusText = "Đang kiểm tra...";

    [ObservableProperty]
    private bool isSyncing = false;

    public SettingsPageViewModel(
        IPOIService poiService,
        ILocationService locationService,
        ILocalizationService localizationService,
        IVoicePreferenceService voicePreference)
    {
        _poiService = poiService;
        _locationService = locationService;
        _localizationService = localizationService;
        _voicePreference = voicePreference;

        SelectedLanguage = _localizationService.IsVietnamese ? "Tiếng Việt" : "English";
        SelectedVoiceGender = _voicePreference.VoiceGender == VoiceGender.Male ? "Nam" : "Nữ";
        UpdateGpsStatus();
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        _localizationService.SetLanguage(GetLanguageCode());
    }

    partial void OnSelectedVoiceGenderChanged(string value)
    {
        var gender = value == "Nam" ? VoiceGender.Male : VoiceGender.Female;
        _voicePreference.SaveVoiceGender(gender);
    }

    private void UpdateGpsStatus()
    {
        IsLocationEnabled = _locationService.IsLocationEnabled;
        GpsStatusText = IsLocationEnabled
            ? _localizationService.GetString("LocationActive")
            : _localizationService.GetString("LocationOff");
    }

    public List<string> AvailableLanguages { get; } = new() { "Tiếng Việt", "English" };
    public List<string> AvailableVoiceGenders { get; } = new() { "Nam", "Nữ" };

    public string GetLanguageCode() => SelectedLanguage == "English" ? "en" : "vi";

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
