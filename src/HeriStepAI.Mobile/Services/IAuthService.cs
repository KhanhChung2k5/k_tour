namespace HeriStepAI.Mobile.Services;

public class UserSession
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
}

public interface IAuthService
{
    bool IsLoggedIn { get; }
    UserSession? CurrentUser { get; }
    string? GetToken();

    /// <summary>Fired when CurrentUser is updated (e.g. background profile refresh).</summary>
    event EventHandler? UserProfileUpdated;

    Task<(bool success, string error)> LoginAsync(string email, string password);
    Task<(bool success, string error)> RegisterAsync(string email, string password, string fullName);
    Task LogoutAsync();
    Task<bool> TryRestoreSessionAsync();
}
