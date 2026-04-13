using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HeriStepAI.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeriStepAI.Web.Controllers;

[Authorize(Roles = "Admin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ShopOwnersController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ShopOwnersController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index([FromQuery] string? status)
    {
        var client = CreateAuthenticatedClient();
        var q = string.IsNullOrWhiteSpace(status) ? "" : $"?status={Uri.EscapeDataString(status)}";
        var resp = await client.GetAsync("auth/shop-owners" + q);

        List<ShopOwnerRow> list = new();
        if (resp.IsSuccessStatusCode)
        {
            var json = await resp.Content.ReadAsStringAsync();
            list = JsonSerializer.Deserialize<List<ShopOwnerRow>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        else
        {
            TempData["Error"] = $"Không tải được danh sách ({(int)resp.StatusCode}).";
        }

        ViewBag.FilterStatus = status ?? "";
        ViewBag.Total    = list.Count;
        ViewBag.Pending  = list.Count(r => r.ApprovalStatus == "Pending");
        ViewBag.Approved = list.Count(r => r.ApprovalStatus == "Approved");
        ViewBag.Rejected = list.Count(r => r.ApprovalStatus == "Rejected");
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, [FromQuery] string? returnStatus)
    {
        var client = CreateAuthenticatedClient();
        var resp = await client.PostAsync($"auth/approve-shop-owner/{id}",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        if (resp.IsSuccessStatusCode)
            TempData["Success"] = "Đã duyệt tài khoản chủ quán.";
        else
            TempData["Error"] = await ReadErrorAsync(resp);
        return RedirectToAction(nameof(Index), new { status = returnStatus });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, [FromQuery] string? returnStatus)
    {
        var client = CreateAuthenticatedClient();
        var resp = await client.PostAsync($"auth/reject-shop-owner/{id}",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        if (resp.IsSuccessStatusCode)
            TempData["Success"] = "Đã từ chối đăng ký.";
        else
            TempData["Error"] = await ReadErrorAsync(resp);
        return RedirectToAction(nameof(Index), new { status = returnStatus });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, [FromQuery] string? returnStatus)
    {
        var client = CreateAuthenticatedClient();
        var resp = await client.PostAsync($"auth/toggle-active/{id}",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            TempData["Success"] = doc.RootElement.TryGetProperty("Message", out var m)
                ? m.GetString() : "Đã cập nhật trạng thái.";
        }
        else
        {
            TempData["Error"] = await ReadErrorAsync(resp);
        }
        return RedirectToAction(nameof(Index), new { status = returnStatus });
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage resp)
    {
        var body = await resp.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("Message", out var m)) return m.GetString() ?? "Lỗi API.";
        }
        catch { }
        return $"Lỗi API ({(int)resp.StatusCode}).";
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
