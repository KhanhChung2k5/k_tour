using HeriStepAI.API.Data;
using HeriStepAI.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HeriStepAI.API.Services;

public class POIService : IPOIService
{
    private readonly ApplicationDbContext _context;

    public POIService(ApplicationDbContext context)
    {
        _context = context;
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
        poi.CreatedAt = DateTime.UtcNow;
        poi.UpdatedAt = DateTime.UtcNow;
        _context.POIs.Add(poi);
        await _context.SaveChangesAsync();
        return poi;
    }

    public async Task<POI?> UpdatePOIAsync(int id, POI poi)
    {
        var existing = await _context.POIs.FindAsync(id);
        if (existing == null) return null;

        existing.Name = poi.Name;
        existing.Description = poi.Description;
        existing.Latitude = poi.Latitude;
        existing.Longitude = poi.Longitude;
        existing.Radius = poi.Radius;
        existing.Priority = poi.Priority;
        existing.ImageUrl = poi.ImageUrl;
        existing.MapLink = poi.MapLink;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
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
