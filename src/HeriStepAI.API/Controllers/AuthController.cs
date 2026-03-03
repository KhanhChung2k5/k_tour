using HeriStepAI.API.Data;
using HeriStepAI.API.Models;
using HeriStepAI.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            return Ok(new { Token = token });
        }
        catch (UnauthorizedAccessException)
        {
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
                request.Phone);
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
                request.FullName);

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
