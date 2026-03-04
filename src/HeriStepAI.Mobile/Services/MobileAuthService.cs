using Newtonsoft.Json;
using System.Text;

namespace HeriStepAI.Mobile.Services;

public class MobileAuthService : IAuthService
{
    private const string TokenKey = "auth_token";
    private const string UserKey = "auth_user";

    private const string _baseUrl = "https://heristep.onrender.com/api/";

    private readonly HttpClient _http = new()
    {
        BaseAddress = new Uri(_baseUrl),
        Timeout = TimeSpan.FromSeconds(60)
    };

    public bool IsLoggedIn => CurrentUser != null;
    public UserSession? CurrentUser { get; private set; }

    public async Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync(TokenKey);
            var userJson = await SecureStorage.Default.GetAsync(UserKey);
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userJson))
                return false;

            CurrentUser = JsonConvert.DeserializeObject<UserSession>(userJson);
            SetToken(token);
            return CurrentUser != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool success, string error)> LoginAsync(string email, string password)
    {
        try
        {
            var payload = JsonConvert.SerializeObject(new { email, password });
            var response = await _http.PostAsync("auth/login",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                var msg = ParseErrorMessage(body) ?? "Đăng nhập thất bại";
                return (false, msg);
            }

            var result = JsonConvert.DeserializeObject<AuthResponse>(body)!;
            await SaveSessionAsync(result);
            return (true, "");
        }
        catch (TaskCanceledException)
        {
            return (false, "Server đang khởi động, vui lòng thử lại sau 30 giây...");
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi kết nối: {ex.Message}");
        }
    }

    public async Task<(bool success, string error)> RegisterAsync(string email, string password, string fullName)
    {
        try
        {
            var payload = JsonConvert.SerializeObject(new { email, password, fullName });
            var response = await _http.PostAsync("auth/register-tourist",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                var msg = ParseErrorMessage(body) ?? "Đăng ký thất bại";
                return (false, msg);
            }

            var result = JsonConvert.DeserializeObject<AuthResponse>(body)!;
            await SaveSessionAsync(result);
            return (true, "");
        }
        catch (TaskCanceledException)
        {
            return (false, "Server đang khởi động, vui lòng thử lại sau 30 giây...");
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi kết nối: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        _http.DefaultRequestHeaders.Authorization = null;
        Preferences.Default.Remove("has_session"); // clear quick-login flag
        try
        {
            SecureStorage.Default.Remove(TokenKey);
            SecureStorage.Default.Remove(UserKey);
        }
        catch { /* ignore */ }
        await Task.CompletedTask;
    }

    public string? GetToken()
    {
        return _http.DefaultRequestHeaders.Authorization?.Parameter;
    }

    // API dùng PropertyNamingPolicy = null (PascalCase), nên cần thử cả hai
    private static string? ParseErrorMessage(string body)
    {
        try
        {
            var jo = Newtonsoft.Json.Linq.JObject.Parse(body);
            return jo["Message"]?.ToString()   // PascalCase (API default)
                ?? jo["message"]?.ToString()   // camelCase fallback
                ?? jo["title"]?.ToString();    // ASP.NET 404 problem details
        }
        catch { return null; }
    }

    private async Task SaveSessionAsync(AuthResponse auth)
    {
        CurrentUser = new UserSession
        {
            Id = auth.UserId,
            Username = auth.Username ?? "",
            Email = auth.Email ?? "",
            FullName = auth.FullName ?? auth.Username ?? ""
        };
        SetToken(auth.Token!);

        Preferences.Default.Set("has_session", "1"); // quick-login flag (sync, 0ms)
        await SecureStorage.Default.SetAsync(TokenKey, auth.Token!);
        await SecureStorage.Default.SetAsync(UserKey, JsonConvert.SerializeObject(CurrentUser));
    }

    private void SetToken(string token)
    {
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private class AuthResponse
    {
        [JsonProperty("token")]
        public string? Token { get; set; }
        [JsonProperty("userId")]
        public int UserId { get; set; }
        [JsonProperty("username")]
        public string? Username { get; set; }
        [JsonProperty("email")]
        public string? Email { get; set; }
        [JsonProperty("fullName")]
        public string? FullName { get; set; }
    }
}
