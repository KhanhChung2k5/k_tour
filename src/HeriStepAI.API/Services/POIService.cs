using HeriStepAI.API.Data;
using HeriStepAI.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HeriStepAI.API.Services;

public class POIService : IPOIService
{
    private readonly ApplicationDbContext _context;
    private readonly IGeocodingService _geocodingService;
    private readonly IPOIContentTranslationSyncService _translationSync;

    public POIService(ApplicationDbContext context, IGeocodingService geocodingService, IPOIContentTranslationSyncService translationSync)
    {
        _context = context;
        _geocodingService = geocodingService;
        _translationSync = translationSync;
    }

    public async Task<List<POI>> GetAllPOIsAsync()
    {
        return await _context.POIs
            .Include(p => p.Contents)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.Id)
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

        await _translationSync.SyncFromVietnameseAsync(poi.Id);

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

        // Compare old vs new Vietnamese content before touching Contents
        var oldViText = existing.Contents?
            .FirstOrDefault(c => c.Language == "vi")?.TextContent ?? "";
        var newViText = poi.Contents?
            .FirstOrDefault(c => c.Language == "vi")?.TextContent ?? "";
        var viContentChanged = !string.Equals(oldViText.Trim(), newViText.Trim(), StringComparison.Ordinal);

        Console.WriteLine($"[POIService] Vietnamese content changed: {viContentChanged}");

        if (viContentChanged)
        {
            // Remove ALL old contents so they get re-translated fresh
            Console.WriteLine($"[POIService] Removing {existing.Contents?.Count ?? 0} old contents for re-translation");
            if (existing.Contents != null && existing.Contents.Any())
                _context.POIContents.RemoveRange(existing.Contents);

            existing.Contents = new List<POIContent>();
            if (poi.Contents != null && poi.Contents.Any())
            {
                foreach (var content in poi.Contents)
                {
                    content.POId = existing.Id;
                    content.CreatedAt = DateTime.UtcNow;
                    existing.Contents.Add(content);
                    Console.WriteLine($"[POIService] Adding content - Lang: {content.Language}, Length: {content.TextContent?.Length ?? 0}");
                }
            }
        }
        else
        {
            // Only update the Vietnamese row in-place; keep all translated contents untouched
            Console.WriteLine($"[POIService] Vietnamese unchanged — keeping existing translations");
            var existingVi = existing.Contents?.FirstOrDefault(c => c.Language == "vi");
            if (existingVi != null && !string.IsNullOrWhiteSpace(newViText))
                existingVi.TextContent = newViText;
        }

        await _context.SaveChangesAsync();
        Console.WriteLine($"[POIService] POI updated successfully");

        if (viContentChanged)
            await _translationSync.SyncFromVietnameseAsync(existing.Id);

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
            .OrderByDescending(p => p.Priority)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<POIContent?> GetContentAsync(int poiId, string language)
    {
        return await _context.POIContents
            .FirstOrDefaultAsync(c => c.POId == poiId && c.Language == language);
    }
}
