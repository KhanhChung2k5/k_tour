namespace HeriStepAI.Mobile.Services;

public interface ILocalizationService
{
    string CurrentLanguage { get; }
    bool IsVietnamese { get; }
    void SetLanguage(string languageCode);
    string GetString(string key);
    event EventHandler? LanguageChanged;
}
