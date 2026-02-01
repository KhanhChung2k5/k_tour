using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

public interface INarrationService
{
    Task PlayNarrationAsync(POI poi, string language, bool forcePlay = false);
    void StopNarration();
    bool IsPlaying { get; }
    event EventHandler? NarrationCompleted;
}
