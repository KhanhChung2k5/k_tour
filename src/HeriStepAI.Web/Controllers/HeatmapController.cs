using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeriStepAI.Web.Controllers;

[Authorize(Roles = "Admin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class HeatmapController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HeatmapController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> Pois()
    {
        var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("poi");
        if (!response.IsSuccessStatusCode)
            return Content("[]", "application/json");
        var json = await response.Content.ReadAsStringAsync();
        return Content(json, "application/json");
    }

    [HttpGet]
    public async Task<IActionResult> Data([FromQuery] string range = "30d")
    {
        var client = CreateAuthenticatedClient();

        DateTime? startDate = range switch
        {
            "7d"  => DateTime.UtcNow.AddDays(-7),
            "30d" => DateTime.UtcNow.AddDays(-30),
            _     => null
        };

        var url = startDate.HasValue
            ? $"analytics/heatmap?startDate={Uri.EscapeDataString(startDate.Value.ToString("O"))}"
            : "analytics/heatmap";

        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return Content("[]", "application/json");

        var json = await response.Content.ReadAsStringAsync();
        return Content(json, "application/json");
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _httpClientFactory.CreateClient("API");
        var token  = Request.Cookies["AuthToken"];
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
