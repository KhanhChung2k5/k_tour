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
