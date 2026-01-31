using HeriStepAI.API.Models;

namespace HeriStepAI.API.Services;

public interface IAnalyticsService
{
    Task LogVisitAsync(int poiId, string? userId, double? latitude, double? longitude, VisitType visitType);
    Task<List<VisitLog>> GetVisitLogsAsync(int? poiId, DateTime? startDate, DateTime? endDate);
    Task<Dictionary<int, int>> GetTopPOIsAsync(int count, DateTime? startDate, DateTime? endDate);
    Task<object> GetPOIStatisticsAsync(int poiId, DateTime? startDate, DateTime? endDate);
}
