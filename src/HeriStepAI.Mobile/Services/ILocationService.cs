using HeriStepAI.Mobile.Models;
using Microsoft.Maui.Devices.Sensors;

namespace HeriStepAI.Mobile.Services;

/// <summary>Dịch vụ location</summary>
public interface ILocationService
{
    /// <summary>Lấy vị trí hiện tại</summary>
    Task<Location?> GetCurrentLocationAsync(GeolocationAccuracy accuracy = GeolocationAccuracy.Medium);
    /// <summary>Yêu cầu quyền truy cập vị trí</summary>
    Task<bool> RequestLocationPermissionAsync();
    /// <summary>Bắt đầu cập nhật vị trí</summary>
    void StartLocationUpdates();
    /// <summary>Dừng cập nhật vị trí</summary>
    void StopLocationUpdates();
    /// <summary>Event khi vị trí thay đổi</summary>
    event EventHandler<Location>? LocationChanged;
    /// <summary>Trạng thái truy cập vị trí</summary>
    bool IsLocationEnabled { get; }
}
