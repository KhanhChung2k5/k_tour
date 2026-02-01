using System.Text.Json;

namespace HeriStepAI.API.Services;

/// <summary>
/// Reverse geocoding qua Nominatim (OpenStreetMap) - miễn phí.
/// </summary>
public class GeocodingService : IGeocodingService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GeocodingService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string?> GetAddressFromCoordinatesAsync(double latitude, double longitude)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", "HeriStepAI/1.0 (heristepai@example.com)");
            var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude:F6}&lon={longitude:F6}&zoom=18&addressdetails=1";
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return root.TryGetProperty("display_name", out var displayName)
                ? displayName.GetString()
                : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Geocoding error: {ex.Message}");
            return null;
        }
    }
}
