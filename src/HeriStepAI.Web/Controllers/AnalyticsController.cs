using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace HeriStepAI.Web.Controllers;

[Authorize]
public class AnalyticsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AnalyticsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("analytics/top-pois?count=10");
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var topPOIs = JsonSerializer.Deserialize<Dictionary<int, int>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            ViewBag.TopPOIs = topPOIs ?? new Dictionary<int, int>();
        }

        return View();
    }

    public async Task<IActionResult> POIDetails(int id)
    {
        var client = CreateAuthenticatedClient();
        var statsResponse = await client.GetAsync($"analytics/poi/{id}/statistics");
        var logsResponse = await client.GetAsync($"analytics/poi/{id}/logs");

        if (statsResponse.IsSuccessStatusCode)
        {
            var statsContent = await statsResponse.Content.ReadAsStringAsync();
            ViewBag.Statistics = JsonSerializer.Deserialize<object>(statsContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        if (logsResponse.IsSuccessStatusCode)
        {
            var logsContent = await logsResponse.Content.ReadAsStringAsync();
            ViewBag.Logs = JsonSerializer.Deserialize<object[]>(logsContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
