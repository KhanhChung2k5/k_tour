using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace HeriStepAI.Web.Controllers;

public class AuthController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public IActionResult Login()
    {
        Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        var client = _httpClientFactory.CreateClient("API");
        var json = JsonSerializer.Serialize(new { Email = email, Password = password });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage? response = null;
        try
        {
            // Retry tối đa 3 lần nếu API trả 429 (Render spin-up throttle)
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var retryContent = new StringContent(json, Encoding.UTF8, "application/json");
                response = await client.PostAsync("auth/login", retryContent);
                if ((int)response.StatusCode != 429) break;
                if (attempt < 2) await Task.Delay(5000 * (attempt + 1)); // 5s, 10s
            }
        }
        catch (Exception)
        {
            ViewBag.Error = "Không kết nối được API. Kiểm tra API đang chạy (port 5000).";
            return View();
        }
        response ??= new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable);

        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LoginResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result?.Token == null) { ViewBag.Error = "Lỗi đăng nhập."; return View(); }

            // Lấy claims từ JWT (phần payload giữa 2 dấu chấm)
            var claims = GetClaimsFromJwt(result.Token);
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
            });

            // Lưu token cho các gọi API từ server
            Response.Cookies.Append("AuthToken", result.Token, new CookieOptions { HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Lax, Path = "/", Expires = DateTimeOffset.UtcNow.AddDays(1) });

            // Redirect based on role
            var roleClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var roleValue = roleClaim?.Value ?? "Unknown";

            // Debug: Log role value
            Console.WriteLine($"[AUTH] Role claim value: '{roleValue}'");

            // Check for ShopOwner role (both enum name and value)
            if (roleValue == "ShopOwner" || roleValue == "2")
            {
                Console.WriteLine("[AUTH] Redirecting to ShopOwner Dashboard");
                return RedirectToAction("Dashboard", "ShopOwner");
            }

            Console.WriteLine("[AUTH] Redirecting to Admin Dashboard");
            return RedirectToAction("Dashboard", "Home");
        }

        ViewBag.Error = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            ? "Email hoặc mật khẩu không đúng. Dùng admin@heristepai.com / admin123."
            : $"Lỗi API ({(int)response.StatusCode}).";
        return View();
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        // Any authenticated user → redirect to Home/Index which handles role routing
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        // Not authenticated → go to login
        return RedirectToAction("Login");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("AuthToken");
        return RedirectToAction("Index", "Home");
    }

    private static List<Claim> GetClaimsFromJwt(string token)
    {
        var claims = new List<Claim>();
        try
        {
            var parts = token.Split('.');
            if (parts.Length >= 2)
            {
                var payload = parts[1];
                payload = payload.Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }
                var bytes = Convert.FromBase64String(payload);
                var json = Encoding.UTF8.GetString(bytes);
                Console.WriteLine($"[AUTH] JWT payload: {json}");
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Map both short names and full URI claim types from JWT
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Value.ValueKind != JsonValueKind.String) continue;
                    var val = prop.Value.GetString() ?? "";
                    switch (prop.Name)
                    {
                        case "nameid":
                        case ClaimTypes.NameIdentifier:
                            claims.Add(new Claim(ClaimTypes.NameIdentifier, val));
                            break;
                        case "email":
                        case ClaimTypes.Email:
                            claims.Add(new Claim(ClaimTypes.Email, val));
                            break;
                        case "unique_name":
                        case ClaimTypes.Name:
                            claims.Add(new Claim(ClaimTypes.Name, val));
                            break;
                        case "role":
                        case ClaimTypes.Role:
                            claims.Add(new Claim(ClaimTypes.Role, val));
                            Console.WriteLine($"[AUTH] Role from JWT: '{val}'");
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AUTH] JWT parse error: {ex.Message}");
        }
        if (!claims.Any(c => c.Type == ClaimTypes.Name))
            claims.Add(new Claim(ClaimTypes.Name, "User"));
        return claims;
    }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
}
