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

    public AnalyticsController(IAnalyticsService analyticsService, IPOIService poiService)
    {
        _analyticsService = analyticsService;
        _poiService = poiService;
    }

    [HttpPost("visit")]
    [AllowAnonymous]
    public async Task<IActionResult> LogVisit([FromBody] VisitLogRequest request)
    {
        await _analyticsService.LogVisitAsync(
            request.POId,
            request.UserId,
            request.Latitude,
            request.Longitude,
            request.VisitType
        );
        return Ok(new { Message = "Visit logged" });
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
        // Check if user has permission
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
        // Check if user has permission
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
}

public class VisitLogRequest
{
    public int POId { get; set; }
    public string? UserId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public VisitType VisitType { get; set; }
}
