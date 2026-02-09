using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HeriStepAI.Web.Models;

namespace HeriStepAI.Web.Controllers;

[Authorize]
public class POIController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public POIController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
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
        return View(new POIViewModel());
    }

    // POST: POI/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(POIViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
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
        // Serialize with PascalCase to match API expectations
        var json = JsonSerializer.Serialize(model);

        // Debug logging
        Console.WriteLine($"[POIController] Creating POI: {model.Name}");
        Console.WriteLine($"[POIController] Contents count: {model.Contents?.Count ?? 0}");
        if (model.Contents != null)
        {
            foreach (var c in model.Contents)
            {
                Console.WriteLine($"[POIController] Content - Lang: {c.Language}, TextLength: {c.TextContent?.Length ?? 0}, Type: {c.ContentType}");
            }
        }
        Console.WriteLine($"[POIController] Full JSON payload:");
        Console.WriteLine(json);

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("poi", content);

        Console.WriteLine($"[POIController] Response Status: {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            var successContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[POIController] Success response: {successContent}");
            TempData["Success"] = "Tạo POI thành công (bao gồm nội dung đa ngôn ngữ)";
            return RedirectToAction(nameof(Index));
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"[POIController] Error response: {errorContent}");
        TempData["Error"] = $"Lỗi khi tạo POI (Status: {response.StatusCode}): {errorContent}";
        return View(model);
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
    public async Task<IActionResult> Edit(int id, POIViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Build Contents list from form inputs (same as Create)
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

        // Debug logging
        Console.WriteLine($"[POIController] Updating POI ID {id}: {model.Name}");
        Console.WriteLine($"[POIController] Contents count: {model.Contents?.Count ?? 0}");
        Console.WriteLine($"[POIController] Full JSON payload:");
        Console.WriteLine(json);

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PutAsync($"poi/{id}", content);

        Console.WriteLine($"[POIController] Response Status: {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Cập nhật POI thành công (bao gồm nội dung đa ngôn ngữ)";
            return RedirectToAction(nameof(Index));
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"[POIController] Error response: {errorContent}");
        TempData["Error"] = $"Lỗi khi cập nhật POI (Status: {response.StatusCode}): {errorContent}";
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
