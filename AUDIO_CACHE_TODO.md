# 🔊 Audio Caching for Offline Narration - TODO

## Vấn đề hiện tại

Hiện tại app đã có offline mode cho POI data (SQLite), nhưng **audio narration files chưa được cache locally**.

Có 2 loại narration content:
1. **TTS (Text-to-Speech)**: ✅ Hoạt động offline (vì chỉ cần text)
2. **Audio Files (AudioUrl)**: ❌ Cần internet để stream/download

## Mục tiêu

Khi user lần đầu mở app (có internet):
- Download tất cả audio files từ POIContent.AudioUrl
- Lưu vào local storage
- Lần sau không có mạng vẫn phát được

## Implementation Plan

### 1. Tạo AudioCacheService

**Location**: `Services/AudioCacheService.cs`

```csharp
public interface IAudioCacheService
{
    /// <summary>
    /// Download và cache audio file từ URL
    /// </summary>
    Task<string?> DownloadAndCacheAsync(string audioUrl, int poiId, string language);

    /// <summary>
    /// Get local file path nếu đã cached
    /// </summary>
    string? GetCachedFilePath(string audioUrl);

    /// <summary>
    /// Check if audio file đã cached
    /// </summary>
    bool IsCached(string audioUrl);

    /// <summary>
    /// Clear cache (optional)
    /// </summary>
    Task ClearCacheAsync();
}

public class AudioCacheService : IAudioCacheService
{
    private readonly HttpClient _httpClient;
    private string CacheDirectory => Path.Combine(FileSystem.AppDataDirectory, "audio_cache");

    public AudioCacheService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("API");
        EnsureCacheDirectoryExists();
    }

    private void EnsureCacheDirectoryExists()
    {
        if (!Directory.Exists(CacheDirectory))
            Directory.CreateDirectory(CacheDirectory);
    }

    public async Task<string?> DownloadAndCacheAsync(string audioUrl, int poiId, string language)
    {
        try
        {
            // Generate unique filename: poi_123_vi.mp3
            var extension = Path.GetExtension(audioUrl) ?? ".mp3";
            var filename = $"poi_{poiId}_{language}{extension}";
            var localPath = Path.Combine(CacheDirectory, filename);

            // Skip if already cached
            if (File.Exists(localPath))
                return localPath;

            // Download
            var audioBytes = await _httpClient.GetByteArrayAsync(audioUrl);
            await File.WriteAllBytesAsync(localPath, audioBytes);

            AppLog.Info($"Cached audio: {filename} ({audioBytes.Length} bytes)");
            return localPath;
        }
        catch (Exception ex)
        {
            AppLog.Error($"Failed to cache audio {audioUrl}: {ex.Message}");
            return null;
        }
    }

    public string? GetCachedFilePath(string audioUrl)
    {
        // Extract filename from URL or use hash
        var filename = GetFilenameFromUrl(audioUrl);
        var localPath = Path.Combine(CacheDirectory, filename);
        return File.Exists(localPath) ? localPath : null;
    }

    public bool IsCached(string audioUrl)
    {
        return GetCachedFilePath(audioUrl) != null;
    }

    public Task ClearCacheAsync()
    {
        if (Directory.Exists(CacheDirectory))
            Directory.Delete(CacheDirectory, recursive: true);
        EnsureCacheDirectoryExists();
        return Task.CompletedTask;
    }

    private string GetFilenameFromUrl(string url)
    {
        // Simple: use URL hash as filename
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(url));
        return Convert.ToBase64String(hash).Replace("/", "_").Replace("+", "-") + ".mp3";
    }
}
```

### 2. Update POIService để cache audio khi sync

```csharp
public async Task SyncPOIsFromServerAsync()
{
    await EnsureDbReadyAsync();
    try
    {
        var serverPOIs = await _apiService.GetAllPOIsAsync();
        if (_db == null || serverPOIs == null || serverPOIs.Count == 0)
            return;

        await _db.DeleteAllAsync<POIContent>();
        await _db.DeleteAllAsync<POI>();

        foreach (var poi in serverPOIs)
        {
            poi.Contents ??= new List<POIContent>();
            await _db.InsertAsync(poi);

            foreach (var content in poi.Contents)
            {
                await _db.InsertAsync(content);

                // ✅ NEW: Cache audio file if ContentType is AudioFile
                if (content.ContentType == ContentType.AudioFile && !string.IsNullOrEmpty(content.AudioUrl))
                {
                    _ = Task.Run(async () =>
                    {
                        var cachedPath = await _audioCacheService.DownloadAndCacheAsync(
                            content.AudioUrl,
                            poi.Id,
                            content.Language);

                        if (cachedPath != null)
                        {
                            // Update local path in database (optional)
                            content.LocalAudioPath = cachedPath;
                            await _db.UpdateAsync(content);
                        }
                    });
                }
            }
        }

        AppLog.Info($"POIService Synced {serverPOIs.Count} POIs");
    }
    catch (Exception ex)
    {
        AppLog.Error($"POIService Error: {ex.Message}");
    }
}
```

### 3. Update POIContent model

```csharp
public class POIContent
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int POId { get; set; }
    public string Language { get; set; } = "vi";
    public string? TextContent { get; set; }
    public string? AudioUrl { get; set; }

    // ✅ NEW: Local cached audio path
    public string? LocalAudioPath { get; set; }

    public ContentType ContentType { get; set; } = ContentType.TTS;
}
```

### 4. Update NarrationService để sử dụng cached audio

