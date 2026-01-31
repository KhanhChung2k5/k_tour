using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

public interface IApiService
{
    Task<List<POI>?> GetAllPOIsAsync();
    Task LogVisitAsync(int poiId, double? latitude, double? longitude, VisitType visitType);
}

public enum VisitType
{
    Geofence = 1,
    MapClick = 2,
    QRCode = 3
}
