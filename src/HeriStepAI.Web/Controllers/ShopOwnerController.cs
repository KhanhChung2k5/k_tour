using HeriStepAI.API.Data;
using HeriStepAI.API.Models;
using HeriStepAI.API.Services;
using HeriStepAI.Web.Models;
using HeriStepAI.Web.Services;
using HeriStepAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeriStepAI.Web.Controllers;

[Authorize(Roles = "ShopOwner")]
public class ShopOwnerController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ISupabaseStorageService _storageService;
    private readonly IPOIContentTranslationSyncService _translationSync;

    public ShopOwnerController(
        ApplicationDbContext context,
        ISupabaseStorageService storageService,
        IPOIContentTranslationSyncService translationSync)
    {
        _context = context;
        _storageService = storageService;
        _translationSync = translationSync;
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

    // GET: /ShopOwner/Create
    public IActionResult Create()
    {
        return View(new ShopOwnerPOICreateViewModel());
    }

    // POST: /ShopOwner/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ShopOwnerPOICreateViewModel model, IFormFile? ImageFile)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        if (!ModelState.IsValid)
            return View(model);

        string? imageUrl = model.ImageUrl;
        if (ImageFile != null && ImageFile.Length > 0)
        {
            using var stream = ImageFile.OpenReadStream();
            imageUrl = await _storageService.UploadImageAsync(stream, ImageFile.FileName, ImageFile.ContentType);
            if (imageUrl == null)
            {
                TempData["Error"] = "Không upload được ảnh. Thử lại.";
                return View(model);
            }
        }

        var poi = new POI
        {
            Name = model.Name,
            Description = model.Description,
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            Address = model.Address,
            Radius = model.Radius,
            Priority = model.Priority,
            OwnerId = userId.Value,
            ImageUrl = imageUrl,
            MapLink = model.MapLink,
            IsActive = model.IsActive,
            Category = model.Category,
            TourId = model.TourId,
            EstimatedMinutes = model.EstimatedMinutes,
            FoodType = model.FoodType,
            PriceMin = model.PriceMin,
            PriceMax = model.PriceMax,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.POIs.Add(poi);
        await _context.SaveChangesAsync();

        var langFields = new[]
        {
            ("vi", model.TextContent_vi),
            ("en", model.TextContent_en),
            ("ko", model.TextContent_ko),
            ("zh", model.TextContent_zh),
            ("ja", model.TextContent_ja),
            ("th", model.TextContent_th),
            ("fr", model.TextContent_fr)
        };

        foreach (var (lang, text) in langFields)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                _context.POIContents.Add(new POIContent
                {
                    POId = poi.Id,
                    Language = lang,
                    TextContent = text,
                    ContentType = ContentType.TTS,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(model.TextContent_vi))
        {
            try
            {
                await _translationSync.SyncFromVietnameseAsync(poi.Id);
                TempData["Success"] = "Đã tạo địa điểm. Nội dung các ngôn ngữ khác đã được dịch từ tiếng Việt.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ShopOwner Create] Translation sync: {ex.Message}");
                TempData["Success"] = "Đã tạo địa điểm. Bạn có thể bổ sung thuyết minh các ngôn ngữ sau.";
            }
        }
        else
            TempData["Success"] = "Đã tạo địa điểm.";

        return RedirectToAction(nameof(Edit), new { id = poi.Id });
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

        var oldViText = poi.Contents?.FirstOrDefault(c => c.Language == "vi")?.TextContent?.Trim() ?? "";

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

        var newViText = Contents?.FirstOrDefault(c => string.Equals(c.Language, "vi", StringComparison.OrdinalIgnoreCase))?.TextContent?.Trim() ?? "";
        var viChanged = !string.Equals(oldViText, newViText, StringComparison.Ordinal);
        if (viChanged && !string.IsNullOrWhiteSpace(newViText))
        {
            try
            {
                await _translationSync.SyncFromVietnameseAsync(poi.Id);
                TempData["Success"] = "Cập nhật thành công. Nội dung thuyết minh các ngôn ngữ khác đã được làm mới từ bản tiếng Việt (dịch tự động).";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ShopOwner] Translation sync failed: {ex.Message}");
                TempData["Success"] = "Cập nhật thông tin thành công. Không thể tự động dịch sang các ngôn ngữ khác lúc này — bạn có thể sửa tay hoặc thử lại sau.";
            }
        }
        else
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
