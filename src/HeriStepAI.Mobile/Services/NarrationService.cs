using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

/// <summary>
/// Narration Engine - Yêu cầu: quản lý hàng đợi, không phát trùng, tự dừng khi có POI mới.
/// </summary>
public class NarrationService : INarrationService
{
    private bool _isPlaying = false;
    private readonly Queue<POI> _narrationQueue = new();
    private POI? _currentPOI = null;
    private readonly Dictionary<int, DateTime> _lastPlayedAt = new(); // Per-POI cooldown
    private readonly TimeSpan _poiCooldown = TimeSpan.FromMinutes(5);
    private CancellationTokenSource? _playCts;

    public bool IsPlaying => _isPlaying;

    public event EventHandler? NarrationCompleted;

    public async Task PlayNarrationAsync(POI poi, string language, bool forcePlay = false)
    {
        // Chống spam (geofence): không phát lại POI đã phát trong X phút. Bỏ qua khi user click thủ công (forcePlay).
        if (!forcePlay && _lastPlayedAt.TryGetValue(poi.Id, out var lastTime) && DateTime.UtcNow - lastTime < _poiCooldown)
            return;

        // Tự dừng khi có POI mới - dừng phát hiện tại, xóa hàng đợi cũ
        if (_isPlaying || _narrationQueue.Count > 0)
        {
            _playCts?.Cancel();
            _narrationQueue.Clear();
        }

        _currentPOI = poi;
        _narrationQueue.Enqueue(poi);

        if (!_isPlaying)
        {
            _playCts = new CancellationTokenSource();
            await ProcessQueueAsync(language, _playCts.Token);
        }
    }

    private async Task ProcessQueueAsync(string language, CancellationToken ct = default)
    {
        if (_narrationQueue.Count == 0)
        {
            _isPlaying = false;
            return;
        }

        _isPlaying = true;
        var poi = _narrationQueue.Dequeue();

        var content = poi.Contents?.FirstOrDefault(c => c.Language == language) 
                   ?? poi.Contents?.FirstOrDefault(c => c.Language == "vi")
                   ?? poi.Contents?.FirstOrDefault();

        string? textToSpeak = null;
        string? audioUrl = null;
        var contentType = ContentType.TTS;

        if (content != null)
        {
            contentType = content.ContentType;
            textToSpeak = content.TextContent;
            audioUrl = content.AudioUrl;
        }

        // Fallback: dùng Description nếu không có POIContent
        if (string.IsNullOrEmpty(textToSpeak) && !string.IsNullOrEmpty(poi.Description))
            textToSpeak = poi.Description;

        if (content == null && string.IsNullOrEmpty(textToSpeak))
        {
            _isPlaying = false;
            await ProcessQueueAsync(language, ct);
            return;
        }

        try
        {
            if (contentType == ContentType.AudioFile && !string.IsNullOrEmpty(audioUrl))
            {
                await PlayAudioFileAsync(audioUrl, ct);
            }
            else if (!string.IsNullOrEmpty(textToSpeak))
            {
                await SpeakTextAsync(textToSpeak, language, ct);
            }
            // Ghi log đã phát để tránh lặp (yêu cầu Step 5)
            _lastPlayedAt[poi.Id] = DateTime.UtcNow;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error playing narration: {ex.Message}");
        }
        finally
        {
            _isPlaying = false;
            NarrationCompleted?.Invoke(this, EventArgs.Empty);
            if (!ct.IsCancellationRequested)
                await ProcessQueueAsync(language, ct);
        }
    }

    private async Task SpeakTextAsync(string text, string language, CancellationToken ct = default)
    {
        try
        {
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            var viLocale = locales.FirstOrDefault(l => l.Language.StartsWith("vi", StringComparison.OrdinalIgnoreCase));
            if (language == "vi" && viLocale != null)
            {
                await TextToSpeech.Default.SpeakAsync(text, new Microsoft.Maui.Media.SpeechOptions { Locale = viLocale });
            }
            else
            {
                await TextToSpeech.Default.SpeakAsync(text);
            }
        }
        catch
        {
            await TextToSpeech.Default.SpeakAsync(text);
        }
    }

    private async Task PlayAudioFileAsync(string audioUrl, CancellationToken ct = default)
    {
        // TODO: MediaManager / plugin để phát file audio. Hiện dùng TTS nếu có TextContent.
        await Task.Delay(100, ct);
        System.Diagnostics.Debug.WriteLine($"Audio playback: {audioUrl}");
    }

    public void StopNarration()
    {
        _playCts?.Cancel();
        _narrationQueue.Clear();
        _isPlaying = false;
        _currentPOI = null;
    }
}
