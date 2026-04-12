using HeriStepAI.API.Models;

namespace HeriStepAI.API.Services;

public interface IAuthService
{
    Task<string> LoginAsync(string email, string password);
    Task<User?> RegisterAsync(string username, string email, string password, UserRole role, string? fullName = null, string? phone = null, AccountApprovalStatus? approvalStatus = null);
    Task<User?> GetUserByIdAsync(int id);
    Task<bool> ApproveShopOwnerAsync(int userId);
    Task<bool> RejectShopOwnerAsync(int userId);
}
