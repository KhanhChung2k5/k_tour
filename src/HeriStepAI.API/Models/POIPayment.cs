using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStepAI.API.Models;

/// <summary>
/// Thanh toán khi ShopOwner tạo POI theo mức ưu tiên.
/// Thấp (1) = 1.000.000đ / Trung bình (2) = 3.000.000đ / Cao (3) = 7.000.000đ.
/// Admin đối soát với sao kê ngân hàng và xác nhận → POI.IsActive = true.
/// </summary>
[Table("POIPayments")]
public class POIPayment
{
    public int Id { get; set; }

    /// <summary>POI cần thanh toán để kích hoạt.</summary>
    public int POIId { get; set; }

    [ForeignKey(nameof(POIId))]
    public POI? POI { get; set; }

    /// <summary>ShopOwner thực hiện thanh toán.</summary>
    public int OwnerId { get; set; }

    [ForeignKey(nameof(OwnerId))]
    public User? Owner { get; set; }

    /// <summary>Mức ưu tiên: 1=Thấp, 2=Trung bình, 3=Cao.</summary>
    public int Priority { get; set; }

    /// <summary>Số tiền phải thanh toán (VND): 1→1.000.000 / 2→3.000.000 / 3→7.000.000.</summary>
    public long AmountVnd { get; set; }

    /// <summary>Mã nội dung chuyển khoản duy nhất, vd. POIPAY-42-7F3A.</summary>
    [MaxLength(64)]
    public string TransferRef { get; set; } = "";

    public PaymentReconciliationStatus Status { get; set; } = PaymentReconciliationStatus.Pending;

    public DateTime ReportedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? VerifiedAtUtc { get; set; }

    public int? VerifiedByUserId { get; set; }

    [MaxLength(500)]
    public string? AdminNote { get; set; }
}

public static class POIPricing
{
    public static long GetPrice(int priority) => priority switch
    {
        1 => 1_000_000,
        2 => 3_000_000,
        3 => 7_000_000,
        _ => 1_000_000
    };

    public static string GetLabel(int priority) => priority switch
    {
        1 => "Thấp",
        2 => "Trung bình",
        3 => "Cao",
        _ => "Thấp"
    };
}
