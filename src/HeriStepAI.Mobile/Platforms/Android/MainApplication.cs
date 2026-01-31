using Android.App;
using Android.Util;
using Android.Runtime;

namespace HeriStepAI.Mobile;

[Application]
public class MainApplication : MauiApplication
{
    private const string LogTag = "HeriStepAI";

    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    public override void OnCreate()
    {
        base.OnCreate();
        SetupCrashLogging();
    }

    private void SetupCrashLogging()
    {
        // Log ra logcat (xem bằng: adb logcat -s HeriStepAI)
        Log.Info(LogTag, "=== HeriStepAI App Started - Crash logging enabled ===");

        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var ex = (Exception)e.ExceptionObject;
            var msg = $"UnhandledException: {ex}\n{ex.StackTrace}";
            Log.Error(LogTag, msg);
            System.Diagnostics.Debug.WriteLine($"[CRASH] {msg}");
        };

        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            var msg = $"UnobservedTaskException: {e.Exception}";
            Log.Error(LogTag, msg);
            System.Diagnostics.Debug.WriteLine($"[CRASH] {msg}");
        };

        AndroidEnvironment.UnhandledExceptionRaiser += (sender, e) =>
        {
            var ex = e.Exception;
            var msg = $"AndroidException: {ex}\n{ex.StackTrace}";
            Log.Error(LogTag, msg);
            System.Diagnostics.Debug.WriteLine($"[CRASH] {msg}");
        };
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}