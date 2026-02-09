namespace HeriStepAI.Mobile.Services;

public enum VoiceGender
{
    Male,
    Female
}

public interface IVoicePreferenceService
{
    VoiceGender VoiceGender { get; set; }
    void SaveVoiceGender(VoiceGender gender);
    VoiceGender LoadVoiceGender();
}

public class VoicePreferenceService : IVoicePreferenceService
{
    private const string VoiceGenderKey = "voice_gender";

    public VoiceGender VoiceGender { get; set; } = VoiceGender.Female;

    public VoicePreferenceService()
    {
        VoiceGender = LoadVoiceGender();
    }

    public void SaveVoiceGender(VoiceGender gender)
    {
        VoiceGender = gender;
        Preferences.Set(VoiceGenderKey, (int)gender);
        AppLog.Info($"Voice gender saved: {gender}");
    }

    public VoiceGender LoadVoiceGender()
    {
        var saved = Preferences.Get(VoiceGenderKey, (int)VoiceGender.Female);
        return (VoiceGender)saved;
    }
}
