namespace HeriStepAI.Web.Models;

public class SubscriptionPaymentRow
{
    public int Id { get; set; }
    public string DeviceKey { get; set; } = "";
    public string TransferRef { get; set; } = "";
    public string PlanCode { get; set; } = "";
    public string? PlanLabel { get; set; }
    public int AmountVnd { get; set; }
    public string Status { get; set; } = "";
    public DateTime ReportedAtUtc { get; set; }
    public DateTime? SubscriptionExpiresAtUtc { get; set; }
    public string? Platform { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public int? VerifiedByUserId { get; set; }
    public string? AdminNote { get; set; }
}
