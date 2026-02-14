namespace HeriStepAI.Mobile.Services;

public class LocalizationService : ILocalizationService
{
    private string _currentLanguage = "vi";
    private const string PreferenceKey = "AppLanguage";

    public string CurrentLanguage => _currentLanguage;
    public bool IsVietnamese => _currentLanguage == "vi";

    public event EventHandler? LanguageChanged;

    private static readonly HashSet<string> SupportedLanguages = new()
        { "vi", "en", "ko", "zh", "ja", "th", "fr" };

    public LocalizationService()
    {
        try
        {
            var saved = Preferences.Get(PreferenceKey, "vi");
            if (SupportedLanguages.Contains(saved))
                _currentLanguage = saved;
        }
        catch { }
    }

    public void SetLanguage(string languageCode)
    {
        var lang = SupportedLanguages.Contains(languageCode) ? languageCode : "vi";
        if (_currentLanguage == lang) return;
        _currentLanguage = lang;
        try { Preferences.Set(PreferenceKey, lang); } catch { }
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string GetString(string key)
    {
        return Keys.TryGetValue(key, out var pair)
            ? (_currentLanguage == "en" ? pair.en : pair.vi)
            : key;
    }

    private static readonly Dictionary<string, (string vi, string en)> Keys = new()
    {
        ["AppTitle"] = ("HERISTEP AI", "HERISTEP AI"),
        ["AppSubtitle"] = ("ĐÀ NẴNG", "DA NANG"),
        ["Welcome"] = ("Chào mừng bạn đến với", "Welcome to"),
        ["AppName"] = ("HeriStepAI", "HeriStepAI"),
        ["AppTagline"] = ("Ứng dụng thuyết minh tự động cho chuyến đi của bạn", "Auto narration app for your trip"),
        ["VoiceNarration"] = ("Giọng thuyết minh", "Narration voice"),
        ["Male"] = ("Nam", "Male"),
        ["Female"] = ("Nữ", "Female"),
        ["RegionNorth"] = ("Miền Bắc", "North"),
        ["RegionCentral"] = ("Miền Trung", "Central"),
        ["RegionSouth"] = ("Miền Nam", "South"),
        ["ChooseTour"] = ("Chọn Tour", "Choose Tour"),
        ["CreateNewTour"] = ("Tạo Tour mới", "Create New Tour"),
        ["RecentTours"] = ("Tour gần đây", "Recent Tours"),
        ["Minutes"] = ("phút", "min"),
        ["Points"] = ("điểm", "points"),
        ["Home"] = ("Trang chủ", "Home"),
        ["Map"] = ("Bản đồ", "Map"),
        ["Places"] = ("Địa điểm", "Places"),
        ["Settings"] = ("Cài đặt", "Settings"),
        ["Search"] = ("Tìm kiếm...", "Search..."),
        ["NearbyPlaces"] = ("Địa điểm gần bạn", "Nearby places"),
        ["ListenNarration"] = ("Nghe thuyết minh", "Listen"),
        ["GetDirections"] = ("Chỉ đường", "Directions"),
        ["SettingsTitle"] = ("Cài đặt", "Settings"),
        ["SettingsSubtitle"] = ("Tùy chỉnh ứng dụng của bạn", "Customize your app"),
        ["Narration"] = ("Thuyết minh", "Narration"),
        ["NarrationLanguage"] = ("Ngôn ngữ thuyết minh", "Narration language"),
        ["NarrationLanguageHint"] = ("Chọn ngôn ngữ cho audio", "Select language for audio"),
        ["Location"] = ("Vị trí", "Location"),
        ["GpsStatus"] = ("Trạng thái GPS", "GPS Status"),
        ["LocationActive"] = ("Vị trí đang hoạt động", "Location is active"),
        ["LocationOff"] = ("Vị trí bị tắt", "Location is off"),
        ["Info"] = ("Thông tin", "Info"),
        ["Version"] = ("Phiên bản", "Version"),
        ["ContactSupport"] = ("Liên hệ hỗ trợ", "Contact support"),
        ["SyncData"] = ("Đồng bộ dữ liệu", "Sync data"),
        ["Success"] = ("Thành công", "Success"),
        ["SyncSuccess"] = ("Đã đồng bộ dữ liệu thành công!", "Data synced successfully!"),
        ["Error"] = ("Lỗi", "Error"),
        ["SyncError"] = ("Không thể đồng bộ dữ liệu", "Could not sync data"),
        ["CreateTour"] = ("Tạo Tour", "Create Tour"),
        ["CreateTourComingSoon"] = ("Tính năng tạo tour mới sẽ sớm có mặt!", "New tour creation coming soon!"),
        ["MapError"] = ("Không thể mở ứng dụng bản đồ", "Could not open map app"),
        ["GettingLocation"] = ("Đang lấy vị trí...", "Getting location..."),
        ["GpsNotEnabled"] = ("GPS chưa bật. Bật vị trí trong Cài đặt để dùng geofence.", "GPS is off. Enable location in Settings for geofence."),
        ["LanguageVi"] = ("Tiếng Việt", "Vietnamese"),
        ["LanguageEn"] = ("English", "English"),
    };
}
