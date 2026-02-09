using HeriStepAI.API.Data;
using HeriStepAI.API.Models;
using HeriStepAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeriStepAI.Web.Controllers;

[Authorize(Roles = "ShopOwner")]
public class ShopOwnerController : Controller
{
    private readonly ApplicationDbContext _context;

    public ShopOwnerController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /ShopOwner/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        // Get POIs owned by current user
        var myPOIs = await _context.POIs
            .Include(p => p.Contents)
            .Where(p => p.OwnerId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        // Get visit statistics for owned POIs
        var poiIds = myPOIs.Select(p => p.Id).ToList();
        var visitStats = await _context.VisitLogs
            .Where(v => poiIds.Contains(v.POId))
            .GroupBy(v => v.POId)
            .Select(g => new
            {
                POId = g.Key,
                TotalVisits = g.Count(),
                UniqueVisitors = g.Select(v => new { v.Latitude, v.Longitude }).Distinct().Count(),
                LastVisit = g.Max(v => v.VisitTime),
                GeofenceVisits = g.Count(v => v.VisitType == VisitType.Geofence),
                ManualVisits = g.Count(v => v.VisitType == VisitType.MapClick)
            })
            .ToDictionaryAsync(x => x.POId);

        // Combine data
        var dashboardData = myPOIs.Select(poi => new ShopOwnerPOIViewModel
        {
            POI = poi,
            TotalVisits = visitStats.ContainsKey(poi.Id) ? visitStats[poi.Id].TotalVisits : 0,
            UniqueVisitors = visitStats.ContainsKey(poi.Id) ? visitStats[poi.Id].UniqueVisitors : 0,
            LastVisit = visitStats.ContainsKey(poi.Id) ? visitStats[poi.Id].LastVisit : null,
            GeofenceVisits = visitStats.ContainsKey(poi.Id) ? visitStats[poi.Id].GeofenceVisits : 0,
            ManualVisits = visitStats.ContainsKey(poi.Id) ? visitStats[poi.Id].ManualVisits : 0
        }).ToList();

        ViewBag.TotalPOIs = myPOIs.Count;
        ViewBag.TotalVisits = dashboardData.Sum(d => d.TotalVisits);
        ViewBag.ActivePOIs = myPOIs.Count(p => p.IsActive);

        return View(dashboardData);
    }

    // GET: /ShopOwner/Edit/{id}
    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var poi = await _context.POIs
            .Include(p => p.Contents)
            .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);

        if (poi == null)
            return NotFound("POI không tồn tại hoặc bạn không có quyền truy cập.");

        return View(poi);
    }

    // POST: /ShopOwner/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, POI model)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var poi = await _context.POIs
            .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);

        if (poi == null)
            return NotFound("POI không tồn tại hoặc bạn không có quyền truy cập.");

        // Only allow editing specific fields
        poi.Name = model.Name;
        poi.Description = model.Description;
        poi.Address = model.Address;
        poi.ImageUrl = model.ImageUrl;
        poi.FoodType = model.FoodType;
        poi.PriceMin = model.PriceMin;
        poi.PriceMax = model.PriceMax;
        poi.EstimatedMinutes = model.EstimatedMinutes;
        poi.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Cập nhật thông tin thành công!";
        return RedirectToAction(nameof(Dashboard));
    }

    // GET: /ShopOwner/Statistics/{id}
    public async Task<IActionResult> Statistics(int id, DateTime? from, DateTime? to)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var poi = await _context.POIs
            .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);

        if (poi == null)
            return NotFound("POI không tồn tại hoặc bạn không có quyền truy cập.");

        // Default date range: last 30 days
        from ??= DateTime.UtcNow.AddDays(-30);
        to ??= DateTime.UtcNow;

        var visits = await _context.VisitLogs
            .Where(v => v.POId == id && v.VisitTime >= from && v.VisitTime <= to)
            .OrderByDescending(v => v.VisitTime)
            .ToListAsync();

        // Daily statistics
        var dailyStats = visits
            .GroupBy(v => v.VisitTime.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalVisits = g.Count(),
                GeofenceVisits = g.Count(v => v.VisitType == VisitType.Geofence),
                ManualVisits = g.Count(v => v.VisitType == VisitType.MapClick)
            })
            .OrderBy(x => x.Date)
            .ToList();

        ViewBag.POI = poi;
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.TotalVisits = visits.Count;
        ViewBag.DailyStats = dailyStats;

        return View(visits);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}
