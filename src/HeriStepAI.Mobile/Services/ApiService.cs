using HeriStepAI.Mobile.Models;
using Newtonsoft.Json;
using System.Text;

namespace HeriStepAI.Mobile.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "http://10.0.2.2:5000/api/"; // Update with your API URL

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
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<POI>>(json);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching POIs: {ex.Message}");
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
