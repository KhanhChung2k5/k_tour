using System.Net.Http.Headers;
using System.Text.Json;
using HeriStepAI.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeriStepAI.Web.Controllers;

[Authorize(Roles = "Admin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class DevicesController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DevicesController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index([FromQuery] int page = 1)
    {
        var client = CreateAuthenticatedClient();
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        DeviceSummary? summary = null;
        DevicePageResult pageResult = new();

        var summaryTask = client.GetAsync("analytics/devices/summary");
        var listTask    = client.GetAsync($"analytics/devices?page={page}&pageSize=50");

        await Task.WhenAll(summaryTask, listTask);

        if (summaryTask.Result.IsSuccessStatusCode)
        {
            var json = await summaryTask.Result.Content.ReadAsStringAsync();
            summary = JsonSerializer.Deserialize<DeviceSummary>(json, opts);
        }

        if (listTask.Result.IsSuccessStatusCode)
        {
            var json = await listTask.Result.Content.ReadAsStringAsync();
            pageResult = JsonSerializer.Deserialize<DevicePageResult>(json, opts) ?? new();
        }
        else
        {
            TempData["Error"] = $"Không tải được danh sách thiết bị ({(int)listTask.Result.StatusCode}).";
        }

        ViewBag.Summary     = summary;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages  = pageResult.PageSize > 0
            ? (int)Math.Ceiling((double)pageResult.Total / pageResult.PageSize)
            : 1;

        return View(pageResult.Items);
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
