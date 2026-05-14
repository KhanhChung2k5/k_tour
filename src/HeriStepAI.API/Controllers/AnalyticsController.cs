using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using HeriStepAI.API.Models;
using HeriStepAI.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HeriStepAI.API.Controllers;

// Singleton tracker — sống cùng vòng đời ứng dụng, không cần DB

/// <summary>
/// Tracker để theo dõi session active.
/// </summary>
public static class HeartbeatTracker
{
    private static readonly TimeSpan Threshold = TimeSpan.FromSeconds(3);
    private static readonly ConcurrentDictionary<string, DateTime> _sessions = new();

    public static void Touch(string userId)
    {
        _sessions[userId] = DateTime.UtcNow;
        Cleanup();
    }

    /// <summary>
    /// Số lượng session active.
    /// </summary>
    public static int Count
    {
        get
        {
            Cleanup();
            return _sessions.Count;
        }
    }

    /// <summary>
    /// Xóa session cũ.
    /// </summary>
    private static void Cleanup()
    {
        var cutoff = DateTime.UtcNow - Threshold;
        foreach (var key in _sessions.Keys)
            if (_sessions.TryGetValue(key, out var t) && t < cutoff)
                _sessions.TryRemove(key, out _);
    }
}

/// <summary>
/// Controller để quản lý analytics.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    /// <summary>
    /// Service để quản lý analytics.
    /// </summary>
    private readonly IAnalyticsService _analyticsService;
    /// <summary>
    /// Service để quản lý POI.
    /// </summary>
    private readonly IPOIService _poiService;
    /// <summary>
    /// Logger để quản lý analytics.
    /// </summary>
    private readonly ILogger<AnalyticsController> _logger;
    /// <summary>
    /// Queue để quản lý visit log.
    /// </summary>
    private readonly VisitLogQueue _visitQueue;

    public AnalyticsController(IAnalyticsService analyticsService, IPOIService poiService,
        ILogger<AnalyticsController> logger, VisitLogQueue visitQueue)
    {
        _analyticsService = analyticsService;
        _poiService = poiService;
        _logger = logger;
        _visitQueue = visitQueue;
    }

    [HttpPost("devices/profile")]
    [AllowAnonymous]
    public async Task<IActionResult> UpsertDeviceProfile([FromBody] DeviceProfileUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.DeviceId) || req.DeviceId.Length > 128)
            return BadRequest(new { Error = "Invalid DeviceId" });

        await _analyticsService.UpsertDeviceProfileAsync(req.DeviceId, req.Profile, req.Cores, req.RamMb);
        return Ok(new { ok = true, profile = req.Profile.ToString() });
    }

    [HttpPost("heartbeat")]
    [AllowAnonymous]

    /// <summary>
    /// Gửi heartbeat đến server để giữ session active.
    /// </summary>
    public IActionResult Heartbeat([FromBody] HeartbeatRequest request)
    {
        var userId = User.Identity?.IsAuthenticated == true
            ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            : request.UserId;

        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new { Error = "UserId required" });

        HeartbeatTracker.Touch(userId);
        return Ok();
    }
    /// <summary>
    /// Lấy số lượng session active.
    /// </summary>

    [HttpGet("online-now")]
    [Authorize(Roles = "Admin")]
    
    public IActionResult GetOnlineNow() =>
        Ok(new { OnlineNow = HeartbeatTracker.Count});

    /// <summary>
    /// Ghi lại lượt ghé thăm.
    /// </summary>
    [HttpPost("visit")]
    [AllowAnonymous]
    public IActionResult LogVisit([FromBody] VisitLogRequest request)
    {
        // Log visit request
        // Always prefer JWT claims when authenticated (prevents client spoofing UserId=0).
        string? userId = null;
        if (User.Identity?.IsAuthenticated == true)
            userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            userId = request.UserId;

        //

        // Log visit request
        _logger.LogInformation(
            "[LogVisit] Received: POId={POId}, UserId={UserId}, VisitType={VisitType} | JWT={JwtAuth}",
            request.POId, userId, request.VisitType, User.Identity?.IsAuthenticated);
        // Log visit request
        if (request.POId <= 0)
        {
            _logger.LogWarning("[LogVisit] REJECTED: POId={POId} invalid", request.POId);
            return BadRequest(new { Error = "Invalid POId" });
        }

        // Enqueue visit log item vào queue
        /// <summary>
        _visitQueue.Enqueue(new VisitLogItem(request.POId, userId, request.Latitude, request.Longitude, request.VisitType));
        _logger.LogInformation("[LogVisit] Enqueued: POId={POId}, UserId={UserId}", request.POId, userId);
        // Trả về status 202 Accepted
        return Accepted(new { Message = "Visit queued" });
    }

    /// <summary>Tổng lượt ghé thăm và phân loại — tính từ VisitLogs (không dùng bảng Analytics).</summary>
    [HttpGet("summary")]
    [Authorize(Roles = "Admin")]
    
    public async Task<IActionResult> GetVisitSummary([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        // Lấy tổng lượt ghé thăm và phân loại
        var (total, geofence, mapClick, qrCode) = await _analyticsService.GetVisitSummaryAsync(startDate, endDate);
        return Ok(new { TotalVisits = total, Geofence = geofence, MapClick = mapClick, QRCode = qrCode });
    }

    [HttpGet("top-pois")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetTopPOIs([FromQuery] int count = 10, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var topPOIs = await _analyticsService.GetTopPOIsAsync(count, startDate, endDate);
        return Ok(topPOIs);
    }

    [HttpGet("poi/{poiId}/statistics")]
    
    /// <summary>
    /// Lấy thống kê lượt ghé thăm của một POI.
    /// </summary>
    public async Task<IActionResult> GetPOIStatistics(int poiId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        if (User.IsInRole("ShopOwner"))
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var poi = await _poiService.GetPOIByIdAsync(poiId);
            if (poi?.OwnerId != userId)
                return Forbid();
        }

        var stats = await _analyticsService.GetPOIStatisticsAsync(poiId, startDate, endDate);
        return Ok(stats);
    }

    [HttpGet("poi/{poiId}/logs")]
    public async Task<IActionResult> GetVisitLogs(int poiId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        if (User.IsInRole("ShopOwner"))
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var poi = await _poiService.GetPOIByIdAsync(poiId);
            if (poi?.OwnerId != userId)
                return Forbid();
        }

        var logs = await _analyticsService.GetVisitLogsAsync(poiId, startDate, endDate);
        return Ok(logs);
    }

    [HttpGet("devices")]
    [Authorize(Roles = "Admin")]

    /// <summary>
    /// Lấy danh sách thiết bị.
    /// </summary>
    public async Task<IActionResult> GetDevices([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var devices = await _analyticsService.GetDeviceStatsAsync(page, pageSize);
        return Ok(devices);
    }

    /// <summary>
    /// Lấy thống kê thiết bị.
    /// </summary>
    [HttpGet("devices/summary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDevicesSummary()
    {
        var summary = await _analyticsService.GetDeviceSummaryAsync();
        return Ok(summary);
    }

    /// <summary>
    /// Lấy danh sách điểm Geofence.
    /// </summary>
    [HttpGet("heatmap")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetHeatmap(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var points = await _analyticsService.GetHeatmapDataAsync(startDate, endDate);
        return Ok(points);
    }
    /// <summary>
    /// Lấy chi tiết thiết bị.
    /// </summary>

    [HttpGet("devices/{deviceId}/details")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDeviceDetail(string deviceId)
    {
        var detail = await _analyticsService.GetDeviceDetailAsync(deviceId);
        if (detail == null) return NotFound(new { Message = "Không tìm thấy thiết bị" });
        return Ok(detail);
    }
}

public class VisitLogRequest
{
    [JsonPropertyName("poiId")]
    public int POId { get; set; }
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }
    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }
    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }
    [JsonPropertyName("visitType")]
    public VisitType VisitType { get; set; }
}

public class HeartbeatRequest
{
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }
}

public class DeviceProfileUpsertRequest
{
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;
    [JsonPropertyName("profile")]
    public MobileDeviceProfile Profile { get; set; }
    [JsonPropertyName("cores")]
    public int? Cores { get; set; }
    [JsonPropertyName("ramMb")]
    public long? RamMb { get; set; }
}
