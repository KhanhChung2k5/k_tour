namespace HeriStepAI.Mobile.Services;

/// <summary>
/// Cross-platform manager for starting/stopping location foreground service
/// </summary>
public static class LocationForegroundServiceManager
{
    private static bool _isRunning = false;

    /// <summary>
    /// Start foreground service to keep location tracking active in background
    /// </summary>
    public static void Start()
    {
        if (_isRunning)
            return;

#if ANDROID
        try
        {
            var intent = new Android.Content.Intent(
                Android.App.Application.Context,
                typeof(Platforms.Android.Services.LocationForegroundService));

            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                Android.App.Application.Context.StartForegroundService(intent);
            }
            else
            {
                Android.App.Application.Context.StartService(intent);
            }

            _isRunning = true;
            System.Diagnostics.Debug.WriteLine("LocationForegroundService started");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting foreground service: {ex.Message}");
        }
#endif
    }

    /// <summary>
    /// Stop foreground service
    /// </summary>
    public static void Stop()
    {
        if (!_isRunning)
            return;

#if ANDROID
        try
        {
            var intent = new Android.Content.Intent(
                Android.App.Application.Context,
                typeof(Platforms.Android.Services.LocationForegroundService));

            Android.App.Application.Context.StopService(intent);
            _isRunning = false;
            System.Diagnostics.Debug.WriteLine("LocationForegroundService stopped");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping foreground service: {ex.Message}");
        }
#endif
    }

    public static bool IsRunning => _isRunning;
}
