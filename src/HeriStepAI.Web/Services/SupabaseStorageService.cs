using System.Net.Http.Headers;

namespace HeriStepAI.Web.Services;

public interface ISupabaseStorageService
{
    Task<string?> UploadImageAsync(Stream fileStream, string fileName, string contentType);
    Task<bool> DeleteImageAsync(string filePath);
}

public class SupabaseStorageService : ISupabaseStorageService
{
    private readonly HttpClient _httpClient;
    private readonly string? _supabaseUrl;
    private readonly string? _serviceKey;
    private readonly string _bucket;
    private readonly bool _isConfigured;

    public SupabaseStorageService(IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL")
            ?? configuration["Supabase:Url"];
        _serviceKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY")
            ?? configuration["Supabase:ServiceRoleKey"];
        _bucket = Environment.GetEnvironmentVariable("SUPABASE_BUCKET")
            ?? configuration["Supabase:Bucket"]
            ?? "c-media";

        _isConfigured = !string.IsNullOrEmpty(_supabaseUrl) && !string.IsNullOrEmpty(_serviceKey);

        if (_isConfigured && _serviceKey != null)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _serviceKey);
            _httpClient.DefaultRequestHeaders.Add("apikey", _serviceKey);
        }
        else
        {
            Console.WriteLine("[SupabaseStorage] WARNING: Supabase Storage not configured. Image upload will be disabled.");
        }
    }

    public async Task<string?> UploadImageAsync(Stream fileStream, string fileName, string contentType)
    {
        if (!_isConfigured || string.IsNullOrEmpty(_supabaseUrl))
        {
            Console.WriteLine("[SupabaseStorage] Upload skipped: Supabase not configured");
            return null; // Return null instead of throwing - image upload is optional
        }

        try
        {
            // Generate unique file path: poi-images/{timestamp}_{filename}
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var safeName = $"poi-images/{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}{ext}";

            var url = $"{_supabaseUrl}/storage/v1/object/{_bucket}/{safeName}";

            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                // Return public URL
                var publicUrl = $"{_supabaseUrl}/storage/v1/object/public/{_bucket}/{safeName}";
                Console.WriteLine($"[SupabaseStorage] Uploaded: {publicUrl}");
                return publicUrl;
            }

            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[SupabaseStorage] Upload failed ({response.StatusCode}): {error}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SupabaseStorage] Error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteImageAsync(string filePath)
    {
        if (!_isConfigured || string.IsNullOrEmpty(_supabaseUrl))
        {
            Console.WriteLine("[SupabaseStorage] Delete skipped: Supabase not configured");
            return false; // Return false instead of throwing - deletion is optional
        }

        try
        {
            // Extract path from full URL
            var prefix = $"{_supabaseUrl}/storage/v1/object/public/{_bucket}/";
            if (filePath.StartsWith(prefix))
            {
                filePath = filePath[prefix.Length..];
            }

            var url = $"{_supabaseUrl}/storage/v1/object/{_bucket}/{filePath}";
            var response = await _httpClient.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SupabaseStorage] Delete error: {ex.Message}");
            return false;
        }
    }
}
