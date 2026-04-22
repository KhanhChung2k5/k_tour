using HeriStepAI.API.Models;

namespace HeriStepAI.API.Services;

public interface IAnalyticsService
{
    Task LogVisitAsync(int poiId, string? userId, double? latitude, double? longitude, VisitType visitType);
    Task<List<VisitLog>> GetVisitLogsAsync(int? poiId, DateTime? startDate, DateTime? endDate);
    Task<Dictionary<int, int>> GetTopPOIsAsync(int count, DateTime? startDate, DateTime? endDate);
    Task<object> GetPOIStatisticsAsync(int poiId, DateTime? startDate, DateTime? endDate);
    /// <summary>Tổng lượt ghé thăm và phân loại (Geofence / MapClick / QRCode) — tính trực tiếp từ VisitLogs.</summary>
    Task<(int TotalVisits, int Geofence, int MapClick, int QRCode)> GetVisitSummaryAsync(DateTime? startDate, DateTime? endDate);
    Task<object> GetDeviceStatsAsync(int page, int pageSize);
    Task<object> GetDeviceSummaryAsync();
    Task<object?> GetDeviceDetailAsync(string deviceId);
    Task<List<double[]>> GetHeatmapDataAsync(DateTime? startDate, DateTime? endDate);
}
