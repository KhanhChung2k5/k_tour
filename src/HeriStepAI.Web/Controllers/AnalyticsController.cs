using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace HeriStepAI.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AnalyticsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AnalyticsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var client = CreateAuthenticatedClient();
            var poisClient = CreateAuthenticatedClient();

            var topPoisTask = client.GetAsync("analytics/top-pois?count=10");
            var poisTask = poisClient.GetAsync("poi");

            await Task.WhenAll(topPoisTask, poisTask);

            // Parse top POIs
            var topPOIs = new Dictionary<string, int>();
            if (topPoisTask.Result.IsSuccessStatusCode)
            {
                var content = await topPoisTask.Result.Content.ReadAsStringAsync();
                topPOIs = JsonSerializer.Deserialize<Dictionary<string, int>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            ViewBag.TopPOIs = topPOIs;

            // Parse POI names
            var poiNames = new Dictionary<string, string>();
            if (poisTask.Result.IsSuccessStatusCode)
            {
                var content = await poisTask.Result.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    var id = el.TryGetProperty("Id", out var idEl) ? idEl.GetInt32().ToString() : "";
                    var name = el.TryGetProperty("Name", out var n) ? n.GetString() ?? "" : "";
                    if (!string.IsNullOrEmpty(id)) poiNames[id] = name;
                }
            }
            ViewBag.POINames = poiNames;

            // Fetch visit type breakdown for each top POI (parallel)
            int totalGeofence = 0, totalManual = 0;
            var breakdownTasks = topPOIs.Keys.Select(async poiId =>
            {
                var c = CreateAuthenticatedClient();
                var resp = await c.GetAsync($"analytics/poi/{poiId}/statistics");
                if (!resp.IsSuccessStatusCode) return (poiId, 0, 0);

                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                int geo = 0, manual = 0;
                if (root.TryGetProperty("VisitsByType", out var vbt))
                {
                    if (vbt.TryGetProperty("Geofence", out var g)) geo = g.GetInt32();
                    if (vbt.TryGetProperty("MapClick", out var m)) manual = m.GetInt32();
                }
                return (poiId, geo, manual);
            }).ToList();

            var results = await Task.WhenAll(breakdownTasks);
            var geoByPoi = new Dictionary<string, int>();
            var manualByPoi = new Dictionary<string, int>();

            foreach (var (poiId, geo, manual) in results)
            {
                geoByPoi[poiId] = geo;
                manualByPoi[poiId] = manual;
                totalGeofence += geo;
                totalManual += manual;
            }

            ViewBag.GeofenceByPOI = geoByPoi;
            ViewBag.ManualByPOI = manualByPoi;
            ViewBag.TotalGeofence = totalGeofence;
            ViewBag.TotalManual = totalManual;
            ViewBag.TotalVisits = totalGeofence + totalManual;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Analytics] Error: {ex.Message}");
            ViewBag.TopPOIs = new Dictionary<string, int>();
            ViewBag.POINames = new Dictionary<string, string>();
            ViewBag.GeofenceByPOI = new Dictionary<string, int>();
            ViewBag.ManualByPOI = new Dictionary<string, int>();
            ViewBag.TotalGeofence = 0;
            ViewBag.TotalManual = 0;
            ViewBag.TotalVisits = 0;
        }

        return View();
    }

    public async Task<IActionResult> POIDetails(int id)
    {
        var statsClient = CreateAuthenticatedClient();
        var logsClient = CreateAuthenticatedClient();
        var statsResponse = await statsClient.GetAsync($"analytics/poi/{id}/statistics");
        var logsResponse = await logsClient.GetAsync($"analytics/poi/{id}/logs");

        if (statsResponse.IsSuccessStatusCode)
        {
            var statsContent = await statsResponse.Content.ReadAsStringAsync();
            ViewBag.Statistics = JsonSerializer.Deserialize<object>(statsContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        if (logsResponse.IsSuccessStatusCode)
        {
            var logsContent = await logsResponse.Content.ReadAsStringAsync();
            ViewBag.Logs = JsonSerializer.Deserialize<object[]>(logsContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
