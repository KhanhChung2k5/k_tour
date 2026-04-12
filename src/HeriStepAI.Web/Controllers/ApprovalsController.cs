using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HeriStepAI.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeriStepAI.Web.Controllers;

[Authorize(Roles = "Admin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ApprovalsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ApprovalsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        var client = CreateAuthenticatedClient();
        var resp = await client.GetAsync("auth/pending-shop-owners");
        if (!resp.IsSuccessStatusCode)
        {
            TempData["Error"] = $"Không tải được danh sách chờ duyệt ({(int)resp.StatusCode}).";
            return View(new List<PendingShopOwnerViewModel>());
        }

        var json = await resp.Content.ReadAsStringAsync();
        var list = JsonSerializer.Deserialize<List<PendingShopOwnerViewModel>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<PendingShopOwnerViewModel>();
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var client = CreateAuthenticatedClient();
        var resp = await client.PostAsync($"auth/approve-shop-owner/{id}",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        if (resp.IsSuccessStatusCode)
            TempData["Success"] = "Đã duyệt tài khoản chủ quán.";
        else
            TempData["Error"] = await ReadApiErrorAsync(resp, (int)resp.StatusCode);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var client = CreateAuthenticatedClient();
        var resp = await client.PostAsync($"auth/reject-shop-owner/{id}",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        if (resp.IsSuccessStatusCode)
            TempData["Success"] = "Đã từ chối đăng ký.";
        else
            TempData["Error"] = await ReadApiErrorAsync(resp, (int)resp.StatusCode);
        return RedirectToAction(nameof(Index));
    }

    private static async Task<string> ReadApiErrorAsync(HttpResponseMessage resp, int statusCode)
    {
        var body = await resp.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("Message", out var m))
                return m.GetString() ?? $"Lỗi API ({statusCode})";
            if (doc.RootElement.TryGetProperty("title", out var t))
                return t.GetString() ?? $"Lỗi API ({statusCode})";
        }
        catch { /* ignore */ }
        if (!string.IsNullOrWhiteSpace(body) && body.Length < 400)
            return $"Lỗi API ({statusCode}): {body}";
        return $"Lỗi API ({statusCode}). Kiểm tra API đang trỏ đúng database (Supabase) và cột ApprovalStatus.";
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
