using HeriStepAI.API.Models;

namespace HeriStepAI.API.Services;

public interface IPOIService
{
    Task<List<POI>> GetAllPOIsAsync();
    Task<POI?> GetPOIByIdAsync(int id);
    Task<POI> CreatePOIAsync(POI poi);
    Task<POI?> UpdatePOIAsync(int id, POI poi);
    Task<bool> DeletePOIAsync(int id);
    Task<List<POI>> GetPOIsByOwnerAsync(int ownerId);
    Task<POIContent?> GetContentAsync(int poiId, string language);
}
