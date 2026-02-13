using HeriStepAI.API.Models;

namespace HeriStepAI.API.Services;

public interface IAuthService
{
    Task<string> LoginAsync(string email, string password);
    Task<User?> RegisterAsync(string username, string email, string password, UserRole role, string? fullName = null, string? phone = null);
    Task<User?> GetUserByIdAsync(int id);
}
