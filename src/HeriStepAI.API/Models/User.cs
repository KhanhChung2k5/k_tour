namespace HeriStepAI.API.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    /// <summary>Duyệt đăng ký ShopOwner: Pending → Admin duyệt → Approved. Admin/Tourist dùng <see cref="AccountApprovalStatus.NotApplicable"/>.</summary>
    public AccountApprovalStatus ApprovalStatus { get; set; } = AccountApprovalStatus.Approved;

    // Device capability — được cập nhật sau khi mobile login
    public MobileDeviceProfile? DeviceProfile { get; set; }
    public int? DeviceCores { get; set; }
    public long? DeviceRamMb { get; set; }
    public DateTime? DeviceProfileAt { get; set; }
}

public enum MobileDeviceProfile
{
    Weak   = 0,
    Strong = 1,
}

public enum UserRole
{
    Admin = 1,
    ShopOwner = 2,
    Tourist = 3
}
