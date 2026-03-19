using System.Security.Claims;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeriStepAI.Web.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HomeController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToAction("Login", "Auth");

        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == "ShopOwner" || role == "2")
            return RedirectToAction("Dashboard", "ShopOwner");
        return RedirectToAction("Dashboard");
    }

    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == "ShopOwner" || role == "2")
        {
            return RedirectToAction("Dashboard", "ShopOwner");
        }

        try
        {
            var client = CreateAuthenticatedClient();

            // Tổng lượt ghé thăm + Tự động nhận diện (Geofence) — đồng bộ với trang Analytics
            var summaryTask = client.GetAsync("analytics/summary");
            // Top 10 địa điểm chỉ để hiển thị biểu đồ (không ảnh hưởng số liệu tổng)
            var topPoisTask = client.GetAsync("analytics/top-pois?count=10");
            var poisClient = CreateAuthenticatedClient();
            var poisTask = poisClient.GetAsync("poi");

            await Task.WhenAll(summaryTask, topPoisTask, poisTask);

            var summaryResponse = summaryTask.Result;
            var topPoisResponse = topPoisTask.Result;
            var poisResponse = poisTask.Result;

            if (summaryResponse.IsSuccessStatusCode)
            {
                var content = await summaryResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                ViewBag.TotalVisits = root.TryGetProperty("TotalVisits", out var tv) ? tv.GetInt32() : 0;
                ViewBag.GeofenceVisits = root.TryGetProperty("Geofence", out var gf) ? gf.GetInt32() : 0;
            }
            else
            {
                Console.WriteLine($"[Dashboard] analytics/summary failed: {(int)summaryResponse.StatusCode} {summaryResponse.ReasonPhrase}");
                ViewBag.TotalVisits = 0;
                ViewBag.GeofenceVisits = 0;
            }

            if (topPoisResponse.IsSuccessStatusCode)
            {
                var content = await topPoisResponse.Content.ReadAsStringAsync();
                ViewBag.TopPOIs = JsonSerializer.Deserialize<Dictionary<string, int>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new Dictionary<string, int>();
            }
            else
            {
                ViewBag.TopPOIs = new Dictionary<string, int>();
            }

            if (poisResponse.IsSuccessStatusCode)
            {
                var content = await poisResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var arr = doc.RootElement;
                ViewBag.TotalPOIs = arr.GetArrayLength();

                int active = 0;
                var poiNames = new Dictionary<string, string>();
                foreach (var el in arr.EnumerateArray())
                {
                    if (el.TryGetProperty("IsActive", out var a) && a.GetBoolean()) active++;
                    var id = el.TryGetProperty("Id", out var idEl) ? idEl.GetInt32().ToString() : "";
                    var name = el.TryGetProperty("Name", out var n) ? n.GetString() ?? "" : "";
                    if (!string.IsNullOrEmpty(id)) poiNames[id] = name;
                }
                ViewBag.ActivePOIs = active;
                ViewBag.POINames = poiNames;
            }
            else
            {
                ViewBag.TotalPOIs = 0;
                ViewBag.ActivePOIs = 0;
                ViewBag.POINames = new Dictionary<string, string>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Dashboard] Error: {ex.Message}");
            ViewBag.TopPOIs = new Dictionary<string, int>();
            ViewBag.TotalVisits = 0;
            ViewBag.GeofenceVisits = 0;
            ViewBag.TotalPOIs = 0;
            ViewBag.ActivePOIs = 0;
            ViewBag.POINames = new Dictionary<string, string>();
        }

        return View();
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
