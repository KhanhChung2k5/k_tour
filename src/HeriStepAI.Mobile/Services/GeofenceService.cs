using HeriStepAI.Mobile.Models;
using Microsoft.Maui.Devices.Sensors;

namespace HeriStepAI.Mobile.Services;

public class GeofenceService : IGeofenceService
{
    private List<POI> _pois = new();
    private POI? _currentPOI = null;
    private DateTime _lastTriggerTime = DateTime.MinValue;
    private readonly TimeSpan _cooldownPeriod = TimeSpan.FromMinutes(5);

    public event EventHandler<POI>? POIEntered;

    public void Initialize(List<POI> pois)
    {
        _pois = pois.OrderByDescending(p => p.Priority).ToList();
    }

    public POI? CheckGeofence(Location location)
    {
        if (_pois == null || !_pois.Any())
            return null;

        // Check cooldown
        if (DateTime.UtcNow - _lastTriggerTime < _cooldownPeriod && _currentPOI != null)
            return null;

        foreach (var poi in _pois)
        {
            var distance = CalculateDistance(location.Latitude, location.Longitude, poi.Latitude, poi.Longitude);
            
            if (distance <= poi.Radius)
            {
                // If we're already in this POI, don't trigger again
                if (_currentPOI?.Id == poi.Id)
                    return null;

                _currentPOI = poi;
                _lastTriggerTime = DateTime.UtcNow;
                POIEntered?.Invoke(this, poi);
                return poi;
            }
        }

        // If we're outside all POIs, reset current POI
        if (_currentPOI != null)
        {
            _currentPOI = null;
        }

        return null;
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula
        const double R = 6371000; // Earth radius in meters
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
