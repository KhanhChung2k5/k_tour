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
    private readonly string _supabaseUrl;
    private readonly string _bucket;

    public SupabaseStorageService(IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL")
            ?? configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("SUPABASE_URL not configured");
        var serviceKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY")
            ?? configuration["Supabase:ServiceRoleKey"]
            ?? throw new InvalidOperationException("SUPABASE_SERVICE_ROLE_KEY not configured");
        _bucket = Environment.GetEnvironmentVariable("SUPABASE_BUCKET")
            ?? configuration["Supabase:Bucket"]
            ?? "c-media";

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", serviceKey);
        _httpClient.DefaultRequestHeaders.Add("apikey", serviceKey);
    }

    public async Task<string?> UploadImageAsync(Stream fileStream, string fileName, string contentType)
    {
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
