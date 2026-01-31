using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

public interface IPOIService
{
    Task<List<POI>> GetAllPOIsAsync();
    Task<POI?> GetPOIByIdAsync(int id);
    Task SyncPOIsFromServerAsync();
}
