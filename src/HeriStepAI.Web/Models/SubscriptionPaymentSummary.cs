namespace HeriStepAI.Web.Models;

public class SubscriptionPaymentSummary
{
    public int Pending { get; set; }
    public int Verified { get; set; }
    public int Rejected { get; set; }
    public int ReportsLast7Days { get; set; }
    public long TotalAmountVndVerified { get; set; }
}
