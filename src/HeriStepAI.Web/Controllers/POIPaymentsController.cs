using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HeriStepAI.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeriStepAI.Web.Controllers;

[Authorize(Roles = "Admin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class POIPaymentsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public POIPaymentsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index([FromQuery] string? status)
    {
        var client = CreateAuthenticatedClient();
        var q = string.IsNullOrWhiteSpace(status) ? "" : $"?status={Uri.EscapeDataString(status)}";
        var resp = await client.GetAsync("poi-payments" + q);
        if (!resp.IsSuccessStatusCode)
        {
            TempData["Error"] = $"Không tải được danh sách ({(int)resp.StatusCode}).";
            return View(new List<POIPaymentRow>());
        }

        var json = await resp.Content.ReadAsStringAsync();
        var list = JsonSerializer.Deserialize<List<POIPaymentRow>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<POIPaymentRow>();

        var sumResp = await client.GetAsync("poi-payments/summary");
        POIPaymentSummary? summary = null;
        if (sumResp.IsSuccessStatusCode)
        {
            var sumJson = await sumResp.Content.ReadAsStringAsync();
            summary = JsonSerializer.Deserialize<POIPaymentSummary>(sumJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        ViewBag.Summary = summary;
        ViewBag.FilterStatus = status ?? "";
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(int id, string? note)
    {
        var client = CreateAuthenticatedClient();
        var body = JsonSerializer.Serialize(new { note });
        var resp = await client.PostAsync($"poi-payments/{id}/verify",
            new StringContent(body, Encoding.UTF8, "application/json"));
        if (resp.IsSuccessStatusCode)
            TempData["Success"] = "Đã xác nhận. POI đã được kích hoạt.";
        else
            TempData["Error"] = await resp.Content.ReadAsStringAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? note)
    {
        var client = CreateAuthenticatedClient();
        var body = JsonSerializer.Serialize(new { note });
        var resp = await client.PostAsync($"poi-payments/{id}/reject",
            new StringContent(body, Encoding.UTF8, "application/json"));
        if (resp.IsSuccessStatusCode)
            TempData["Success"] = "Đã từ chối. POI không được kích hoạt.";
        else
            TempData["Error"] = await resp.Content.ReadAsStringAsync();
        return RedirectToAction(nameof(Index));
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _httpClientFactory.CreateClient("API");
        var token = Request.Cookies["AuthToken"];
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
