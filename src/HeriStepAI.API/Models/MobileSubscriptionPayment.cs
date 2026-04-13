using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStepAI.API.Models;

/// <summary>
/// Báo thanh toán gói từ app (khách CK theo VietQR + nội dung HSA...).
/// Admin đối soát với sao kê ngân hàng và xác nhận / từ chối.
/// </summary>
[Table("MobileSubscriptionPayments")]
public class MobileSubscriptionPayment
{
    public int Id { get; set; }

    [MaxLength(16)]
    public string DeviceKey { get; set; } = "";

    /// <summary>Mã nội dung CK unique theo device + gói, vd. HSA3F2B1M</summary>
    [MaxLength(64)]
    public string TransferRef { get; set; } = "";

    [MaxLength(8)]
    public string PlanCode { get; set; } = "";

    [MaxLength(64)]
    public string? PlanLabel { get; set; }

    public int AmountVnd { get; set; }

    /// <summary>Thời điểm hết hạn gói trên máy (UTC), để đối soát thời hạn.</summary>
    public DateTime? SubscriptionExpiresAtUtc { get; set; }

    [MaxLength(32)]
    public string? Platform { get; set; }

    public PaymentReconciliationStatus Status { get; set; } = PaymentReconciliationStatus.Pending;

    public DateTime ReportedAtUtc { get; set; }

    public DateTime? VerifiedAtUtc { get; set; }

    public int? VerifiedByUserId { get; set; }

    [MaxLength(500)]
    public string? AdminNote { get; set; }
}
