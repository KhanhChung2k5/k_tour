using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeriStepAI.Mobile.Services;

namespace HeriStepAI.Mobile.ViewModels;

public record PlanOption(
    SubscriptionPlan Plan,
    string Label,
    string Price,
    string PriceNote,
    int Amount,        // VND amount for QR
    char PlanCode,     // W / M / Y  — appended to transfer ref
    string Badge);     // e.g. "PHỔ BIẾN"

public partial class SubscriptionViewModel : ObservableObject
{
    // ── Bank info — CHANGE THESE to your own account ─────────────────────
    private const string BankCode      = "ICB";                // Vietinbank
    private const string AccountNo     = "104879400502";
    private const string AccountName   = "CHUNG HOANG CONG KHANH";
    // ──────────────────────────────────────────────────────────────────────

    private readonly ISubscriptionService _subscription;
    private readonly ILocalizationService _localizationService;

    private static readonly string[] _langCycle = { "vi", "en", "ko", "zh", "ja", "th", "fr" };
    private static readonly Dictionary<string, string> _langLabels = new()
    {
        ["vi"] = "🇻🇳 VI", ["en"] = "🇬🇧 EN", ["ko"] = "🇰🇷 KO",
        ["zh"] = "🇨🇳 ZH", ["ja"] = "🇯🇵 JA", ["th"] = "🇹🇭 TH", ["fr"] = "🇫🇷 FR",
    };

    [ObservableProperty]
    private string currentLangLabel = "🇻🇳 VI";

