using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeriStepAI.Mobile.Services;

namespace HeriStepAI.Mobile.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
    private readonly IPOIService _poiService;
    private readonly ILocationService _locationService;

    [ObservableProperty]
    private string selectedLanguage = "Tiếng Việt";

    [ObservableProperty]
    private bool isLocationEnabled;

    [ObservableProperty]
    private string gpsStatusText = "Đang kiểm tra...";

    [ObservableProperty]
    private bool isSyncing = false;

    public SettingsPageViewModel(IPOIService poiService, ILocationService locationService)
    {
        _poiService = poiService;
        _locationService = locationService;
        
        UpdateGpsStatus();
    }

    private void UpdateGpsStatus()
    {
        IsLocationEnabled = _locationService.IsLocationEnabled;
        GpsStatusText = IsLocationEnabled 
            ? "Vị trí đang hoạt động" 
            : "Vị trí bị tắt";
    }

    public List<string> AvailableLanguages { get; } = new() 
    { 
        "Tiếng Việt", 
        "English", 
        "中文", 
        "日本語", 
        "한국어" 
    };

    public string GetLanguageCode() => SelectedLanguage switch
    {
        "English" => "en",
        "中文" => "zh",
        "日本語" => "ja",
        "한국어" => "ko",
        _ => "vi"
    };

    [RelayCommand]
    private async Task SyncData()
    {
        if (IsSyncing) return;

        try
        {
            IsSyncing = true;
            await _poiService.SyncPOIsFromServerAsync();
            await Shell.Current.DisplayAlert("Thành công", "Đã đồng bộ dữ liệu thành công!", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Sync error: {ex.Message}");
            await Shell.Current.DisplayAlert("Lỗi", $"Không thể đồng bộ dữ liệu: {ex.Message}", "OK");
        }
        finally
        {
            IsSyncing = false;
        }
    }
}
