using HeriStepAI.API.Data;
using HeriStepAI.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HeriStepAI.API.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;

    public AnalyticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogVisitAsync(int poiId, string? userId, double? latitude, double? longitude, VisitType visitType)
    {
        var log = new VisitLog
        {
            POId = poiId,
            UserId = userId,
            Latitude = latitude,
            Longitude = longitude,
            VisitTime = DateTime.UtcNow,
            VisitType = visitType
        };

        _context.VisitLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<VisitLog>> GetVisitLogsAsync(int? poiId, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.VisitLogs.Include(v => v.POI).AsQueryable();

        if (poiId.HasValue)
            query = query.Where(v => v.POId == poiId.Value);

        if (startDate.HasValue)
            query = query.Where(v => v.VisitTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(v => v.VisitTime <= endDate.Value);

        return await query.OrderByDescending(v => v.VisitTime).ToListAsync();
    }

    public async Task<Dictionary<int, int>> GetTopPOIsAsync(int count, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.VisitLogs.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(v => v.VisitTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(v => v.VisitTime <= endDate.Value);

        var topPOIs = await query
            .GroupBy(v => v.POId)
            .Select(g => new { POId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(count)
            .ToListAsync();

        return topPOIs.ToDictionary(x => x.POId, x => x.Count);
    }

    public async Task<object> GetPOIStatisticsAsync(int poiId, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.VisitLogs.Where(v => v.POId == poiId);

        if (startDate.HasValue)
            query = query.Where(v => v.VisitTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(v => v.VisitTime <= endDate.Value);

        var totalVisits = await query.CountAsync();
        var uniqueVisitors = await query.Where(v => v.UserId != null).Select(v => v.UserId).Distinct().CountAsync();
        var avgDuration = await query.Where(v => v.DurationSeconds.HasValue).AverageAsync(v => (double?)v.DurationSeconds) ?? 0;

        var visitsByType = await query
            .GroupBy(v => v.VisitType)
            .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        return new
        {
            TotalVisits = totalVisits,
            UniqueVisitors = uniqueVisitors,
            AverageDuration = avgDuration,
            VisitsByType = visitsByType.ToDictionary(v => v.Type, v => v.Count)
        };
    }
}
