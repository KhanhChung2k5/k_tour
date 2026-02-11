using HeriStepAI.Mobile.Models;
using Microsoft.Maui.Devices.Sensors;

namespace HeriStepAI.Mobile.Services;

public class GeofenceService : IGeofenceService
{
    private List<POI> _pois = new();
    private POI? _currentPOI = null;
    private readonly Dictionary<int, DateTime> _poiCooldowns = new();
    private readonly TimeSpan _cooldownPeriod = TimeSpan.FromMinutes(5);
    private const double MinRadius = 50; // Minimum geofence radius in meters

    public event EventHandler<POI>? POIEntered;

    public void Initialize(List<POI> pois)
    {
        _pois = pois.ToList();
        _currentPOI = null;
        _poiCooldowns.Clear();
        AppLog.Info($"GeofenceService initialized with {_pois.Count} POIs, cooldowns reset");
    }

    public POI? CheckGeofence(Location location)
    {
        if (_pois == null || !_pois.Any())
            return null;

        // Find the CLOSEST POI within its effective radius.
        // This prevents overlapping geofences from blocking each other:
        // when 2 POIs overlap, the one nearest to the user always wins.
        POI? closestPOI = null;
        double closestDistance = double.MaxValue;

        foreach (var poi in _pois)
        {
            var effectiveRadius = Math.Max(poi.Radius, MinRadius);
            var distance = CalculateDistance(location.Latitude, location.Longitude, poi.Latitude, poi.Longitude);

            if (distance <= effectiveRadius && distance < closestDistance)
            {
                closestPOI = poi;
                closestDistance = distance;
            }
        }

        if (closestPOI != null)
        {
            // Already inside this same POI - no re-trigger
            if (_currentPOI?.Id == closestPOI.Id)
                return null;

            // Per-POI cooldown check
            if (_poiCooldowns.TryGetValue(closestPOI.Id, out var lastTime)
                && DateTime.UtcNow - lastTime < _cooldownPeriod)
            {
                AppLog.Info($"⏭️ Skipped (cooldown): {closestPOI.Name}");
                return null;
            }

            // Trigger!
            _currentPOI = closestPOI;
            _poiCooldowns[closestPOI.Id] = DateTime.UtcNow;
            POIEntered?.Invoke(this, closestPOI);
            AppLog.Info($"🎯 Geofence entered: {closestPOI.Name} (dist={closestDistance:F1}m, radius={Math.Max(closestPOI.Radius, MinRadius)}m)");
            return closestPOI;
        }

        // Outside all POIs - reset current
        if (_currentPOI != null)
        {
            AppLog.Info($"📤 Left geofence: {_currentPOI.Name}");
            _currentPOI = null;
        }

        return null;
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}
