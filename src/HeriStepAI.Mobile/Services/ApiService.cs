using HeriStepAI.Mobile.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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
                if (_authService.GetToken() is { } token)
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
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
            var userId = _authService.CurrentUser?.Id.ToString();
            var hasToken = _authService.GetToken() is { };
            AppLog.Info($"LogVisit: POI={poiId}, UserId={userId ?? "null"}, HasToken={hasToken}");

            var visitLog = new
            {
                poiId = poiId,
                userId = userId,
                latitude = latitude,
                longitude = longitude,
                visitType = (int)visitType
            };

            var json = JsonConvert.SerializeObject(visitLog, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            AppLog.Info($"LogVisit JSON: {json}");
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            if (_authService.GetToken() is { } token)
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync("analytics/visit", content);
            }
            catch (Exception connEx) when (connEx is HttpRequestException or TaskCanceledException)
            {
                // Server cold start (Render free tier) — retry once after 5s
                AppLog.Info($"LogVisit retry after: {connEx.Message}");
                await Task.Delay(5000);
                var retryContent = new StringContent(json, Encoding.UTF8, "application/json");
                response = await _httpClient.PostAsync("analytics/visit", retryContent);
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                AppLog.Error($"LogVisit failed: {response.StatusCode} - {responseBody}");
            }
            else
            {
                AppLog.Info($"LogVisit success: {response.StatusCode} - {responseBody}");
            }
        }
        catch (Exception ex)
        {
            AppLog.Error($"LogVisit error: {ex.Message}");
        }
    }
}
