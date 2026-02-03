#if ANDROID
using Android.Util;
#endif

namespace HeriStepAI.Mobile.Services;

/// <summary>
/// Log ra logcat với tag HeriStepAI. Xem bằng: adb logcat -s HeriStepAI:*
/// </summary>
internal static class AppLog
{
    private const string Tag = "HeriStepAI";

    public static void Info(string message)
    {
#if ANDROID
        Log.Info(Tag, message);
#endif
        System.Diagnostics.Debug.WriteLine($"[{Tag}] {message}");
    }

    public static void Error(string message)
    {
#if ANDROID
        Log.Error(Tag, message);
#endif
        System.Diagnostics.Debug.WriteLine($"[{Tag}] ERROR: {message}");
    }
}
