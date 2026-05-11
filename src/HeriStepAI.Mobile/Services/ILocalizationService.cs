namespace HeriStepAI.Mobile.Services;

/// <summary>Dịch vụ localize</summary>
public interface ILocalizationService
{
    string CurrentLanguage { get; }
    /// <summary>Kiểm tra ngôn ngữ hiện tại</summary>
    bool IsVietnamese { get; }
    /// <summary>Thiết lập ngôn ngữ hiện tại</summary>
    void SetLanguage(string languageCode);
    /// <summary>Lấy văn bản theo ngôn ngữ hiện tại</summary>
    string GetString(string key);
    event EventHandler? LanguageChanged;
}
