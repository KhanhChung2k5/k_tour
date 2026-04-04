using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

public enum SubscriptionPlan
{
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
    void Clear();
}
