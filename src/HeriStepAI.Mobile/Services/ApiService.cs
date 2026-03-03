using HeriStepAI.Mobile.Models;
using Newtonsoft.Json;
using System.Text;

namespace HeriStepAI.Mobile.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    private static readonly string _baseUrl =
#if DEBUG
        "http://10.0.2.2:5000/api/"; // Emulator local API
#else
        "https://heristep.onrender.com/api/"; // Production
#endif

    public ApiService(IAuthService authService)
    {
        _authService = authService;
        try
        {
            _httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate })
            {
                BaseAddress = new Uri(_baseUrl)
            };
            _httpClient.Timeout = TimeSpan.FromSeconds(45);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "HeriStepAI-Mobile/1.0");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing ApiService: {ex.Message}");
            _httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl), Timeout = TimeSpan.FromSeconds(45) };
        }
    }

    public async Task<List<POI>?> GetAllPOIsAsync()
    {
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                AppLog.Info($"ApiService GET poi (attempt {attempt}/{maxRetries})");
                var response = await _httpClient.GetAsync("poi");
                AppLog.Info($"ApiService GET poi -> {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var pois = JsonConvert.DeserializeObject<List<POI>>(json);
                    AppLog.Info($"ApiService Deserialized {pois?.Count ?? 0} POIs");
                    return pois ?? new List<POI>();
                }
                var errBody = await response.Content.ReadAsStringAsync();
                AppLog.Error($"ApiService Error {response.StatusCode}: {errBody}");
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                AppLog.Error($"ApiService Timeout (attempt {attempt}) - Render cold start?");
                if (attempt < maxRetries) await Task.Delay(2000 * attempt);
            }
            catch (Exception ex)
            {
                AppLog.Error($"ApiService Error (attempt {attempt}): {ex.Message}");
                if (attempt < maxRetries) await Task.Delay(2000 * attempt);
            }
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
                UserId = _authService.CurrentUser?.Id.ToString(),
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
