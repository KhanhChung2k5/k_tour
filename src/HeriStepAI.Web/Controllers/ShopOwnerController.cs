using HeriStepAI.API.Data;
using HeriStepAI.API.Models;
using HeriStepAI.Web.Services;
using HeriStepAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeriStepAI.Web.Controllers;

[Authorize]
public class ShopOwnerController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ISupabaseStorageService _storageService;

    public ShopOwnerController(ApplicationDbContext context, ISupabaseStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }

    // GET: /ShopOwner/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var myPOIs = await _context.POIs
            .Include(p => p.Contents)
            .Where(p => p.OwnerId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var poiIds = myPOIs.Select(p => p.Id).ToList();
        var visitLogs = await _context.VisitLogs
            .Where(v => poiIds.Contains(v.POId))
            .ToListAsync();

        var visitStats = visitLogs
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
            .ToDictionary(x => x.POId);

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

        var viewModel = new ShopOwnerEditViewModel
        {
            POI = poi,
            Contents = poi.Contents?.Select(c => new POIContentEditModel
            {
                Id = c.Id,
                Language = c.Language,
                TextContent = c.TextContent,
                AudioUrl = c.AudioUrl,
                ContentType = (int)c.ContentType
            }).ToList() ?? new List<POIContentEditModel>()
        };

        return View(viewModel);
    }

    // POST: /ShopOwner/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, POI model, IFormFile? ImageFile, List<POIContentEditModel>? Contents)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var poi = await _context.POIs
            .Include(p => p.Contents)
            .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);

        if (poi == null)
            return NotFound("POI không tồn tại hoặc bạn không có quyền truy cập.");

        // Upload new image if provided
        if (ImageFile != null && ImageFile.Length > 0)
        {
            using var stream = ImageFile.OpenReadStream();
            var imageUrl = await _storageService.UploadImageAsync(stream, ImageFile.FileName, ImageFile.ContentType);
            if (imageUrl != null)
            {
                poi.ImageUrl = imageUrl;
            }
        }

        // Update POI fields
        poi.Name = model.Name;
        poi.Description = model.Description;
        poi.Address = model.Address;
        poi.FoodType = model.FoodType;
        poi.PriceMin = model.PriceMin;
        poi.PriceMax = model.PriceMax;
        poi.EstimatedMinutes = model.EstimatedMinutes;
        poi.UpdatedAt = DateTime.UtcNow;

        // Update POI Contents
        var existingContents = poi.Contents?.ToList() ?? new List<POIContent>();
        var submittedIds = Contents?.Where(c => c.Id > 0).Select(c => c.Id).ToHashSet() ?? new HashSet<int>();

        // Remove deleted contents
        foreach (var existing in existingContents)
        {
            if (!submittedIds.Contains(existing.Id))
            {
                _context.Set<POIContent>().Remove(existing);
            }
        }

        // Update or add contents
        if (Contents != null)
        {
            foreach (var content in Contents)
            {
                if (content.Id > 0)
                {
                    // Update existing
                    var existing = existingContents.FirstOrDefault(c => c.Id == content.Id);
                    if (existing != null)
                    {
                        existing.Language = content.Language;
                        existing.TextContent = content.TextContent;
                        existing.ContentType = (ContentType)content.ContentType;
                    }
                }
                else
                {
                    // Add new
                    var newContent = new POIContent
                    {
                        POId = poi.Id,
                        Language = content.Language,
                        TextContent = content.TextContent,
                        ContentType = (ContentType)content.ContentType,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Set<POIContent>().Add(newContent);
                }
            }
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Cập nhật thông tin thành công!";
        return RedirectToAction(nameof(Edit), new { id });
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

        from ??= DateTime.UtcNow.AddDays(-30);
        to ??= DateTime.UtcNow;

        var visits = await _context.VisitLogs
            .Where(v => v.POId == id && v.VisitTime >= from && v.VisitTime <= to)
            .OrderByDescending(v => v.VisitTime)
            .ToListAsync();

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
