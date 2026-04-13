namespace HeriStepAI.API.Models;

/// <summary>Trạng thái đối soát chuyển khoản gói mobile (báo từ app, xác nhận thủ công trên Admin).</summary>
public enum PaymentReconciliationStatus : byte
{
    Pending = 0,
    Verified = 1,
    Rejected = 2
}
