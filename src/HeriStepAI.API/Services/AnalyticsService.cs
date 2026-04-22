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

    public async Task<object?> GetDeviceDetailAsync(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return null;

        deviceId = deviceId.Trim();
        var normalizedDeviceKey = NormalizePossibleDeviceKey(deviceId);

        var logs = await _context.VisitLogs
            .Where(v => v.UserId == deviceId)
            .Select(v => new { v.POId, v.VisitTime })
            .ToListAsync();

        var payment = await _context.MobileSubscriptionPayments
            .AsNoTracking()
            .Where(p => p.DeviceKey == deviceId)
            .OrderByDescending(p => p.ReportedAtUtc)
            .Select(p => new
            {
                p.Id,
                p.DeviceKey,
                p.TransferRef,
                p.PlanCode,
                p.PlanLabel,
                p.AmountVnd,
                Status = p.Status.ToString(),
                p.ReportedAtUtc,
                p.SubscriptionExpiresAtUtc,
                p.VerifiedAtUtc
            })
            .FirstOrDefaultAsync();

        // Backward-compat: some visit IDs are stored as "dev_<DeviceKey>" while
        // payments are stored as "<DeviceKey>" (e.g. dev_1EFEBB vs 1EFEBB).
        if (payment == null && normalizedDeviceKey != null)
        {
            payment = await _context.MobileSubscriptionPayments
                .AsNoTracking()
                .Where(p => p.DeviceKey.ToUpper() == normalizedDeviceKey)
                .OrderByDescending(p => p.ReportedAtUtc)
                .Select(p => new
                {
                    p.Id,
                    p.DeviceKey,
                    p.TransferRef,
                    p.PlanCode,
                    p.PlanLabel,
                    p.AmountVnd,
                    Status = p.Status.ToString(),
                    p.ReportedAtUtc,
                    p.SubscriptionExpiresAtUtc,
                    p.VerifiedAtUtc
                })
                .FirstOrDefaultAsync();
        }

        if (!logs.Any() && payment == null)
            return null;

        var poiNames = await _context.POIs
            .AsNoTracking()
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        var poiVisits = logs
            .GroupBy(v => v.POId)
            .Select(g => new
            {
                POIId = g.Key,
                POIName = poiNames.TryGetValue(g.Key, out var name) ? name : $"POI #{g.Key}",
                VisitCount = g.Count(),
                FirstVisit = g.Min(x => x.VisitTime),
                LastVisit = g.Max(x => x.VisitTime)
            })
            .OrderByDescending(x => x.VisitCount)
            .ThenByDescending(x => x.LastVisit)
            .ToList();

        var firstSeen = logs.Any()
            ? logs.Min(v => v.VisitTime)
            : payment!.ReportedAtUtc;

        var lastSeen = logs.Any()
            ? logs.Max(v => v.VisitTime)
            : payment!.ReportedAtUtc;

        return new
        {
            DeviceId = deviceId,
            VisitCount = logs.Count,
            UniquePOIs = poiVisits.Count,
            FirstSeen = firstSeen,
            LastSeen = lastSeen,
            Payment = payment,
            POIs = poiVisits
        };
    }

    public async Task<List<double[]>> GetHeatmapDataAsync(DateTime? startDate, DateTime? endDate)
    {
        var query = _context.VisitLogs
            .Where(v => v.Latitude.HasValue && v.Longitude.HasValue && v.VisitType == VisitType.Geofence)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(v => v.VisitTime >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(v => v.VisitTime <= endDate.Value);

        var points = await query
            .Select(v => new { v.Latitude, v.Longitude })
            .ToListAsync();

        return points
            .Select(p => new double[] { p.Latitude!.Value, p.Longitude!.Value })
            .ToList();
    }

    private static string? NormalizePossibleDeviceKey(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var raw = userId.Trim();
        if (!raw.StartsWith("dev_", StringComparison.OrdinalIgnoreCase))
            return null;

        var candidate = raw.Substring(4).Trim();
        if (candidate.Length != 6)
            return null;

        foreach (var ch in candidate)
        {
            var isHex = (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
            if (!isHex) return null;
        }

        return candidate.ToUpperInvariant();
    }
}
