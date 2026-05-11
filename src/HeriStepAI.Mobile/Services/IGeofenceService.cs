using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

/// <summary>Dịch vụ kiểm tra geofence</summary>
public interface IGeofenceService
{
    void Initialize(List<POI> pois);
    /// <summary>Kiểm tra geofence</summary>
    POI? CheckGeofence(Location location);
    /// <summary>Event khi vào vùng POI</summary>
    event EventHandler<POI>? POIEntered;
}