```csharp
private async Task PlaySingleAsync(POI poi, string language, CancellationToken ct)
{
    var content = poi.Contents?.FirstOrDefault(c => c.Language == language)
               ?? poi.Contents?.FirstOrDefault(c => c.Language == "vi")
               ?? poi.Contents?.FirstOrDefault();

    string? textToSpeak = content?.TextContent;
    string? audioUrl = content?.AudioUrl;
    string? localAudioPath = content?.LocalAudioPath; // ✅ NEW
    var contentType = content?.ContentType ?? ContentType.TTS;

    // Fallback: dùng Description nếu không có POIContent
    if (string.IsNullOrEmpty(textToSpeak) && !string.IsNullOrEmpty(poi.Description))
        textToSpeak = poi.Description;

    if (string.IsNullOrEmpty(textToSpeak) && string.IsNullOrEmpty(audioUrl))
        return;

    if (contentType == ContentType.AudioFile)
    {
        // ✅ Try local cached file first
        if (!string.IsNullOrEmpty(localAudioPath) && File.Exists(localAudioPath))
        {
            await PlayAudioFileAsync(localAudioPath, ct);
        }
        // Fallback: stream from URL if cached file not available
        else if (!string.IsNullOrEmpty(audioUrl))
        {
            await PlayAudioFileAsync(audioUrl, ct);
        }
        // Fallback: TTS if no audio available
        else if (!string.IsNullOrEmpty(textToSpeak))
        {
            await SpeakTextAsync(textToSpeak, language, ct);
        }
    }
    else if (!string.IsNullOrEmpty(textToSpeak))
    {
        await SpeakTextAsync(textToSpeak, language, ct);
    }
}
```

### 5. Implement audio playback (MediaManager)

Hiện tại `PlayAudioFileAsync` chỉ là placeholder. Cần implement thật:

**Option 1**: Sử dụng Plugin.Maui.Audio

```bash
dotnet add package Plugin.Maui.Audio
```

```csharp
using Plugin.Maui.Audio;

private async Task PlayAudioFileAsync(string audioPath, CancellationToken ct)
{
    try
    {
        var audioManager = AudioManager.Current;
        IAudioPlayer? player = null;

        if (audioPath.StartsWith("http"))
        {
            // Stream from URL
            player = audioManager.CreatePlayer(await GetStreamFromUrl(audioPath));
        }
        else
        {
            // Play from local file
            using var stream = File.OpenRead(audioPath);
            player = audioManager.CreatePlayer(stream);
        }

        player.Play();

        // Wait for playback to complete or cancellation
        while (player.IsPlaying && !ct.IsCancellationRequested)
        {
            await Task.Delay(100, ct);
        }

        player.Stop();
        player.Dispose();
    }
    catch (Exception ex)
    {
        AppLog.Error($"Audio playback error: {ex.Message}");
    }
}

private async Task<Stream> GetStreamFromUrl(string url)
{
    var response = await _httpClient.GetAsync(url);
    return await response.Content.ReadAsStreamAsync();
}
```

**Option 2**: Sử dụng platform-specific MediaPlayer

```csharp
#if ANDROID
using Android.Media;
#endif

private async Task PlayAudioFileAsync(string audioPath, CancellationToken ct)
{
#if ANDROID
    var player = new MediaPlayer();
    try
    {
        if (audioPath.StartsWith("http"))
            player.SetDataSource(audioPath);
        else
            player.SetDataSource(audioPath);

        player.Prepare();
        player.Start();

        while (player.IsPlaying && !ct.IsCancellationRequested)
        {
            await Task.Delay(100, ct);
        }

        player.Stop();
    }
    finally
    {
        player.Release();
    }
#endif
}
```

### 6. Register service in MauiProgram.cs

```csharp
// Services
builder.Services.AddSingleton<IAudioCacheService, AudioCacheService>();
```

### 7. Update App.xaml.cs to cache audio on startup

```csharp
// Trigger initial POI sync from server to SQLite for offline mode
Task.Run(async () =>
{
    try
    {
        var poiService = serviceProvider.GetService<IPOIService>();
        if (poiService != null)
        {
            await poiService.SyncPOIsFromServerAsync();
            LogToDebug("App: Initial POI sync completed (includes audio caching)");
        }
    }
    catch (Exception ex)
    {
        LogToDebug($"App: Initial POI sync failed: {ex.Message}");
    }
});
```

## Testing Plan

### Test 1: First Launch (with internet)
1. Cài APK mới trên máy có internet
2. Mở app lần đầu
3. Wait for sync to complete
4. Check logs: "Cached audio: poi_123_vi.mp3 (xxxxx bytes)"
5. Check file system: `FileSystem.AppDataDirectory/audio_cache/` should have .mp3 files

### Test 2: Offline Playback
1. Sau khi sync xong, tắt WiFi/4G
2. Navigate to MapPage
3. Trigger POI geofence hoặc manual play narration
4. Verify audio plays from local cache
5. Check logs: should NOT see network requests

### Test 3: Fallback to TTS
1. Tắt internet
2. Play POI không có cached audio
3. Should fallback to TTS (text-to-speech)

## File Size Considerations

- Average audio file: ~500KB - 2MB per POI per language
- 50 POIs × 2 languages × 1MB = ~100MB storage
- Need to inform user or implement selective download
- Consider compression (MP3 vs AAC vs Opus)

## Optional Improvements

- [ ] Show download progress UI
- [ ] Selective download: only download for selected tour
- [ ] Compression: convert to lower bitrate
- [ ] Cache expiration: auto-delete old files
- [ ] Background download with WorkManager
- [ ] Retry failed downloads

---

**Status**: 📝 TODO - Chưa implement
**Priority**: Medium (TTS hiện tại đã work offline)
**Estimated effort**: 4-6 hours
