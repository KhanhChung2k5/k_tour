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

    public async Task<(int TotalVisits, int Geofence, int MapClick, int QRCode)> GetVisitSummaryAsync(DateTime? startDate, DateTime? endDate)
    {
        var query = _context.VisitLogs.AsQueryable();
        if (startDate.HasValue) query = query.Where(v => v.VisitTime >= startDate.Value);
        if (endDate.HasValue) query = query.Where(v => v.VisitTime <= endDate.Value);

        var total = await query.CountAsync();
        var geofence = await query.CountAsync(v => v.VisitType == VisitType.Geofence);
        var mapClick = await query.CountAsync(v => v.VisitType == VisitType.MapClick);
        var qrCode = await query.CountAsync(v => v.VisitType == VisitType.QRCode);
        return (total, geofence, mapClick, qrCode);
    }

    public async Task<object> GetDeviceStatsAsync(int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;

        var logs = await _context.VisitLogs
            .Where(v => v.UserId != null)
            .Select(v => new { v.UserId, v.VisitTime, v.POId })
            .ToListAsync();

        var grouped = logs
            .GroupBy(v => v.UserId!)
            .Select(g => new
            {
                DeviceId    = g.Key,
                VisitCount  = g.Count(),
                UniquePOIs  = g.Select(v => v.POId).Distinct().Count(),
                FirstSeen   = g.Min(v => v.VisitTime),
                LastSeen    = g.Max(v => v.VisitTime),
            })
            .OrderByDescending(d => d.LastSeen)
            .ToList();

        var total = grouped.Count;
        var items = grouped.Skip(skip).Take(pageSize).ToList();

        return new { Total = total, Page = page, PageSize = pageSize, Items = items };
    }

    public async Task<object> GetDeviceSummaryAsync()
    {
        var now = DateTime.UtcNow;
        var today    = now.Date;
        var week7    = now.AddDays(-7);
        var month30  = now.AddDays(-30);

        var logs = await _context.VisitLogs
            .Where(v => v.UserId != null)
            .Select(v => new { v.UserId, v.VisitTime })
            .ToListAsync();

        var total       = logs.Select(v => v.UserId).Distinct().Count();
        var activeToday = logs.Where(v => v.VisitTime >= today).Select(v => v.UserId).Distinct().Count();
        var activeWeek  = logs.Where(v => v.VisitTime >= week7).Select(v => v.UserId).Distinct().Count();
        var activeMonth = logs.Where(v => v.VisitTime >= month30).Select(v => v.UserId).Distinct().Count();

        return new
        {
            TotalDevices  = total,
            ActiveToday   = activeToday,
            ActiveThisWeek = activeWeek,
            ActiveThisMonth = activeMonth,
        };
    }
}
