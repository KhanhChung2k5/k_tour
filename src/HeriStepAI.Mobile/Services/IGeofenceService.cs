using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

public interface IGeofenceService
{
    void Initialize(List<POI> pois);
    POI? CheckGeofence(Location location);
    event EventHandler<POI>? POIEntered;
}
