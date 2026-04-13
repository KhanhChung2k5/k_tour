using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

public interface IApiService
{
    Task<List<POI>?> GetAllPOIsAsync();
    Task LogVisitAsync(int poiId, double? latitude, double? longitude, VisitType visitType);

    /// <summary>Gửi báo thanh toán lên server để Admin đối soát CK.</summary>
    /// <returns>true nếu server trả 2xx.</returns>
    Task<bool> ReportSubscriptionPaymentAsync(SubscriptionPaymentReport report);

    /// <summary>Trạng thái gói theo DeviceKey (sau khi Admin xác nhận CK).</summary>
    Task<SubscriptionEntitlementDto?> GetSubscriptionEntitlementAsync(string deviceKey);
}

public sealed class SubscriptionEntitlementDto
{
    public string Status { get; set; } = "";
    public string? PlanCode { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}

public sealed class SubscriptionPaymentReport
{
    public string DeviceKey { get; init; } = "";
    public string TransferRef { get; init; } = "";
    public string PlanCode { get; init; } = "";
    public string? PlanLabel { get; init; }
    public int AmountVnd { get; init; }
    public DateTime? SubscriptionExpiresAtUtc { get; init; }
    public string? Platform { get; init; }
}

public enum VisitType
{
    Geofence = 1,
    MapClick = 2,
    QRCode = 3
}
