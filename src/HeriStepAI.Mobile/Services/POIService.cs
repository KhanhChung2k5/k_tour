using HeriStepAI.Mobile.Models;
using SQLite;

namespace HeriStepAI.Mobile.Services;

public class POIService : IPOIService
{
    private readonly IApiService _apiService;
    private SQLiteAsyncConnection? _db;

    public POIService(IApiService apiService)
    {
        _apiService = apiService;
        InitializeDatabase();
    }

    private async void InitializeDatabase()
    {
        try
        {
            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "pois.db");
            _db = new SQLiteAsyncConnection(databasePath);
            await _db.CreateTableAsync<POI>();
            await _db.CreateTableAsync<POIContent>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
        }
    }

    public async Task<List<POI>> GetAllPOIsAsync()
    {
        if (_db == null) return new List<POI>();

        var pois = await _db.Table<POI>().ToListAsync();
        foreach (var poi in pois)
        {
            poi.Contents = await _db.Table<POIContent>()
                .Where(c => c.POId == poi.Id)
                .ToListAsync();
        }
        return pois;
    }

    public async Task<POI?> GetPOIByIdAsync(int id)
    {
        if (_db == null) return null;

        var poi = await _db.Table<POI>().FirstOrDefaultAsync(p => p.Id == id);
        if (poi != null)
        {
            poi.Contents = await _db.Table<POIContent>()
                .Where(c => c.POId == poi.Id)
                .ToListAsync();
        }
        return poi;
    }

    public async Task SyncPOIsFromServerAsync()
    {
        try
        {
            var serverPOIs = await _apiService.GetAllPOIsAsync();
            if (_db == null || serverPOIs == null) return;

            await _db.DeleteAllAsync<POIContent>();
            await _db.DeleteAllAsync<POI>();

            foreach (var poi in serverPOIs)
            {
                await _db.InsertAsync(poi);
                foreach (var content in poi.Contents)
                {
                    await _db.InsertAsync(content);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error syncing POIs: {ex.Message}");
        }
    }
}
