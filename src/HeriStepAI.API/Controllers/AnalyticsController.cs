using System.Text.Json.Serialization;
using HeriStepAI.API.Models;
using HeriStepAI.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HeriStepAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IPOIService _poiService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IAnalyticsService analyticsService, IPOIService poiService,
        ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _poiService = poiService;
        _logger = logger;
    }

    [HttpPost("visit")]
    [AllowAnonymous]
    public IActionResult LogVisit([FromBody] VisitLogRequest request)
    {
        // Always prefer JWT claims when authenticated (prevents client spoofing UserId=0).
        string? userId = null;
        if (User.Identity?.IsAuthenticated == true)
            userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            userId = request.UserId;

        _logger.LogInformation(
            "[LogVisit] Received: POId={POId}, UserId={UserId}, VisitType={VisitType} | JWT={JwtAuth}",
            request.POId, userId, request.VisitType, User.Identity?.IsAuthenticated);

        if (request.POId <= 0)
        {
            _logger.LogWarning("[LogVisit] REJECTED: POId={POId} invalid", request.POId);
            return BadRequest(new { Error = "Invalid POId" });
        }

        // Fire-and-forget: respond immediately so mobile never times out.
        // DB write happens in background; use a scoped service factory to avoid DbContext threading issues.
        var poiId = request.POId;
        var lat = request.Latitude;
        var lon = request.Longitude;
        var visitType = request.VisitType;
        var services = HttpContext.RequestServices;
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = services.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
                await svc.LogVisitAsync(poiId, userId, lat, lon, visitType);
                _logger.LogInformation("[LogVisit] DB write OK: POId={POId}, UserId={UserId}", poiId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LogVisit] DB write FAILED: POId={POId}, UserId={UserId}", poiId, userId);
            }
        });

        return Accepted(new { Message = "Visit queued" });
    }

    /// <summary>Tổng lượt ghé thăm và phân loại — tính từ VisitLogs (không dùng bảng Analytics).</summary>
    [HttpGet("summary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetVisitSummary([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
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
    public async Task<IActionResult> GetDevices([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var devices = await _analyticsService.GetDeviceStatsAsync(page, pageSize);
        return Ok(devices);
    }

    [HttpGet("devices/summary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDevicesSummary()
    {
        var summary = await _analyticsService.GetDeviceSummaryAsync();
        return Ok(summary);
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
