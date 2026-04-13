using HeriStepAI.Mobile.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace HeriStepAI.Mobile.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;

    private static string GetBaseUrl()
    {
#if DEBUG
        // Local API: Android emulator dùng 10.0.2.2, iOS simulator / máy thật trong mạng LAN dùng IP máy PC
        if (DeviceInfo.Platform == DevicePlatform.Android)
            return "http://10.0.2.2:5000/api/";
        if (DeviceInfo.Platform == DevicePlatform.iOS)
            return "http://127.0.0.1:5000/api/"; // iOS simulator; máy thật: đổi thành IP PC (vd 192.168.1.x:5000)
        return "http://localhost:5000/api/";
#else
        return "https://heristep.onrender.com/api/";
#endif
    }

    public ApiService(IAuthService authService)
    {
        _authService = authService;
        var baseUrl = GetBaseUrl();
        try
        {
            _httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate })
            {
                BaseAddress = new Uri(baseUrl)
            };
            _httpClient.Timeout = TimeSpan.FromSeconds(45);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "HeriStepAI-Mobile/1.0");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing ApiService: {ex.Message}");
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(45) };
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
            AppLog.Info($"LogVisit: POI={poiId}, UserId={userId ?? "null"}, HasToken={hasToken}, URL={_httpClient.BaseAddress}analytics/visit");

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

            var response = await _httpClient.PostAsync("analytics/visit", content);
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

    public async Task<bool> ReportSubscriptionPaymentAsync(SubscriptionPaymentReport report)
    {
        var previousAuth = _httpClient.DefaultRequestHeaders.Authorization;
        try
        {
            // Endpoint AllowAnonymous — không gửi Bearer (tránh 401 nếu token hết hạn)
            _httpClient.DefaultRequestHeaders.Authorization = null;

            var payload = new
            {
                report.DeviceKey,
                report.TransferRef,
                report.PlanCode,
                report.PlanLabel,
                report.AmountVnd,
                report.SubscriptionExpiresAtUtc,
                report.Platform
            };
            var json = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("subscription-payments/report", content);
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                AppLog.Info($"ReportSubscriptionPayment OK: {body}");
                return true;
            }

            AppLog.Error($"ReportSubscriptionPayment {response.StatusCode}: {body}");
            return false;
        }
        catch (Exception ex)
        {
            AppLog.Error($"ReportSubscriptionPayment error: {ex.Message}");
            return false;
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = previousAuth;
        }
    }

    public async Task<SubscriptionEntitlementDto?> GetSubscriptionEntitlementAsync(string deviceKey)
    {
        var previousAuth = _httpClient.DefaultRequestHeaders.Authorization;
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            var url = $"subscription-payments/entitlement?deviceKey={Uri.EscapeDataString(deviceKey)}";
            var response = await _httpClient.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                AppLog.Error($"GetSubscriptionEntitlement {response.StatusCode}: {body}");
                return null;
            }

            return JsonConvert.DeserializeObject<SubscriptionEntitlementDto>(body, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }
        catch (Exception ex)
        {
            AppLog.Error($"GetSubscriptionEntitlement error: {ex.Message}");
            return null;
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = previousAuth;
        }
    }
}
