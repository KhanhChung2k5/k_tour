using HeriStepAI.API.Models;

namespace HeriStepAI.Web.ViewModels;

public class ShopOwnerPOIViewModel
{
    public POI POI { get; set; } = new();
    public int TotalVisits { get; set; }
    public int UniqueVisitors { get; set; }
    public DateTime? LastVisit { get; set; }
    public int GeofenceVisits { get; set; }
    public int ManualVisits { get; set; }
}
