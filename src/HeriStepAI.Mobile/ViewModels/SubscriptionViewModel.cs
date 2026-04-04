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

    public List<PlanOption> Plans { get; } = new()
    {
        new(SubscriptionPlan.Weekly,  "Gói Tuần",  "29.000 đ",  "/7 ngày",   29000, 'W', ""),
        new(SubscriptionPlan.Monthly, "Gói Tháng", "79.000 đ",  "/30 ngày",  79000, 'M', "PHỔ BIẾN"),
        new(SubscriptionPlan.Yearly,  "Gói Năm",   "499.000 đ", "/365 ngày", 499000,'Y', "TIẾT KIỆM"),
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

    public SubscriptionViewModel(ISubscriptionService subscription)
    {
        _subscription = subscription;
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
