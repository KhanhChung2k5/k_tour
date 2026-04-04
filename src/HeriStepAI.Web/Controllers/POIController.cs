using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HeriStepAI.Web.Models;
using HeriStepAI.Web.Services;
using HeriStepAI.API.Data;
using HeriStepAI.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HeriStepAI.Web.Controllers;

[Authorize(Roles = "Admin")]
public class POIController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISupabaseStorageService _storageService;
    private readonly ApplicationDbContext _context;

    public POIController(IHttpClientFactory httpClientFactory, ISupabaseStorageService storageService, ApplicationDbContext context)
    {
        _httpClientFactory = httpClientFactory;
        _storageService = storageService;
        _context = context;
    }

    // GET: POI
    public async Task<IActionResult> Index()
    {
        var pois = await _context.POIs
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new POIViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Address = p.Address,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                ImageUrl = p.ImageUrl,
                IsActive = p.IsActive,
                Category = p.Category,
                FoodType = p.FoodType,
                PriceMin = p.PriceMin,
                PriceMax = p.PriceMax,
                Priority = p.Priority,
                Rating = p.Rating,
                ReviewCount = p.ReviewCount,
                OwnerId = p.OwnerId,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return View(pois);
    }

    // GET: POI/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var p = await _context.POIs
            .Include(x => x.Contents)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p == null)
        {
            TempData["Error"] = "Không tìm thấy POI";
            return RedirectToAction(nameof(Index));
        }

        var poi = new POIViewModel
        {
            Id = p.Id, Name = p.Name, Description = p.Description,
            Address = p.Address, Latitude = p.Latitude, Longitude = p.Longitude,
            ImageUrl = p.ImageUrl, MapLink = p.MapLink, IsActive = p.IsActive,
            Category = p.Category, FoodType = p.FoodType, PriceMin = p.PriceMin,
            PriceMax = p.PriceMax, Priority = p.Priority, Rating = p.Rating,
            ReviewCount = p.ReviewCount, OwnerId = p.OwnerId, TourId = p.TourId,
            EstimatedMinutes = p.EstimatedMinutes, Radius = p.Radius,
            CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt,
            Contents = p.Contents?.Select(c => new POIContentViewModel
            {
                Id = c.Id, POId = c.POId, Language = c.Language,
                TextContent = c.TextContent, AudioUrl = c.AudioUrl,
                ContentType = (int)c.ContentType, CreatedAt = c.CreatedAt
            }).ToList()
        };

        return View(poi);
    }

    // GET: POI/Create
    public IActionResult Create()
    {
        return View(new CreatePOIWithOwnerViewModel());
    }

    // POST: POI/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePOIWithOwnerViewModel model, IFormFile? ImageFile)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var client = CreateAuthenticatedClient();

        try
        {
            // Step 1: Create ShopOwner account
            Console.WriteLine($"[POIController] Creating ShopOwner account: {model.OwnerUsername}");

            var registerRequest = new
            {
                Username = model.OwnerUsername,
                Email = model.OwnerEmail,
                Password = model.OwnerPassword,
                FullName = model.OwnerFullName,
                Phone = model.OwnerPhone,
                Role = 2 // ShopOwner
            };

            var registerJson = JsonSerializer.Serialize(registerRequest);
            var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
            var registerResponse = await client.PostAsync("auth/register", registerContent);

            if (!registerResponse.IsSuccessStatusCode)
            {
                var errorContent = await registerResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"[POIController] Error creating owner account: {errorContent}");
                TempData["Error"] = $"Lỗi khi tạo tài khoản chủ quán: {errorContent}";
                return View(model);
            }

            var userResult = await registerResponse.Content.ReadAsStringAsync();
            var userResponse = JsonSerializer.Deserialize<JsonElement>(userResult, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            var ownerId = userResponse.GetProperty("userId").GetInt32();

            Console.WriteLine($"[POIController] Owner account created successfully. OwnerId: {ownerId}");

            // Step 2: Upload image if provided
            if (ImageFile != null && ImageFile.Length > 0)
            {
                using var stream = ImageFile.OpenReadStream();
                var imageUrl = await _storageService.UploadImageAsync(stream, ImageFile.FileName, ImageFile.ContentType);
                if (imageUrl != null)
                {
                    model.ImageUrl = imageUrl;
                }
                else
                {
                    TempData["Error"] = "Lỗi khi upload hình ảnh. Vui lòng thử lại.";
                    return View(model);
                }
            }

            // Step 3: Build Contents list
            var contents = new List<POIContentViewModel>();

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
                    contents.Add(new POIContentViewModel
                    {
                        Language = lang,
                        TextContent = text,
                        ContentType = 1 // TTS
                    });
                }
            }

            // Step 4: Create POI with OwnerId
            var poiData = new POIViewModel
            {
                Name = model.Name,
                Description = model.Description,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Address = model.Address,
                Radius = model.Radius,
                Priority = model.Priority,
                OwnerId = ownerId, // Link to owner account
                ImageUrl = model.ImageUrl,
                MapLink = model.MapLink,
                IsActive = model.IsActive,
                Category = model.Category,
                TourId = model.TourId,
                EstimatedMinutes = model.EstimatedMinutes,
                FoodType = model.FoodType,
                PriceMin = model.PriceMin,
                PriceMax = model.PriceMax,
                Contents = contents
            };

            var poiJson = JsonSerializer.Serialize(poiData);
            Console.WriteLine($"[POIController] Creating POI: {model.Name} with OwnerId: {ownerId}");

            var poiContent = new StringContent(poiJson, Encoding.UTF8, "application/json");
            var poiResponse = await client.PostAsync("poi", poiContent);

            if (poiResponse.IsSuccessStatusCode)
            {
                TempData["Success"] = $"✅ Tạo POI thành công! Tài khoản chủ quán: {model.OwnerUsername} ({model.OwnerEmail})";
                return RedirectToAction(nameof(Index));
            }

            var poiError = await poiResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"[POIController] Error creating POI: {poiError}");
            TempData["Error"] = $"Tạo tài khoản thành công nhưng lỗi khi tạo POI: {poiError}";
            return View(model);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[POIController] Exception: {ex.Message}");
            TempData["Error"] = $"Lỗi: {ex.Message}";
            return View(model);
        }
    }

    // GET: POI/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var p = await _context.POIs
            .Include(x => x.Contents)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p == null)
        {
            TempData["Error"] = "Không tìm thấy POI";
            return RedirectToAction(nameof(Index));
        }

        var poi = new POIViewModel
        {
            Id = p.Id, Name = p.Name, Description = p.Description,
            Address = p.Address, Latitude = p.Latitude, Longitude = p.Longitude,
            ImageUrl = p.ImageUrl, MapLink = p.MapLink, IsActive = p.IsActive,
            Category = p.Category, FoodType = p.FoodType, PriceMin = p.PriceMin,
            PriceMax = p.PriceMax, Priority = p.Priority, Rating = p.Rating,
            ReviewCount = p.ReviewCount, OwnerId = p.OwnerId, TourId = p.TourId,
            EstimatedMinutes = p.EstimatedMinutes, Radius = p.Radius,
            CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt,
            TextContent_vi = p.Contents?.FirstOrDefault(c => c.Language == "vi")?.TextContent,
            TextContent_en = p.Contents?.FirstOrDefault(c => c.Language == "en")?.TextContent,
            TextContent_ko = p.Contents?.FirstOrDefault(c => c.Language == "ko")?.TextContent,
            TextContent_zh = p.Contents?.FirstOrDefault(c => c.Language == "zh")?.TextContent,
            TextContent_ja = p.Contents?.FirstOrDefault(c => c.Language == "ja")?.TextContent,
            TextContent_th = p.Contents?.FirstOrDefault(c => c.Language == "th")?.TextContent,
            TextContent_fr = p.Contents?.FirstOrDefault(c => c.Language == "fr")?.TextContent
        };

        return View(poi);
    }

    // POST: POI/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, POIViewModel model, IFormFile? ImageFile)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existing = await _context.POIs
            .Include(p => p.Contents)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (existing == null)
        {
            TempData["Error"] = "Không tìm thấy POI";
            return RedirectToAction(nameof(Index));
        }

        // Upload new image if provided
        if (ImageFile != null && ImageFile.Length > 0)
        {
            using var stream = ImageFile.OpenReadStream();
            var imageUrl = await _storageService.UploadImageAsync(stream, ImageFile.FileName, ImageFile.ContentType);
            if (imageUrl != null)
            {
                existing.ImageUrl = imageUrl;
                Console.WriteLine($"[POIController] Image updated to: {imageUrl}");
            }
            else
            {
                TempData["Error"] = "Lỗi khi upload hình ảnh lên Supabase. Vui lòng thử lại.";
                return View(model);
            }
        }

        // Update basic fields directly on the tracked entity
        existing.Name = model.Name;
        existing.Description = model.Description;
        existing.Latitude = model.Latitude;
        existing.Longitude = model.Longitude;
        existing.Address = model.Address;
        existing.Radius = model.Radius;
        existing.Priority = model.Priority;
        existing.MapLink = model.MapLink;
        existing.IsActive = model.IsActive;
        existing.Category = model.Category;
        existing.FoodType = model.FoodType;
        existing.PriceMin = model.PriceMin;
        existing.PriceMax = model.PriceMax;
        existing.EstimatedMinutes = model.EstimatedMinutes;
        existing.TourId = model.TourId;
        existing.UpdatedAt = DateTime.UtcNow;

        // Rebuild Contents
        if (existing.Contents != null && existing.Contents.Any())
        {
            _context.Set<POIContent>().RemoveRange(existing.Contents);
        }

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

        existing.Contents = new List<POIContent>();
        foreach (var (lang, text) in langFields)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                existing.Contents.Add(new POIContent
                {
                    POId = existing.Id,
                    Language = lang,
                    TextContent = text,
                    ContentType = ContentType.TTS,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
        Console.WriteLine($"[POIController] POI {id} saved directly to DB. ImageUrl: {existing.ImageUrl}");

        TempData["Success"] = "Cập nhật POI thành công!";
        return RedirectToAction(nameof(Index));
    }

    // POST: POI/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var poi = await _context.POIs.FindAsync(id);
        if (poi != null)
        {
            poi.IsActive = false;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa POI thành công";
        }
        else
        {
            TempData["Error"] = "Không tìm thấy POI";
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: POI/ToggleActive/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var poi = await _context.POIs.FindAsync(id);
        if (poi == null)
        {
            TempData["Error"] = "Không tìm thấy POI";
            return RedirectToAction(nameof(Index));
        }

        poi.IsActive = !poi.IsActive;
        await _context.SaveChangesAsync();
        TempData["Success"] = poi.IsActive ? "Đã kích hoạt POI" : "Đã vô hiệu hóa POI";
        return RedirectToAction(nameof(Index));
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _httpClientFactory.CreateClient("API");
        var token = Request.Cookies["AuthToken"];
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return client;
    }
}
