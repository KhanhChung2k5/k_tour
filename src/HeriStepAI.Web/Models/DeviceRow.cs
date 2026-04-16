namespace HeriStepAI.Web.Models;

public class DeviceRow
{
    public string DeviceId { get; set; } = "";
    public int VisitCount { get; set; }
    public int UniquePOIs { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
}

public class DeviceSummary
{
    public int TotalDevices { get; set; }
    public int ActiveToday { get; set; }
    public int ActiveThisWeek { get; set; }
    public int ActiveThisMonth { get; set; }
}

public class DevicePageResult
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<DeviceRow> Items { get; set; } = new();
}

public class DeviceDetailViewModel
{
    public string DeviceId { get; set; } = "";
    public int VisitCount { get; set; }
    public int UniquePOIs { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public DevicePaymentInfo? Payment { get; set; }
    public List<DevicePoiVisitRow> POIs { get; set; } = new();
}

public class DevicePaymentInfo
{
    public int Id { get; set; }
    public string DeviceKey { get; set; } = "";
    public string TransferRef { get; set; } = "";
    public string PlanCode { get; set; } = "";
    public string? PlanLabel { get; set; }
    public int AmountVnd { get; set; }
    public string Status { get; set; } = "";
    public DateTime? ReportedAtUtc { get; set; }
    public DateTime? SubscriptionExpiresAtUtc { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
}

public class DevicePoiVisitRow
{
    public int POIId { get; set; }
    public string POIName { get; set; } = "";
    public int VisitCount { get; set; }
    public DateTime FirstVisit { get; set; }
    public DateTime LastVisit { get; set; }
}
