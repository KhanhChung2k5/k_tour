using HeriStepAI.API.Models;
using HeriStepAI.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HeriStepAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class POIController : ControllerBase
{
    private readonly IPOIService _poiService;
    private readonly IGeocodingService _geocodingService;

    public POIController(IPOIService poiService, IGeocodingService geocodingService)
    {
        _poiService = poiService;
        _geocodingService = geocodingService;
    }

    [HttpGet("geocode")]
    public async Task<IActionResult> Geocode([FromQuery] double lat, [FromQuery] double lng)
    {
        var address = await _geocodingService.GetAddressFromCoordinatesAsync(lat, lng);
        return Ok(new { Latitude = lat, Longitude = lng, Address = address ?? "Không xác định" });
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPOIs()
    {
        try
        {
            var pois = await _poiService.GetAllPOIsAsync();
            return Ok(pois);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[POIController] GetAllPOIs error: {ex}");
            return StatusCode(500, new { error = ex.Message, detail = ex.ToString() });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPOI(int id)
    {
        var poi = await _poiService.GetPOIByIdAsync(id);
        if (poi == null) return NotFound();
        return Ok(poi);
    }

    [HttpGet("{id}/content/{language}")]
    public async Task<IActionResult> GetContent(int id, string language)
    {
        var content = await _poiService.GetContentAsync(id, language);
        if (content == null) return NotFound();
        return Ok(content);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ShopOwner")]
    public async Task<IActionResult> CreatePOI([FromBody] POI poi)
    {
        Console.WriteLine($"[API POIController] CreatePOI called");
        Console.WriteLine($"[API POIController] POI Name: {poi?.Name}");
        Console.WriteLine($"[API POIController] Contents count: {poi?.Contents?.Count ?? 0}");

        if (poi?.Contents != null)
        {
            foreach (var c in poi.Contents)
            {
                Console.WriteLine($"[API POIController] Content - Lang: {c.Language}, TextLength: {c.TextContent?.Length ?? 0}");
            }
        }

        if (User.IsInRole("ShopOwner"))
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            poi.OwnerId = userId;
        }

        var created = await _poiService.CreatePOIAsync(poi);
        Console.WriteLine($"[API POIController] POI created with ID: {created.Id}");
        return CreatedAtAction(nameof(GetPOI), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,ShopOwner")]
    public async Task<IActionResult> UpdatePOI(int id, [FromBody] POI poi)
    {
        Console.WriteLine($"[API POIController] UpdatePOI called for ID: {id}");
        Console.WriteLine($"[API POIController] POI Name: {poi?.Name}");
        Console.WriteLine($"[API POIController] Contents count: {poi?.Contents?.Count ?? 0}");

        if (poi?.Contents != null)
        {
            foreach (var c in poi.Contents)
            {
                Console.WriteLine($"[API POIController] Content - Lang: {c.Language}, TextLength: {c.TextContent?.Length ?? 0}");
            }
        }

        if (User.IsInRole("ShopOwner"))
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var existing = await _poiService.GetPOIByIdAsync(id);
            if (existing?.OwnerId != userId)
                return Forbid();
        }

        var updated = await _poiService.UpdatePOIAsync(id, poi);
        if (updated == null) return NotFound();

        Console.WriteLine($"[API POIController] POI updated successfully");
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,ShopOwner")]
    public async Task<IActionResult> DeletePOI(int id)
    {
        if (User.IsInRole("ShopOwner"))
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var existing = await _poiService.GetPOIByIdAsync(id);
            if (existing?.OwnerId != userId)
                return Forbid();
        }

        var deleted = await _poiService.DeletePOIAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpGet("my-pois")]
    [Authorize(Roles = "ShopOwner")]
    public async Task<IActionResult> GetMyPOIs()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var pois = await _poiService.GetPOIsByOwnerAsync(userId);
        return Ok(pois);
    }
}
