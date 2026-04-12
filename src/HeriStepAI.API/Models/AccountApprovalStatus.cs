namespace HeriStepAI.API.Models;

/// <summary>
/// Trạng thái duyệt tài khoản (chủ yếu cho ShopOwner đăng ký công khai).
/// Admin / Tourist: <see cref="NotApplicable"/>.
/// </summary>
public enum AccountApprovalStatus
{
    NotApplicable = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3
}
