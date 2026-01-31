using CommunityToolkit.Mvvm.ComponentModel;
using HeriStepAI.Mobile.Services;

namespace HeriStepAI.Mobile.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
    private readonly IPOIService _poiService;

    [ObservableProperty]
    private string selectedLanguage = "vi";

    [ObservableProperty]
    private bool isLocationEnabled;

    public SettingsPageViewModel(IPOIService poiService, ILocationService locationService)
    {
        _poiService = poiService;
        IsLocationEnabled = locationService.IsLocationEnabled;
    }

    public List<string> AvailableLanguages { get; } = new() { "vi", "en", "zh", "ja", "ko" };
}
