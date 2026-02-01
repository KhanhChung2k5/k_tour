using HeriStepAI.Mobile.Models;
using Microsoft.Maui.Devices.Sensors;

namespace HeriStepAI.Mobile.Services;

public interface ILocationService
{
    Task<Location?> GetCurrentLocationAsync(GeolocationAccuracy accuracy = GeolocationAccuracy.Medium);
    Task<bool> RequestLocationPermissionAsync();
    void StartLocationUpdates();
    void StopLocationUpdates();
    event EventHandler<Location>? LocationChanged;
    bool IsLocationEnabled { get; }
}
