using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

/// <summary>Dịch vụ narration</summary>
public interface INarrationService
{
    Task PlayNarrationAsync(POI poi, string language, bool forcePlay = false);
    /// <summary>Dừng narration</summary>
    void StopNarration();
    /// <summary>Trạng thái phát narration</summary>
    bool IsPlaying { get; }
    /// <summary>Event khi narration hoàn thành</summary>
    event EventHandler? NarrationCompleted;
}
