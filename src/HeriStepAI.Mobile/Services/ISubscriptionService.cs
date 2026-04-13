using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

public enum SubscriptionPlan
{
    Daily,    // 1 day
    Weekly,   // 7 days
    Monthly,  // 30 days
    Yearly    // 365 days
}

public interface ISubscriptionService
{
    bool IsActive { get; }
    SubscriptionPlan? CurrentPlan { get; }
    DateTime? ExpiryDate { get; }
    string DeviceKey { get; }

    void Activate(SubscriptionPlan plan);

    /// <summary>Kích hoạt theo ngày hết hạn do server gán sau khi Admin duyệt CK.</summary>
    void ActivateFromServer(SubscriptionPlan plan, DateTime expiresAtUtc);

    void Clear();
}