    public List<PlanOption> Plans { get; } = new()
    {
        new(SubscriptionPlan.Daily,   "Gói Ngày",  "29.000 đ",   "/1 ngày",   29000,  'D', ""),
        new(SubscriptionPlan.Weekly,  "Gói Tuần",  "99.000 đ",   "/7 ngày",   99000,  'W', ""),
        new(SubscriptionPlan.Monthly, "Gói Tháng", "199.000 đ",  "/30 ngày",  199000, 'M', "PHỔ BIẾN"),
        new(SubscriptionPlan.Yearly,  "Gói Năm",   "999.000 đ",  "/365 ngày", 999000, 'Y', "TIẾT KIỆM"),
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QrImageUrl))]
    [NotifyPropertyChangedFor(nameof(TransferRef))]
    [NotifyPropertyChangedFor(nameof(TransferAmount))]
    [NotifyPropertyChangedFor(nameof(TransferNote))]
    [NotifyPropertyChangedFor(nameof(HasSelectedPlan))]
    private PlanOption? selectedPlan;

    [ObservableProperty]
    private bool isPaying;      // QR panel visible

    [ObservableProperty]
    private bool isConfirming;  // spinner while "activating"

    public bool HasSelectedPlan => SelectedPlan is not null;

    // ── Transfer content ─────────────────────────────────────────────────

    /// <summary>Unique reference per device + plan: e.g. HSAA3F9B2M</summary>
    public string TransferRef =>
        SelectedPlan is null
            ? ""
            : $"HSA{_subscription.DeviceKey}{SelectedPlan.PlanCode}";

    public string TransferAmount =>
        SelectedPlan is null ? "" : $"{SelectedPlan.Amount:N0} đ";

    public string TransferNote =>
        SelectedPlan is null
            ? ""
            : $"Nội dung CK: {TransferRef}";

    // ── VietQR dynamic URL ────────────────────────────────────────────────
    /// <summary>
    /// Generates a VietQR image URL that encodes the bank, account, amount,
    /// and unique transfer reference. Renders as a scannable QR image.
    /// Docs: https://www.vietqr.io/danh-sach-api/
    /// </summary>
    public string QrImageUrl
    {
        get
        {
            if (SelectedPlan is null) return string.Empty;
            var encoded = Uri.EscapeDataString(TransferRef);
            var nameEncoded = Uri.EscapeDataString(AccountName);
            return $"https://img.vietqr.io/image/{BankCode}-{AccountNo}-compact2.png" +
                   $"?amount={SelectedPlan.Amount}&addInfo={encoded}&accountName={nameEncoded}";
        }
    }

    // ── Expiry info (shown when still active but user opens page) ─────────
    public string ExpiryText =>
        _subscription.ExpiryDate.HasValue
            ? $"Hết hạn: {_subscription.ExpiryDate.Value.ToLocalTime():dd/MM/yyyy HH:mm}"
            : "";

    // ── Localized labels ─────────────────────────────────────────────────
    private string T(string key) => _localizationService.GetString(key);

    public string LblHeroSubtitle    => T("SubHeroSubtitle");
    public string LblHeroPrompt      => T("SubHeroPrompt");

    public string LblPlanDaily       => T("PlanDaily");
    public string LblPlanDailyDesc   => T("PlanDailyDesc");
    public string LblPlanDailyPeriod => T("PlanDailyPeriod");

    public string LblPlanWeekly       => T("PlanWeekly");
    public string LblPlanWeeklyDesc   => T("PlanWeeklyDesc");
    public string LblPlanWeeklyPeriod => T("PlanWeeklyPeriod");

    public string LblPlanMonthly       => T("PlanMonthly");
    public string LblPlanMonthlyDesc   => T("PlanMonthlyDesc");
    public string LblPlanMonthlyPeriod => T("PlanMonthlyPeriod");
    public string LblBadgePopular      => T("BadgePopular");

    public string LblPlanYearly       => T("PlanYearly");
    public string LblPlanYearlyDesc   => T("PlanYearlyDesc");
    public string LblPlanYearlyPeriod => T("PlanYearlyPeriod");
    public string LblBadgeSave        => T("BadgeSave");

    public string LblFeaturesTitle => T("SubFeaturesTitle");
    public string LblFeature1      => T("SubFeature1");
    public string LblFeature2      => T("SubFeature2");
    public string LblFeature3      => T("SubFeature3");
    public string LblFeature4      => T("SubFeature4");
    public string LblFooterNote    => T("SubFooterNote");

    public string LblPayTitle      => T("SubPayTitle");
    public string LblScanQR        => T("SubScanQR");
    public string LblOrManual      => T("SubOrManual");
    public string LblBank          => T("SubBank");
    public string LblAccountNo     => T("SubAccountNo");
    public string LblAmount        => T("SubAmount");
    public string LblTransferRef   => T("SubTransferRef");
    public string LblRefImportant  => T("SubRefImportant");
    public string LblWarning       => T("SubWarning");
    public string LblConfirmBtn    => T("SubConfirmBtn");
    public string LblConfirmNote   => T("SubConfirmNote");

    private void RefreshTranslations()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(LblHeroSubtitle));
            OnPropertyChanged(nameof(LblHeroPrompt));
            OnPropertyChanged(nameof(LblPlanDaily));
            OnPropertyChanged(nameof(LblPlanDailyDesc));
            OnPropertyChanged(nameof(LblPlanDailyPeriod));
            OnPropertyChanged(nameof(LblPlanWeekly));
            OnPropertyChanged(nameof(LblPlanWeeklyDesc));
            OnPropertyChanged(nameof(LblPlanWeeklyPeriod));
            OnPropertyChanged(nameof(LblPlanMonthly));
            OnPropertyChanged(nameof(LblPlanMonthlyDesc));
            OnPropertyChanged(nameof(LblPlanMonthlyPeriod));
            OnPropertyChanged(nameof(LblBadgePopular));
            OnPropertyChanged(nameof(LblPlanYearly));
            OnPropertyChanged(nameof(LblPlanYearlyDesc));
            OnPropertyChanged(nameof(LblPlanYearlyPeriod));
            OnPropertyChanged(nameof(LblBadgeSave));
            OnPropertyChanged(nameof(LblFeaturesTitle));
            OnPropertyChanged(nameof(LblFeature1));
            OnPropertyChanged(nameof(LblFeature2));
            OnPropertyChanged(nameof(LblFeature3));
            OnPropertyChanged(nameof(LblFeature4));
            OnPropertyChanged(nameof(LblFooterNote));
            OnPropertyChanged(nameof(LblPayTitle));
            OnPropertyChanged(nameof(LblScanQR));
            OnPropertyChanged(nameof(LblOrManual));
            OnPropertyChanged(nameof(LblBank));
            OnPropertyChanged(nameof(LblAccountNo));
            OnPropertyChanged(nameof(LblAmount));
            OnPropertyChanged(nameof(LblTransferRef));
            OnPropertyChanged(nameof(LblRefImportant));
            OnPropertyChanged(nameof(LblWarning));
            OnPropertyChanged(nameof(LblConfirmBtn));
            OnPropertyChanged(nameof(LblConfirmNote));
        });
    }

    public SubscriptionViewModel(ISubscriptionService subscription, ILocalizationService localizationService)
    {
        _subscription = subscription;
        _localizationService = localizationService;
        currentLangLabel = _langLabels.GetValueOrDefault(_localizationService.CurrentLanguage, "🇻🇳 VI");
        _localizationService.LanguageChanged += (_, _) => RefreshTranslations();
    }

    [RelayCommand]
    private void SwitchLanguage()
    {
        var current = _localizationService.CurrentLanguage;
        var idx = Array.IndexOf(_langCycle, current);
        var next = _langCycle[(idx + 1) % _langCycle.Length];
        _localizationService.SetLanguage(next);
        CurrentLangLabel = _langLabels.GetValueOrDefault(next, "🇻🇳 VI");
    }

    // ── Commands ──────────────────────────────────────────────────────────

    [RelayCommand]
    private void SelectPlan(PlanOption plan)
    {
        SelectedPlan = plan;
        IsPaying = true;
    }

    [RelayCommand]
    private void BackToPlans()
    {
        IsPaying = false;
        SelectedPlan = null;
    }

    /// <summary>
    /// Called when user taps "Tôi đã thanh toán".
    /// In a production app this would verify via API; here we activate locally.
    /// The unique TransferRef lets the admin correlate the bank transfer to this device.
    /// </summary>
    [RelayCommand]
    private async Task ConfirmPayment()
    {
        if (SelectedPlan is null) return;

        IsConfirming = true;
        await Task.Delay(1500); // simulate brief processing

        _subscription.Activate(SelectedPlan.Plan);
        IsConfirming = false;

        // Navigate to main shell
        Application.Current!.MainPage = App.Services!.GetRequiredService<AppShell>();
    }
}
