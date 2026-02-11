using HeriStepAI.Mobile.Models;
using Newtonsoft.Json;
using System.Text;

namespace HeriStepAI.Mobile.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private static readonly string _baseUrl = 
#if DEBUG
        "http://10.0.2.2:5000/api/"; // Emulator local API
#else
        "https://heristep.onrender.com/api/"; // Production
#endif

    public ApiService()
    {
        try
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl)
            };
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing ApiService: {ex.Message}");
            _httpClient = new HttpClient();
        }
    }

    public async Task<List<POI>?> GetAllPOIsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("poi");
            AppLog.Info($"ApiService GET poi -> {response.StatusCode}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var pois = JsonConvert.DeserializeObject<List<POI>>(json);
                AppLog.Info($"ApiService Deserialized {pois?.Count ?? 0} POIs");
                return pois;
            }
            var errBody = await response.Content.ReadAsStringAsync();
            AppLog.Error($"ApiService Error response: {errBody}");
        }
        catch (Exception ex)
        {
            AppLog.Error($"ApiService Error: {ex.Message}");
        }
        return null;
    }

    public async Task LogVisitAsync(int poiId, double? latitude, double? longitude, VisitType visitType)
    {
        try
        {
            var visitLog = new
            {
                POId = poiId,
                UserId = (string?)null,
                Latitude = latitude,
                Longitude = longitude,
                VisitType = (int)visitType
            };

            var json = JsonConvert.SerializeObject(visitLog);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("analytics/visit", content);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error logging visit: {ex.Message}");
        }
    }
}
