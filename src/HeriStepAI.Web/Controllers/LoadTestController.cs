using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HeriStepAI.API.Data;
using HeriStepAI.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeriStepAI.Web.Controllers;

[Authorize(Roles = "Admin")]
public class LoadTestController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogRunner _logRunner;
    private readonly ApplicationDbContext _db;

    public LoadTestController(IHttpClientFactory httpClientFactory, ILogRunner logRunner,
        ApplicationDbContext db)
    {
        _httpClientFactory = httpClientFactory;
        _logRunner = logRunner;
        _db = db;
    }

    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult GeofenceSimulator() => View();

    [HttpGet]
    public async Task<IActionResult> POIs()
    {
        var client = CreateAuthenticatedClient();
        var resp = await client.GetAsync("poi");
        if (!resp.IsSuccessStatusCode)
            return StatusCode((int)resp.StatusCode, new { error = "Không tải được danh sách POI." });

        var json = await resp.Content.ReadAsStringAsync();
        return Content(json, "application/json", Encoding.UTF8);
    }

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
            total   = results.Length,
            success = results.Count(r => r.Success),
            failed  = results.Count(r => !r.Success),
            avgMs   = results.Length > 0 ? (int)results.Average(r => r.ElapsedMs) : 0,
            maxMs   = results.Length > 0 ? results.Max(r => r.ElapsedMs) : 0,
            minMs   = results.Length > 0 ? results.Min(r => r.ElapsedMs) : 0,
            items   = results
        });
    }

    [HttpPost]
    public async Task<IActionResult> FireGeofenceVisit([FromBody] GeofenceVisitRequest req)
    {
        if (req == null || req.PoiId <= 0)
            return BadRequest(new { ok = false, error = "PoiId invalid" });

        var sw = Stopwatch.StartNew();
        try
        {
            var client  = _httpClientFactory.CreateClient("API");
            var payload = JsonSerializer.Serialize(new
            {
                poiId     = req.PoiId,
                userId    = req.DeviceId,
                latitude  = req.Latitude,
                longitude = req.Longitude,
                visitType = 1  // Geofence
            });
            var content  = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("analytics/visit", content);
            sw.Stop();
            return Ok(new { ok = response.IsSuccessStatusCode, status = (int)response.StatusCode, elapsedMs = sw.ElapsedMilliseconds });
        }
        catch (Exception ex)
        {
            sw.Stop();
            return Ok(new { ok = false, status = 0, elapsedMs = sw.ElapsedMilliseconds, error = ex.Message });
        }
    }

    /// <summary>
    /// Trả về các VisitLog của SIM-DEV-* được INSERT vào DB trong N giây gần nhất.
    /// Dùng để hiển thị server-side queue log trong GeofenceSimulator.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> RecentVisitLogs([FromQuery] int seconds = 60)
    {
        seconds = Math.Clamp(seconds, 5, 300);
        var since = DateTime.UtcNow.AddSeconds(-seconds);

        var logs = await _db.VisitLogs
            .Where(v => v.VisitTime >= since
                     && v.UserId != null
                     && v.UserId.StartsWith("SIM-DEV-"))
            .OrderBy(v => v.VisitTime)
            .Select(v => new
            {
                v.Id,
                v.POId,
                PoiName    = v.POI != null ? v.POI.Name : null,
                v.UserId,
                VisitTimeMs = new DateTimeOffset(v.VisitTime, TimeSpan.Zero).ToUnixTimeMilliseconds(),
                VisitTimeStr = v.VisitTime.ToString("HH:mm:ss.fff"),
                v.VisitType
            })
            .ToListAsync();

        return Ok(logs);
    }

    [HttpPost]
    public async Task<IActionResult> SaveQueueLog([FromBody] QueueLogSaveRequest req)
    {
        if (req == null) return BadRequest(new { error = "Payload rỗng." });
        await _logRunner.OverwriteAsync(req.Text ?? string.Empty, req.SessionId);
        return Ok(new { ok = true, path = _logRunner.LogFilePath });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadQueueLog()
    {
        if (!System.IO.File.Exists(_logRunner.LogFilePath))
            return NotFound("Chưa có file logqueue.txt");
        var bytes = await _logRunner.ReadBytesAsync();
        return File(bytes, "text/plain", "logqueue.txt");
    }

    [HttpGet]
    public async Task<IActionResult> ReadQueueLog()
    {
        var text = await _logRunner.ReadAsync();
        return Ok(new { text });
    }

    private async Task<DeviceResult> FireVisit(int index, int poiId, string? token)
    {
        var deviceId = $"dev_SIM{index:D3}";
        var sw = Stopwatch.StartNew();
        try
        {
            var client  = _httpClientFactory.CreateClient("API");
            var payload = JsonSerializer.Serialize(new
            {
                poiId     = poiId,
                userId    = deviceId,
                latitude  = (double?)null,
                longitude = (double?)null,
                visitType = 1  // Geofence
            });
            var content  = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("analytics/visit", content);
            sw.Stop();
            return new DeviceResult
            {
                DeviceId   = deviceId,
                Success    = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                ElapsedMs  = sw.ElapsedMilliseconds
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

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _httpClientFactory.CreateClient("API");
        var token  = Request.Cookies["AuthToken"];
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

public class LoadTestRequest
{
    public int DeviceCount { get; set; }
    public int PoiId       { get; set; }
}

public class DeviceResult
{
    public string  DeviceId   { get; set; } = "";
    public bool    Success    { get; set; }
    public int     StatusCode { get; set; }
    public long    ElapsedMs  { get; set; }
    public string? Error      { get; set; }
}

public class QueueLogSaveRequest
{
    public string? Text      { get; set; }
    public string? SessionId { get; set; }
}

public class GeofenceVisitRequest
{
    public int     PoiId     { get; set; }
    public string? DeviceId  { get; set; }
    public double? Latitude  { get; set; }
    public double? Longitude { get; set; }
}
