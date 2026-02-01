using HeriStepAI.Mobile.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace HeriStepAI.Mobile.Services;

public class LocationService : ILocationService
{
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isListening = false;

    public bool IsLocationEnabled => true;

    public event EventHandler<Location>? LocationChanged;

    public async Task<bool> RequestLocationPermissionAsync()
    {
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status == PermissionStatus.Granted)
        {
            return true;
        }

        // Request background location for Android
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            status = await Permissions.RequestAsync<Permissions.LocationAlways>();
        }

        return status == PermissionStatus.Granted;
    }

    /// <summary>
    /// Lấy vị trí - tối ưu pin với Medium accuracy, dùng Best khi cần chính xác.
    /// </summary>
    public async Task<Location?> GetCurrentLocationAsync(GeolocationAccuracy accuracy = GeolocationAccuracy.Medium)
    {
        try
        {
            var request = new GeolocationRequest
            {
                DesiredAccuracy = accuracy,
                Timeout = TimeSpan.FromSeconds(10)
            };

            var location = await Geolocation.Default.GetLocationAsync(request);
            return location;
        }
        catch
        {
            return null;
        }
    }

    public void StartLocationUpdates()
    {
        if (_isListening)
            return;

        _isListening = true;
        _cancellationTokenSource = new CancellationTokenSource();

        Task.Run(async () =>
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var location = await GetCurrentLocationAsync();
                        if (location != null)
                        {
                            LocationChanged?.Invoke(this, location);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error getting location: {ex.Message}");
                    }
                    await Task.Delay(5000, _cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in location loop: {ex.Message}");
            }
        }, _cancellationTokenSource.Token);
    }

    public void StopLocationUpdates()
    {
        _isListening = false;
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }
}
