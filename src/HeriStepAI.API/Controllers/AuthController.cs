using HeriStepAI.API.Data;
using HeriStepAI.API.Models;
using HeriStepAI.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeriStepAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;

    public AuthController(IAuthService authService, ApplicationDbContext context)
    {
        _authService = authService;
        _context = context;
    }

    /// <summary>
    /// Seed admin. ?force=1 để reset mật khẩu admin về admin123.
    /// GET http://localhost:5000/api/auth/seed
    /// </summary>
    [HttpGet("seed")]
    public async Task<IActionResult> Seed([FromQuery] bool force = false)
    {
        var seedService = new SeedService(_context);
        var (created, message) = await seedService.EnsureAdminAsync(force);
        return Ok(new { Created = created, Message = message });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { Message = "Email and password required" });
        }
        try
        {
            var token = await _authService.LoginAsync(request.Email, request.Password ?? "");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);
            return Ok(new
            {
                Token = token,
                UserId = user!.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            if (ex.Message == "ACCOUNT_PENDING_APPROVAL")
                return StatusCode(403, new { Message = "Tài khoản chờ Admin duyệt. Bạn sẽ đăng nhập được sau khi được phê duyệt.", Code = ex.Message });
            if (ex.Message == "ACCOUNT_REJECTED")
                return StatusCode(403, new { Message = "Tài khoản đăng ký đã bị từ chối.", Code = ex.Message });
            return Unauthorized(new { Message = "Invalid credentials" });
        }
    }

    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var user = await _authService.RegisterAsync(
                request.Username,
                request.Email,
                request.Password,
                request.Role,
                request.FullName,
                request.Phone,
                approvalStatus: request.Role == UserRole.ShopOwner ? AccountApprovalStatus.Approved : null);
            return Ok(new { UserId = user!.Id, Username = user.Username, Email = user.Email, FullName = user.FullName });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    // Public tourist registration — không cần Admin token
    [HttpPost("register-tourist")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterTourist([FromBody] TouristRegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { Message = "Email và mật khẩu không được để trống" });

        if (request.Password.Length < 6)
            return BadRequest(new { Message = "Mật khẩu phải có ít nhất 6 ký tự" });

        try
        {
            var user = await _authService.RegisterAsync(
                request.Username ?? request.Email.Split('@')[0],
                request.Email,
                request.Password,
                UserRole.Tourist,
                request.FullName,
                approvalStatus: AccountApprovalStatus.NotApplicable);

            var token = await _authService.LoginAsync(request.Email, request.Password);
            return Ok(new
            {
                Token = token,
                UserId = user!.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>Đăng ký chủ quán (công khai) — chờ Admin duyệt mới đăng nhập được.</summary>
    [HttpPost("register-shop-owner")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterShopOwner([FromBody] ShopOwnerSelfRegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { Message = "Username, email và mật khẩu là bắt buộc" });
        if (request.Password.Length < 6)
            return BadRequest(new { Message = "Mật khẩu phải có ít nhất 6 ký tự" });

        try
        {
            var user = await _authService.RegisterAsync(
                request.Username.Trim(),
                request.Email.Trim(),
                request.Password,
                UserRole.ShopOwner,
                request.FullName,
                request.Phone,
                approvalStatus: AccountApprovalStatus.Pending);
            return Ok(new
            {
                Message = "Đăng ký thành công. Vui lòng chờ Admin duyệt trước khi đăng nhập.",
                UserId = user!.Id
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("pending-shop-owners")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPendingShopOwners()
    {
        var list = await _context.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.ShopOwner && u.ApprovalStatus == AccountApprovalStatus.Pending)
            .OrderBy(u => u.CreatedAt)
            .Select(u => new { u.Id, u.Username, u.Email, u.FullName, u.Phone, u.CreatedAt })
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>Lấy tất cả ShopOwner (Admin) — có thể lọc theo ApprovalStatus.</summary>
    [HttpGet("shop-owners")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllShopOwners([FromQuery] string? status)
    {
        var q = _context.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.ShopOwner);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AccountApprovalStatus>(status, true, out var st))
            q = q.Where(u => u.ApprovalStatus == st);

        var list = await q
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id, u.Username, u.Email, u.FullName, u.Phone,
                u.IsActive, u.CreatedAt,
                ApprovalStatus = u.ApprovalStatus.ToString()
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Admin bật/tắt IsActive của một ShopOwner.</summary>
    [HttpPost("toggle-active/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var user = await _context.Users.AsTracking()
            .FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRole.ShopOwner);
        if (user == null) return NotFound(new { Message = "Không tìm thấy ShopOwner." });

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new { Message = user.IsActive ? "Đã kích hoạt tài khoản." : "Đã vô hiệu hóa tài khoản.", isActive = user.IsActive });
    }

    [HttpPost("approve-shop-owner/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveShopOwner(int id)
    {
        try
        {
            var ok = await _authService.ApproveShopOwnerAsync(id);
            return ok ? Ok(new { Message = "Đã duyệt tài khoản chủ quán." }) : NotFound(new { Message = "Không tìm thấy user ShopOwner hoặc không phải ShopOwner." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Auth] ApproveShopOwner: {ex}");
            return StatusCode(500, new { Message = "Lỗi khi cập nhật DB. Kiểm tra cột ApprovalStatus trên Supabase và connection string API.", Detail = ex.Message });
        }
    }

    [HttpPost("reject-shop-owner/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RejectShopOwner(int id)
    {
        try
        {
            var ok = await _authService.RejectShopOwnerAsync(id);
            return ok ? Ok(new { Message = "Đã từ chối tài khoản." }) : NotFound(new { Message = "Không tìm thấy user ShopOwner hoặc không phải ShopOwner." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Auth] RejectShopOwner: {ex}");
            return StatusCode(500, new { Message = "Lỗi khi cập nhật DB.", Detail = ex.Message });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null) return NotFound();

        return Ok(new { user.Id, user.Username, user.Email, user.FullName, user.Role });
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public UserRole Role { get; set; }
}

public class TouristRegisterRequest
{
    public string? Username { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FullName { get; set; }
}

public class ShopOwnerSelfRegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Phone { get; set; }
}
