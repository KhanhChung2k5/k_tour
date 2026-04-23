using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeriStepAI.Web.Controllers;

[Authorize(Roles = "Admin")]
public class LoadTestController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public LoadTestController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> Run([FromBody] LoadTestRequest req)
    {
        if (req.DeviceCount <= 0 || req.DeviceCount > 200)
            return BadRequest(new { error = "DeviceCount phải từ 1 đến 200" });
        if (req.PoiId <= 0)
            return BadRequest(new { error = "PoiId không hợp lệ" });

        var token = Request.Cookies["AuthToken"];
        var tasks = Enumerable.Range(1, req.DeviceCount).Select(i => FireVisit(i, req.PoiId, token));
        var results = await Task.WhenAll(tasks);

        return Ok(new
        {
            total    = results.Length,
            success  = results.Count(r => r.Success),
            failed   = results.Count(r => !r.Success),
            avgMs    = results.Length > 0 ? (int)results.Average(r => r.ElapsedMs) : 0,
            maxMs    = results.Length > 0 ? results.Max(r => r.ElapsedMs) : 0,
            minMs    = results.Length > 0 ? results.Min(r => r.ElapsedMs) : 0,
            items    = results
        });
    }

    private async Task<DeviceResult> FireVisit(int index, int poiId, string? token)
    {
        var deviceId = $"dev_SIM{index:D3}";
        var sw = Stopwatch.StartNew();
        try
        {
            var client = _httpClientFactory.CreateClient("API");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = JsonSerializer.Serialize(new
            {
                poiId    = poiId,
                userId   = deviceId,
                latitude = (double?)null,
                longitude = (double?)null,
                visitType = 0  // Geofence
            });

            var content  = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("analytics/visit", content);
            sw.Stop();

            return new DeviceResult
            {
                DeviceId  = deviceId,
                Success   = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new DeviceResult
            {
                DeviceId   = deviceId,
                Success    = false,
                StatusCode = 0,
                ElapsedMs  = sw.ElapsedMilliseconds,
                Error      = ex.Message
            };
        }
    }
}

public class LoadTestRequest
{
    public int DeviceCount { get; set; }
    public int PoiId { get; set; }
}

public class DeviceResult
{
    public string DeviceId  { get; set; } = "";
    public bool   Success   { get; set; }
    public int    StatusCode { get; set; }
    public long   ElapsedMs { get; set; }
    public string? Error    { get; set; }
}
