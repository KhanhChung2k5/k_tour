using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

/// <summary>
/// Narration Engine - Hàng đợi tuần tự: khi 2 POI gần nhau, phát lần lượt thay vì hủy.
/// forcePlay (user click) = hủy hiện tại, phát ngay. Auto (geofence) = thêm vào hàng đợi.
/// </summary>
public class NarrationService : INarrationService
{
    private volatile bool _isProcessing;
    private readonly List<POI> _queue = new();
    private readonly object _queueLock = new();
    private POI? _currentPOI;
    private readonly Dictionary<int, DateTime> _lastPlayedAt = new();
    private readonly TimeSpan _poiCooldown = TimeSpan.FromMinutes(5);
    private CancellationTokenSource? _cts;
    private string _language = "vi";
    private readonly IVoicePreferenceService _voicePreference;

    public NarrationService(IVoicePreferenceService voicePreference)
    {
        _voicePreference = voicePreference;
    }

    public bool IsPlaying => _isProcessing;

    public event EventHandler? NarrationCompleted;

    public async Task PlayNarrationAsync(POI poi, string language, bool forcePlay = false)
    {
        _language = language;

        if (forcePlay)
        {
            // User click: hủy tất cả, phát ngay POI này
            _cts?.Cancel();
            lock (_queueLock) { _queue.Clear(); }
            await Task.Delay(200); // Cho loop cũ kết thúc
            lock (_queueLock) { _queue.Add(poi); }
            _isProcessing = false;
            EnsureProcessing();
            return;
        }

        // Auto-trigger (geofence): kiểm tra cooldown
        if (_lastPlayedAt.TryGetValue(poi.Id, out var last) && DateTime.UtcNow - last < _poiCooldown)
        {
            AppLog.Info($"⏭️ Narration cooldown skip: {poi.Name}");
            // Vẫn fire completed để simulator biết advance
            NarrationCompleted?.Invoke(this, EventArgs.Empty);
            return;
        }

        // Không trùng: bỏ qua nếu đang phát hoặc đã có trong hàng đợi
        lock (_queueLock)
        {
            if (_currentPOI?.Id == poi.Id) return;
            if (_queue.Any(p => p.Id == poi.Id)) return;
            _queue.Add(poi);
            AppLog.Info($"📋 Queued narration: {poi.Name} (queue={_queue.Count})");
        }

        EnsureProcessing();
    }

    private void EnsureProcessing()
    {
        if (_isProcessing) return;

        lock (_queueLock)
        {
            if (_isProcessing || _queue.Count == 0) return;
            _isProcessing = true;
        }

        _cts = new CancellationTokenSource();
        _ = ProcessLoopAsync(_cts.Token);
    }

    private async Task ProcessLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                POI? poi;
                lock (_queueLock)
                {
                    if (_queue.Count == 0) break;
                    poi = _queue[0];
                    _queue.RemoveAt(0);
                }

                _currentPOI = poi;

                try
                {
                    await PlaySingleAsync(poi, _language, ct);
                    _lastPlayedAt[poi.Id] = DateTime.UtcNow;
                    AppLog.Info($"✅ Narration done: {poi.Name}");
                    NarrationCompleted?.Invoke(this, EventArgs.Empty);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    AppLog.Error($"Narration error for {poi.Name}: {ex.Message}");
                    // Vẫn fire completed để không block simulator
                    NarrationCompleted?.Invoke(this, EventArgs.Empty);
                }
                finally
                {
                    _currentPOI = null;
                }
            }
        }
        catch (OperationCanceledException)
        {
            AppLog.Info("Narration processing cancelled");
        }
        finally
        {
            _isProcessing = false;
            _currentPOI = null;

            // Re-check: items có thể đã được thêm trong lúc shutdown
            bool hasMore;
            lock (_queueLock) { hasMore = _queue.Count > 0; }
            if (hasMore && !ct.IsCancellationRequested)
                EnsureProcessing();
        }
    }

    private async Task PlaySingleAsync(POI poi, string language, CancellationToken ct)
    {
        var content = poi.Contents?.FirstOrDefault(c => c.Language == language)
                   ?? poi.Contents?.FirstOrDefault(c => c.Language == "vi")
                   ?? poi.Contents?.FirstOrDefault();

        string? textToSpeak = content?.TextContent;
        string? audioUrl = content?.AudioUrl;
        var contentType = content?.ContentType ?? ContentType.TTS;

        // Fallback: dùng Description nếu không có POIContent
        if (string.IsNullOrEmpty(textToSpeak) && !string.IsNullOrEmpty(poi.Description))
            textToSpeak = poi.Description;

        if (string.IsNullOrEmpty(textToSpeak) && string.IsNullOrEmpty(audioUrl))
            return; // Không có gì để phát

        if (contentType == ContentType.AudioFile && !string.IsNullOrEmpty(audioUrl))
            await PlayAudioFileAsync(audioUrl, ct);
        else if (!string.IsNullOrEmpty(textToSpeak))
            await SpeakTextAsync(textToSpeak, language, ct);
    }

    /// <summary>
    /// Map language code to TTS locale prefix.
    /// "zh" maps to "zh" (Android supports zh-CN, zh-TW etc.)
    /// </summary>
    private static string GetTtsLanguagePrefix(string language) => language switch
    {
        "ko" => "ko",
        "zh" => "zh",
        "ja" => "ja",
        "th" => "th",
        "fr" => "fr",
        "en" => "en",
        _ => "vi",
    };

    private async Task SpeakTextAsync(string text, string language, CancellationToken ct)
    {
        try
        {
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            var langPrefix = GetTtsLanguagePrefix(language);

            var langLocales = locales.Where(l =>
                l.Language.StartsWith(langPrefix, StringComparison.OrdinalIgnoreCase)).ToList();

            Locale? locale = null;

            if (_voicePreference.VoiceGender == VoiceGender.Male)
            {
                locale = langLocales.FirstOrDefault(l =>
                    l.Name.Contains("Male", StringComparison.OrdinalIgnoreCase) ||
                    l.Name.Contains("Nam", StringComparison.OrdinalIgnoreCase) ||
                    l.Id.Contains("male", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                locale = langLocales.FirstOrDefault(l =>
                    l.Name.Contains("Female", StringComparison.OrdinalIgnoreCase) ||
                    l.Name.Contains("Nữ", StringComparison.OrdinalIgnoreCase) ||
                    l.Id.Contains("female", StringComparison.OrdinalIgnoreCase));
            }

            locale ??= langLocales.FirstOrDefault();

            if (locale != null)
            {
                AppLog.Info($"TTS voice: {locale.Name} ({locale.Language})");
                await TextToSpeech.Default.SpeakAsync(text,
                    new Microsoft.Maui.Media.SpeechOptions
                    {
                        Locale = locale,
                        Pitch = 1.0f,
                        Volume = 1.0f
                    }, ct);
            }
            else
            {
                await TextToSpeech.Default.SpeakAsync(text, cancelToken: ct);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TTS error: {ex.Message}");
            try { await TextToSpeech.Default.SpeakAsync(text, cancelToken: ct); }
            catch { }
        }
    }

    private async Task PlayAudioFileAsync(string audioUrl, CancellationToken ct)
    {
        // TODO: MediaManager / plugin để phát file audio
        await Task.Delay(100, ct);
        System.Diagnostics.Debug.WriteLine($"Audio playback: {audioUrl}");
    }

    public void StopNarration()
    {
        _cts?.Cancel();
        lock (_queueLock) { _queue.Clear(); }
        _isProcessing = false;
        _currentPOI = null;
    }
}
