namespace HeriStepAI.Web.Models;

public class POIPaymentRow
{
    public int Id { get; set; }
    public int POIId { get; set; }
    public string POIName { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public int Priority { get; set; }
    public string PriorityLabel { get; set; } = "";
    public long AmountVnd { get; set; }
    public string TransferRef { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime ReportedAtUtc { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public int? VerifiedByUserId { get; set; }
    public string? AdminNote { get; set; }
}

public class POIPaymentSummary
{
    public int Pending { get; set; }
    public int Verified { get; set; }
    public int Rejected { get; set; }
    public long TotalAmountVndVerified { get; set; }
}
