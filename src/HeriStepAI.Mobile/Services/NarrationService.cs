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

            // Chờ loop cũ thực sự kết thúc (tối đa 1s), không chờ cứng 200ms
            var deadline = DateTime.UtcNow.AddSeconds(1);
            while (_isProcessing && DateTime.UtcNow < deadline)
                await Task.Delay(50);

            // Reset trạng thái sau khi loop cũ đã dừng
            lock (_queueLock)
            {
                _queue.Clear(); // Clear lại phòng case queue bị thêm trong lúc chờ
                _queue.Add(poi);
            }
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
        AppLog.Info($"PlaySingleAsync: POI={poi.Name}, lang={language}, contents={poi.Contents?.Count ?? 0}, desc={poi.Description?.Length ?? 0} chars");

        var content = poi.Contents?.FirstOrDefault(c => c.Language == language)
                   ?? poi.Contents?.FirstOrDefault(c => c.Language == "vi")
                   ?? poi.Contents?.FirstOrDefault();

        string? textToSpeak = content?.TextContent;
        string? audioUrl = content?.AudioUrl;
        var contentType = content?.ContentType ?? ContentType.TTS;

        AppLog.Info($"PlaySingleAsync: content={content?.Language ?? "null"}, textLen={textToSpeak?.Length ?? 0}, contentType={contentType}");

        // Fallback: dùng Description nếu không có POIContent
        if (string.IsNullOrEmpty(textToSpeak) && !string.IsNullOrEmpty(poi.Description))
        {
            textToSpeak = poi.Description;
            AppLog.Info($"PlaySingleAsync: fallback to Description ({textToSpeak.Length} chars)");
        }

        if (string.IsNullOrEmpty(textToSpeak) && string.IsNullOrEmpty(audioUrl))
        {
            AppLog.Error($"PlaySingleAsync: no text or audio to play for {poi.Name}!");
            return;
        }

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
            AppLog.Info($"SpeakTextAsync: lang={language}, textLen={text.Length}, first50={text[..Math.Min(50, text.Length)]}");

            ct.ThrowIfCancellationRequested();

            var locales = await TextToSpeech.Default.GetLocalesAsync();
            var langPrefix = GetTtsLanguagePrefix(language);

            var langLocales = locales.Where(l =>
                l.Language.StartsWith(langPrefix, StringComparison.OrdinalIgnoreCase)).ToList();

            AppLog.Info($"TTS locales for '{langPrefix}': {langLocales.Count} found");

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

            ct.ThrowIfCancellationRequested();

            if (locale != null)
            {
                AppLog.Info($"TTS speaking with locale: {locale.Name} ({locale.Language})");
                var options = new Microsoft.Maui.Media.SpeechOptions
                {
                    Locale = locale,
                    Pitch = 1.0f,
                    Volume = 1.0f
                };
                // Gọi TTS trên main thread — một số thiết bị Android yêu cầu điều này
                await MainThread.InvokeOnMainThreadAsync(() =>
                    TextToSpeech.Default.SpeakAsync(text, options, ct));
            }
            else
            {
                AppLog.Info($"TTS no locale for '{langPrefix}', using system default");
                await MainThread.InvokeOnMainThreadAsync(() =>
                    TextToSpeech.Default.SpeakAsync(text, cancelToken: ct));
            }

            AppLog.Info($"TTS SpeakAsync completed for: {text[..Math.Min(30, text.Length)]}");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            AppLog.Error($"TTS error: {ex.GetType().Name}: {ex.Message}");
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    TextToSpeech.Default.SpeakAsync(text, cancelToken: ct));
            }
            catch (Exception ex2)
            {
                AppLog.Error($"TTS fallback also failed: {ex2.Message}");
            }
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
