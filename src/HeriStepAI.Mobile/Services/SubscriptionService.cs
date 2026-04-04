using System.Security.Cryptography;
using System.Text;

namespace HeriStepAI.Mobile.Services;

/// <summary>
/// Manages subscription state stored in SecureStorage (encrypted on-device).
/// No server required — works offline. Expiry is enforced locally.
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private const string KeyPlan   = "sub_plan";
    private const string KeyExpiry = "sub_expiry";
    private const string KeyDevice = "sub_device_key";

    // ── Public API ──────────────────────────────────────────────────────────

    public bool IsActive
    {
        get
        {
            if (ExpiryDate is not DateTime expiry) return false;
            return DateTime.UtcNow < expiry;
        }
    }

    public SubscriptionPlan? CurrentPlan
    {
        get
        {
            var raw = SecureStorage.Default.GetAsync(KeyPlan).GetAwaiter().GetResult();
            if (Enum.TryParse<SubscriptionPlan>(raw, out var plan)) return plan;
            return null;
        }
    }

    public DateTime? ExpiryDate
    {
        get
        {
            var raw = SecureStorage.Default.GetAsync(KeyExpiry).GetAwaiter().GetResult();
            if (DateTime.TryParse(raw, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                return dt;
            return null;
        }
    }

    /// <summary>
    /// Stable 6-character uppercase hex key unique to this device install.
    /// Stored permanently so it survives subscription cycles.
    /// </summary>
    public string DeviceKey
    {
        get
        {
            var stored = SecureStorage.Default.GetAsync(KeyDevice).GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(stored)) return stored;

            // Generate a new key from device info + random guid
            var raw = $"{DeviceInfo.Current.Name}|{DeviceInfo.Current.Model}|{DeviceInfo.Platform}|{Guid.NewGuid()}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            var key  = Convert.ToHexString(hash).Substring(0, 6).ToUpper();
            SecureStorage.Default.SetAsync(KeyDevice, key).GetAwaiter().GetResult();
            return key;
        }
    }

    // ── Mutation ─────────────────────────────────────────────────────────────

    public void Activate(SubscriptionPlan plan)
    {
        var days = plan switch
        {
            SubscriptionPlan.Weekly  => 7,
            SubscriptionPlan.Monthly => 30,
            SubscriptionPlan.Yearly  => 365,
            _                        => 7
        };

        var expiry = DateTime.UtcNow.AddDays(days).ToString("O"); // ISO round-trip
        SecureStorage.Default.SetAsync(KeyPlan,   plan.ToString()).GetAwaiter().GetResult();
        SecureStorage.Default.SetAsync(KeyExpiry, expiry).GetAwaiter().GetResult();
    }

    public void Clear()
    {
        SecureStorage.Default.Remove(KeyPlan);
        SecureStorage.Default.Remove(KeyExpiry);
    }
}
