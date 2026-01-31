using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

public interface INarrationService
{
    Task PlayNarrationAsync(POI poi, string language);
    void StopNarration();
    bool IsPlaying { get; }
    event EventHandler? NarrationCompleted;
}
