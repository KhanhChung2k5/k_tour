namespace HeriStepAI.Web.Models;

public class PendingShopOwnerViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
}
