namespace HeriStepAI.Web.Models;

public class ShopOwnerRow
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ApprovalStatus { get; set; } = "";
}
