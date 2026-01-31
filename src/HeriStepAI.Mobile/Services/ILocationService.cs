using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

public interface ILocationService
{
    Task<Location?> GetCurrentLocationAsync();
    Task<bool> RequestLocationPermissionAsync();
    void StartLocationUpdates();
    void StopLocationUpdates();
    event EventHandler<Location>? LocationChanged;
    bool IsLocationEnabled { get; }
}
