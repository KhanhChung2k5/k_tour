using HeriStepAI.Mobile.Models;
using System.Text;

namespace HeriStepAI.Mobile.Services;

public class NarrationService : INarrationService
{
    private bool _isPlaying = false;
    private readonly Queue<POI> _narrationQueue = new();
    private POI? _currentPOI = null;

    public bool IsPlaying => _isPlaying;

    public event EventHandler? NarrationCompleted;

    public async Task PlayNarrationAsync(POI poi, string language)
    {
        // Prevent duplicate playback
        if (_currentPOI?.Id == poi.Id && _isPlaying)
            return;

        _currentPOI = poi;
        _narrationQueue.Enqueue(poi);

        if (!_isPlaying)
        {
            await ProcessQueueAsync(language);
        }
    }

    private async Task ProcessQueueAsync(string language)
    {
        if (_narrationQueue.Count == 0)
        {
            _isPlaying = false;
            return;
        }

        _isPlaying = true;
        var poi = _narrationQueue.Dequeue();

        var content = poi.Contents.FirstOrDefault(c => c.Language == language) 
                   ?? poi.Contents.FirstOrDefault(c => c.Language == "vi")
                   ?? poi.Contents.FirstOrDefault();

        if (content == null)
        {
            _isPlaying = false;
            await ProcessQueueAsync(language);
            return;
        }

        try
        {
            if (content.ContentType == ContentType.AudioFile && !string.IsNullOrEmpty(content.AudioUrl))
            {
                // Play audio file
                await PlayAudioFileAsync(content.AudioUrl);
            }
            else if (!string.IsNullOrEmpty(content.TextContent))
            {
                // Use TTS
                await SpeakTextAsync(content.TextContent, language);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error playing narration: {ex.Message}");
        }
        finally
        {
            _isPlaying = false;
            NarrationCompleted?.Invoke(this, EventArgs.Empty);
            await ProcessQueueAsync(language);
        }
    }

    private async Task SpeakTextAsync(string text, string language)
    {
        // Use platform-specific TTS
        if (Microsoft.Maui.Devices.DeviceInfo.Platform == Microsoft.Maui.Devices.DevicePlatform.Android)
        {
            // Android TTS implementation
            await Task.Run(() =>
            {
                // This would use Android.TextToSpeech in a real implementation
                System.Diagnostics.Debug.WriteLine($"TTS: {text} (Language: {language})");
                Thread.Sleep(2000); // Simulate speech
            });
        }
        else if (Microsoft.Maui.Devices.DeviceInfo.Platform == Microsoft.Maui.Devices.DevicePlatform.iOS)
        {
            // iOS AVSpeechSynthesizer implementation
            await Task.Run(() =>
            {
                System.Diagnostics.Debug.WriteLine($"TTS: {text} (Language: {language})");
                Thread.Sleep(2000); // Simulate speech
            });
        }
    }

    private async Task PlayAudioFileAsync(string audioUrl)
    {
        // Download and play audio file
        await Task.Run(() =>
        {
            System.Diagnostics.Debug.WriteLine($"Playing audio: {audioUrl}");
            Thread.Sleep(3000); // Simulate audio playback
        });
    }

    public void StopNarration()
    {
        _narrationQueue.Clear();
        _isPlaying = false;
        _currentPOI = null;
    }
}
