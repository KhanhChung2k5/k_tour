using HeriStepAI.Geo;
using HeriStepAI.Mobile.Models;
using Microsoft.Maui.Devices.Sensors;

namespace HeriStepAI.Mobile.Services;

/// <summary>
/// Dịch vụ phát hiện vào vùng POI.
/// </summary>
public class GeofenceService : IGeofenceService
{
    private List<POI> _pois = new();
    private POI? _currentPOI = null;
    private readonly Dictionary<int, DateTime> _poiCooldowns = new();
    private readonly TimeSpan _cooldownPeriod = TimeSpan.FromMinutes(5);
    private const double MinRadius = 50;

    public event EventHandler<POI>? POIEntered;

    public void Initialize(List<POI> pois)
    {
        _pois = pois.ToList();
        _currentPOI = null;
        _poiCooldowns.Clear();
        AppLog.Info($"GeofenceService initialized with {_pois.Count} POIs, cooldowns reset");
    }

    /// <summary>
    /// Phát hiện vào vùng POI.
    /// </summary>
    public POI? CheckGeofence(Location location)
    {
        if (_pois == null || !_pois.Any())
            return null;

        // Trong vùng geofence: ưu tiên Priority (cao hơn trước), cùng Priority → gần hơn thắng (GeofenceSelection).
        var geoPois = _pois
            .Select(p => new GeofencePoi(p.Id, p.Name, p.Latitude, p.Longitude, p.Radius, p.Priority))
            .ToList();

        var bestGeo = GeofenceSelection.FindBestInside(location.Latitude, location.Longitude, geoPois, MinRadius);

        POI? closestPOI = null;
        double closestDistance = 0;
        if (bestGeo != null)
        {
            closestPOI = _pois.First(p => p.Id == bestGeo.Value.Id);
            closestDistance = GeofenceSelection.HaversineMeters(
                location.Latitude, location.Longitude,
                closestPOI.Latitude, closestPOI.Longitude);
        }

        if (closestPOI != null)
        {
            if (_currentPOI?.Id == closestPOI.Id)
                return null;

            if (_poiCooldowns.TryGetValue(closestPOI.Id, out var lastTime)
                && DateTime.UtcNow - lastTime < _cooldownPeriod)
            {
                AppLog.Info($"⏭️ Skipped (cooldown): {closestPOI.Name}");
                return null;
            }

            _currentPOI = closestPOI;
            _poiCooldowns[closestPOI.Id] = DateTime.UtcNow;
            POIEntered?.Invoke(this, closestPOI);
            AppLog.Info($"🎯 Geofence entered: {closestPOI.Name} (dist={closestDistance:F1}m, radius={Math.Max(closestPOI.Radius, MinRadius)}m)");
            return closestPOI;
        }

        if (_currentPOI != null)
        {
            AppLog.Info($"📤 Left geofence: {_currentPOI.Name}");
            _currentPOI = null;
        }

        return null;
    }
}
