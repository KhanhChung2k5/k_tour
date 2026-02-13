using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HeriStepAI.Web.Models;
using HeriStepAI.Web.Services;

namespace HeriStepAI.Web.Controllers;

[Authorize(Roles = "Admin")]
public class POIController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISupabaseStorageService _storageService;

    public POIController(IHttpClientFactory httpClientFactory, ISupabaseStorageService storageService)
    {
        _httpClientFactory = httpClientFactory;
        _storageService = storageService;
    }

    // GET: POI
    public async Task<IActionResult> Index()
    {
        var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("poi");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var pois = JsonSerializer.Deserialize<List<POIViewModel>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<POIViewModel>();
            return View(pois);
        }

        TempData["Error"] = "Không thể tải danh sách POI";
        return View(new List<POIViewModel>());
    }

    // GET: POI/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var client = CreateAuthenticatedClient();
        var response = await client.GetAsync($"poi/{id}");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var poi = JsonSerializer.Deserialize<POIViewModel>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return View(poi);
        }

        TempData["Error"] = "Không tìm thấy POI";
        return RedirectToAction(nameof(Index));
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

            if (!string.IsNullOrWhiteSpace(model.TextContent_vi))
            {
                contents.Add(new POIContentViewModel
                {
                    Language = "vi",
                    TextContent = model.TextContent_vi,
                    ContentType = 1 // TTS
                });
            }

            if (!string.IsNullOrWhiteSpace(model.TextContent_en))
            {
                contents.Add(new POIContentViewModel
                {
                    Language = "en",
                    TextContent = model.TextContent_en,
                    ContentType = 1 // TTS
                });
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
        var client = CreateAuthenticatedClient();
        var response = await client.GetAsync($"poi/{id}");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var poi = JsonSerializer.Deserialize<POIViewModel>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Extract Contents to form fields
            if (poi != null && poi.Contents != null)
            {
                var viContent = poi.Contents.FirstOrDefault(c => c.Language == "vi");
                var enContent = poi.Contents.FirstOrDefault(c => c.Language == "en");

                poi.TextContent_vi = viContent?.TextContent;
                poi.TextContent_en = enContent?.TextContent;
            }

            return View(poi);
        }

        TempData["Error"] = "Không tìm thấy POI";
        return RedirectToAction(nameof(Index));
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

        // Upload new image if provided
        if (ImageFile != null && ImageFile.Length > 0)
        {
            using var stream = ImageFile.OpenReadStream();
            var imageUrl = await _storageService.UploadImageAsync(stream, ImageFile.FileName, ImageFile.ContentType);
            if (imageUrl != null)
            {
                model.ImageUrl = imageUrl;
            }
        }

        // Build Contents list from form inputs
        model.Contents = new List<POIContentViewModel>();

        if (!string.IsNullOrWhiteSpace(model.TextContent_vi))
        {
            model.Contents.Add(new POIContentViewModel
            {
                Language = "vi",
                TextContent = model.TextContent_vi,
                ContentType = 1 // TTS
            });
        }

        if (!string.IsNullOrWhiteSpace(model.TextContent_en))
        {
            model.Contents.Add(new POIContentViewModel
            {
                Language = "en",
                TextContent = model.TextContent_en,
                ContentType = 1 // TTS
            });
        }

        var client = CreateAuthenticatedClient();
        var json = JsonSerializer.Serialize(model);

        Console.WriteLine($"[POIController] Updating POI ID {id}: {model.Name}");

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PutAsync($"poi/{id}", content);

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Cập nhật POI thành công!";
            return RedirectToAction(nameof(Index));
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        TempData["Error"] = $"Lỗi khi cập nhật POI: {errorContent}";
        return View(model);
    }

    // POST: POI/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var client = CreateAuthenticatedClient();
        var response = await client.DeleteAsync($"poi/{id}");

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Xóa POI thành công";
        }
        else
        {
            TempData["Error"] = "Lỗi khi xóa POI";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: POI/ToggleActive/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var client = CreateAuthenticatedClient();

        // Get current POI
        var getResponse = await client.GetAsync($"poi/{id}");
        if (!getResponse.IsSuccessStatusCode)
        {
            TempData["Error"] = "Không tìm thấy POI";
            return RedirectToAction(nameof(Index));
        }

        var content = await getResponse.Content.ReadAsStringAsync();
        var poi = JsonSerializer.Deserialize<POIViewModel>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (poi != null)
        {
            poi.IsActive = !poi.IsActive;
            var json = JsonSerializer.Serialize(poi);
            var updateContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"poi/{id}", updateContent);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = poi.IsActive ? "Đã kích hoạt POI" : "Đã vô hiệu hóa POI";
            }
            else
            {
                TempData["Error"] = "Lỗi khi cập nhật trạng thái POI";
            }
        }

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
