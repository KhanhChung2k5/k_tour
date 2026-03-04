using HeriStepAI.API.Data;
using HeriStepAI.API.Models;
using HeriStepAI.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HeriStepAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IPOIService _poiService;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AnalyticsController(IAnalyticsService analyticsService, IPOIService poiService,
        ApplicationDbContext context, IConfiguration configuration)
    {
        _analyticsService = analyticsService;
        _poiService = poiService;
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("visit")]
    [AllowAnonymous]
    public async Task<IActionResult> LogVisit([FromBody] VisitLogRequest request)
    {
        var userId = request.UserId;
        if (string.IsNullOrWhiteSpace(userId) && User.Identity?.IsAuthenticated == true)
            userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        await _analyticsService.LogVisitAsync(
            request.POId,
            userId,
            request.Latitude,
            request.Longitude,
            request.VisitType
        );
        return Ok(new { Message = "Visit logged" });
    }

    [HttpGet("top-pois")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetTopPOIs([FromQuery] int count = 10, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var topPOIs = await _analyticsService.GetTopPOIsAsync(count, startDate, endDate);
        return Ok(topPOIs);
    }

    [HttpGet("poi/{poiId}/statistics")]
    public async Task<IActionResult> GetPOIStatistics(int poiId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        if (User.IsInRole("ShopOwner"))
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var poi = await _poiService.GetPOIByIdAsync(poiId);
            if (poi?.OwnerId != userId)
                return Forbid();
        }

        var stats = await _analyticsService.GetPOIStatisticsAsync(poiId, startDate, endDate);
        return Ok(stats);
    }

    [HttpGet("poi/{poiId}/logs")]
    public async Task<IActionResult> GetVisitLogs(int poiId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        if (User.IsInRole("ShopOwner"))
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var poi = await _poiService.GetPOIByIdAsync(poiId);
            if (poi?.OwnerId != userId)
                return Forbid();
        }

        var logs = await _analyticsService.GetVisitLogsAsync(poiId, startDate, endDate);
        return Ok(logs);
    }

    // ─── Daily Report for n8n Digest Workflow ───────────────────────────────
    [HttpGet("daily-report")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDailyReport([FromQuery] string? token)
    {
        var expectedToken = _configuration["Report:Token"] ?? Environment.GetEnvironmentVariable("REPORT_TOKEN");
        if (!string.IsNullOrEmpty(expectedToken) && token != expectedToken)
            return Unauthorized(new { error = "Invalid token" });

        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        var allLogs = await _context.VisitLogs
            .Include(v => v.POI)
            .Where(v => v.VisitTime >= yesterday)
            .ToListAsync();

        var todayLogs = allLogs.Where(v => v.VisitTime >= today).ToList();
        var yesterdayLogs = allLogs.Where(v => v.VisitTime >= yesterday && v.VisitTime < today).ToList();

        var topPOIs = todayLogs
            .GroupBy(v => new { v.POId, v.POI.Name })
            .Select(g => new
            {
                poiId = g.Key.POId,
                name = g.Key.Name,
                visits = g.Count(),
                geofence = g.Count(v => v.VisitType == VisitType.Geofence),
                mapClick = g.Count(v => v.VisitType == VisitType.MapClick),
                qrCode = g.Count(v => v.VisitType == VisitType.QRCode)
            })
            .OrderByDescending(x => x.visits)
            .Take(5)
            .ToList();

        var activePOIIds = await _context.POIs
            .Where(p => p.IsActive)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync();

        var visitedTodayIds = todayLogs.Select(v => v.POId).ToHashSet();
        var zeroPOIs = activePOIIds
            .Where(p => !visitedTodayIds.Contains(p.Id))
            .Select(p => p.Name)
            .ToList();

        var hourlyPeak = todayLogs
            .GroupBy(v => v.VisitTime.Hour)
            .Select(g => new { hour = g.Key, visits = g.Count() })
            .OrderByDescending(x => x.visits)
            .FirstOrDefault();

        var totalToday = todayLogs.Count;
        var totalYesterday = yesterdayLogs.Count;
        var growth = totalYesterday > 0
            ? Math.Round((totalToday - totalYesterday) / (double)totalYesterday * 100, 1)
            : (totalToday > 0 ? 100.0 : 0.0);

        return Ok(new
        {
            date = today.ToString("yyyy-MM-dd"),
            totalVisitsToday = totalToday,
            totalVisitsYesterday = totalYesterday,
            growthPercent = growth,
            topPOIs,
            zeroPOIs,
            visitsByType = new
            {
                geofence = todayLogs.Count(v => v.VisitType == VisitType.Geofence),
                mapClick = todayLogs.Count(v => v.VisitType == VisitType.MapClick),
                qrCode = todayLogs.Count(v => v.VisitType == VisitType.QRCode)
            },
            hourlyPeak = hourlyPeak == null ? null : new { hourlyPeak.hour, hourlyPeak.visits },
            totalActivePOIs = activePOIIds.Count
        });
    }

    // ─── User Daily Visits for Certificate Workflow ──────────────────────────
    [HttpGet("user-daily-visits")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserDailyVisits([FromQuery] string? token, [FromQuery] string? date = null)
    {
        var expectedToken = _configuration["Report:Token"] ?? Environment.GetEnvironmentVariable("REPORT_TOKEN");
        if (!string.IsNullOrEmpty(expectedToken) && token != expectedToken)
            return Unauthorized(new { error = "Invalid token" });

        var targetDate = date != null && DateTime.TryParse(date, out var d) ? d.Date : DateTime.UtcNow.Date;
        var nextDate = targetDate.AddDays(1);

        // Get all visits that day with POI info, for logged-in users only
        var visits = await _context.VisitLogs
            .Include(v => v.POI)
            .Where(v => v.VisitTime >= targetDate && v.VisitTime < nextDate && v.UserId != null)
            .OrderBy(v => v.VisitTime)
            .ToListAsync();

        if (!visits.Any())
            return Ok(new { date = targetDate.ToString("yyyy-MM-dd"), users = new List<object>() });

        // Get user info for those user IDs
        var userIds = visits.Select(v => v.UserId!).Distinct().ToList();
        var userIdInts = userIds.Select(id => int.TryParse(id, out var i) ? i : -1).Where(i => i > 0).ToList();

        var users = await _context.Users
            .Where(u => userIdInts.Contains(u.Id) && u.Role == UserRole.Tourist)
            .ToListAsync();

        var result = users.Select(u =>
        {
            var userVisits = visits
                .Where(v => v.UserId == u.Id.ToString())
                .GroupBy(v => v.POId)
                .Select(g => new
                {
                    poiId = g.Key,
                    poiName = g.First().POI.Name,
                    imageUrl = g.First().POI.ImageUrl ?? "",
                    address = g.First().POI.Address ?? "",
                    firstVisitTime = g.Min(v => v.VisitTime).ToString("HH:mm"),
                    visitCount = g.Count()
                })
                .OrderBy(v => v.firstVisitTime)
                .ToList();

            return new
            {
                userId = u.Id,
                username = u.Username,
                fullName = u.FullName ?? u.Username,
                email = u.Email,
                totalPOIs = userVisits.Count,
                pois = userVisits
            };
        }).Where(u => u.totalPOIs > 0).ToList();

        return Ok(new
        {
            date = targetDate.ToString("yyyy-MM-dd"),
            dateFormatted = targetDate.ToString("dd/MM/yyyy"),
            totalUsers = result.Count,
            users = result
        });
    }
}

public class VisitLogRequest
{
    public int POId { get; set; }
    public string? UserId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public VisitType VisitType { get; set; }
}
