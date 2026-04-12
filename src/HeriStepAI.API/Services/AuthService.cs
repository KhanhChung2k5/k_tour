using System.Security.Cryptography;
using HeriStepAI.API.Data;
using HeriStepAI.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HeriStepAI.API.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<string> LoginAsync(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        if (user.Role == UserRole.ShopOwner)
        {
            if (user.ApprovalStatus == AccountApprovalStatus.Pending)
                throw new UnauthorizedAccessException("ACCOUNT_PENDING_APPROVAL");
            if (user.ApprovalStatus == AccountApprovalStatus.Rejected)
                throw new UnauthorizedAccessException("ACCOUNT_REJECTED");
        }

        return GenerateJwtToken(user);
    }

    public async Task<User?> RegisterAsync(string username, string email, string password, UserRole role, string? fullName = null, string? phone = null, AccountApprovalStatus? approvalStatus = null)
    {
        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            throw new InvalidOperationException("Email already exists");
        }

        var status = approvalStatus ?? DefaultApprovalForRole(role);

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = fullName,
            Phone = phone,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            ApprovalStatus = status
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private static AccountApprovalStatus DefaultApprovalForRole(UserRole role) => role switch
    {
        UserRole.Admin => AccountApprovalStatus.NotApplicable,
        UserRole.Tourist => AccountApprovalStatus.NotApplicable,
        UserRole.ShopOwner => AccountApprovalStatus.Approved,
        _ => AccountApprovalStatus.Approved
    };

    public async Task<bool> ApproveShopOwnerAsync(int userId)
    {
        // Dùng SQL trực tiếp để tránh lỗi tracking/FindAsync với pooler (Supabase) và đảm bảo ghi xuống DB.
        var rows = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Users""
            SET ""ApprovalStatus"" = {(int)AccountApprovalStatus.Approved}
            WHERE ""Id"" = {userId} AND ""Role"" = {(int)UserRole.ShopOwner}");
        return rows > 0;
    }

    public async Task<bool> RejectShopOwnerAsync(int userId)
    {
        var rows = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Users""
            SET ""ApprovalStatus"" = {(int)AccountApprovalStatus.Rejected}, ""IsActive"" = false
            WHERE ""Id"" = {userId} AND ""Role"" = {(int)UserRole.ShopOwner}");
        return rows > 0;
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
            ?? jwtSettings["SecretKey"]
            ?? "YourSuperSecretKeyForJWTTokenGeneration12345";
        var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? jwtSettings["Issuer"] ?? "HeriStepAI";
        var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? jwtSettings["Audience"] ?? "HeriStepAIUsers";
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "1440");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        if (keyBytes.Length < 32)
            keyBytes = SHA256.HashData(keyBytes);
        var key = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
