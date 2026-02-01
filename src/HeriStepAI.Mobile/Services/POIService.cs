using HeriStepAI.Mobile.Models;
using SQLite;

namespace HeriStepAI.Mobile.Services;

public class POIService : IPOIService
{
    private readonly IApiService _apiService;
    private SQLiteAsyncConnection? _db;
    private readonly Task _initTask;

    public POIService(IApiService apiService)
    {
        _apiService = apiService;
        _initTask = InitializeDatabaseAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "pois.db");
            _db = new SQLiteAsyncConnection(databasePath);
            await _db.CreateTableAsync<POI>();
            await _db.CreateTableAsync<POIContent>();

            // Migration: thêm cột mới nếu chưa có (SQLite không có IF NOT EXISTS cho cột)
            try
            {
                await _db.ExecuteAsync("ALTER TABLE POI ADD COLUMN Rating REAL");
            }
            catch { /* column may exist */ }
            try
            {
                await _db.ExecuteAsync("ALTER TABLE POI ADD COLUMN ReviewCount INTEGER DEFAULT 0");
            }
            catch { }
            try
            {
                await _db.ExecuteAsync("ALTER TABLE POI ADD COLUMN Category INTEGER DEFAULT 0");
            }
            catch { }
            try
            {
                await _db.ExecuteAsync("ALTER TABLE POI ADD COLUMN TourId INTEGER");
            }
            catch { }
            try
            {
                await _db.ExecuteAsync("ALTER TABLE POI ADD COLUMN EstimatedMinutes INTEGER DEFAULT 30");
            }
            catch { }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
        }
    }

    private async Task EnsureDbReadyAsync()
    {
        await _initTask;
    }

    public async Task<List<POI>> GetAllPOIsAsync()
    {
        await EnsureDbReadyAsync();
        if (_db == null) return new List<POI>();

        try
        {
            var pois = await _db.Table<POI>().ToListAsync();
            foreach (var poi in pois)
            {
                poi.Contents = await _db.Table<POIContent>()
                    .Where(c => c.POId == poi.Id)
                    .ToListAsync();
            }
            return pois;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetAllPOIsAsync error: {ex.Message}");
            return new List<POI>();
        }
    }

    public async Task<POI?> GetPOIByIdAsync(int id)
    {
        await EnsureDbReadyAsync();
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
        await EnsureDbReadyAsync();
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
