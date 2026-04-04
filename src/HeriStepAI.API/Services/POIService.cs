using HeriStepAI.API.Data;
using HeriStepAI.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HeriStepAI.API.Services;

public class POIService : IPOIService
{
    private readonly ApplicationDbContext _context;
    private readonly IGeocodingService _geocodingService;

    public POIService(ApplicationDbContext context, IGeocodingService geocodingService)
    {
        _context = context;
        _geocodingService = geocodingService;
    }

    public async Task<List<POI>> GetAllPOIsAsync()
    {
        return await _context.POIs
            .Include(p => p.Contents)
            .Where(p => p.IsActive)
            .ToListAsync();
    }

    public async Task<POI?> GetPOIByIdAsync(int id)
    {
        return await _context.POIs
            .Include(p => p.Contents)
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
    }

    public async Task<POI> CreatePOIAsync(POI poi)
    {
        Console.WriteLine($"[POIService] CreatePOIAsync called for: {poi.Name}");
        Console.WriteLine($"[POIService] Contents count before save: {poi.Contents?.Count ?? 0}");

        poi.CreatedAt = DateTime.UtcNow;
        poi.UpdatedAt = DateTime.UtcNow;

        if (string.IsNullOrEmpty(poi.Address))
            poi.Address = await _geocodingService.GetAddressFromCoordinatesAsync(poi.Latitude, poi.Longitude);

        // Set CreatedAt for Contents
        if (poi.Contents != null)
        {
            foreach (var content in poi.Contents)
            {
                content.CreatedAt = DateTime.UtcNow;
                Console.WriteLine($"[POIService] Adding content - Lang: {content.Language}, Length: {content.TextContent?.Length ?? 0}");
            }
        }

        _context.POIs.Add(poi);
        await _context.SaveChangesAsync();

        Console.WriteLine($"[POIService] POI created with ID: {poi.Id}, Contents saved: {poi.Contents?.Count ?? 0}");
        return poi;
    }

    public async Task<POI?> UpdatePOIAsync(int id, POI poi)
    {
        Console.WriteLine($"[POIService] UpdatePOIAsync called for ID: {id}");
        Console.WriteLine($"[POIService] Incoming ImageUrl: {poi.ImageUrl ?? "NULL"}");

        var existing = await _context.POIs
            .Include(p => p.Contents)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (existing == null)
        {
            Console.WriteLine($"[POIService] POI ID {id} not found");
            return null;
        }

        // Update basic fields
        existing.Name = poi.Name;
        existing.Description = poi.Description;
        existing.Latitude = poi.Latitude;
        existing.Longitude = poi.Longitude;
        existing.Radius = poi.Radius;
        existing.Priority = poi.Priority;
        existing.ImageUrl = poi.ImageUrl;
        existing.MapLink = poi.MapLink;
        existing.Category = poi.Category;
        existing.FoodType = poi.FoodType;
        existing.PriceMin = poi.PriceMin;
        existing.PriceMax = poi.PriceMax;
        existing.Rating = poi.Rating;
        existing.ReviewCount = poi.ReviewCount;
        existing.EstimatedMinutes = poi.EstimatedMinutes;
        existing.TourId = poi.TourId;

        if (string.IsNullOrEmpty(poi.Address))
            existing.Address = await _geocodingService.GetAddressFromCoordinatesAsync(poi.Latitude, poi.Longitude);
        else
            existing.Address = poi.Address;

        Console.WriteLine($"[POIService] Setting ImageUrl: {existing.ImageUrl} -> {poi.ImageUrl}");
        existing.UpdatedAt = DateTime.UtcNow;

        // Update Contents - remove old and add new
        Console.WriteLine($"[POIService] Removing {existing.Contents?.Count ?? 0} old contents");
        if (existing.Contents != null && existing.Contents.Any())
        {
            _context.POIContents.RemoveRange(existing.Contents);
        }

        Console.WriteLine($"[POIService] Adding {poi.Contents?.Count ?? 0} new contents");
        if (poi.Contents != null && poi.Contents.Any())
        {
            existing.Contents = new List<POIContent>();
            foreach (var content in poi.Contents)
            {
                content.POId = existing.Id;
                content.CreatedAt = DateTime.UtcNow;
                existing.Contents.Add(content);
                Console.WriteLine($"[POIService] Adding content - Lang: {content.Language}, Length: {content.TextContent?.Length ?? 0}");
            }
        }

        await _context.SaveChangesAsync();
        Console.WriteLine($"[POIService] POI updated successfully");

        return existing;
    }

    public async Task<bool> DeletePOIAsync(int id)
    {
        var poi = await _context.POIs.FindAsync(id);
        if (poi == null) return false;

        poi.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<POI>> GetPOIsByOwnerAsync(int ownerId)
    {
        return await _context.POIs
            .Include(p => p.Contents)
            .Where(p => p.OwnerId == ownerId && p.IsActive)
            .ToListAsync();
    }

    public async Task<POIContent?> GetContentAsync(int poiId, string language)
    {
        return await _context.POIContents
            .FirstOrDefaultAsync(c => c.POId == poiId && c.Language == language);
    }
}
