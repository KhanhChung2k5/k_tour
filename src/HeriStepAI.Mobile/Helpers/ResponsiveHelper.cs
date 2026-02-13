namespace HeriStepAI.Mobile.Helpers;

/// <summary>
/// Helper class for responsive design - calculates adaptive sizes based on screen dimensions
/// </summary>
public static class ResponsiveHelper
{
    private static double? _screenWidth;
    private static double? _screenHeight;
    private static double _scaleFactor = 1.0;

    /// <summary>
    /// Initialize responsive helper with current screen dimensions
    /// Call this once during app startup
    /// </summary>
    public static void Initialize()
    {
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        _screenWidth = displayInfo.Width / displayInfo.Density;
        _screenHeight = displayInfo.Height / displayInfo.Density;

        // Calculate scale factor based on standard phone width (360dp)
        const double baseWidth = 360.0;
        _scaleFactor = (_screenWidth ?? baseWidth) / baseWidth;

        // Clamp scale factor to reasonable range
        _scaleFactor = Math.Max(0.8, Math.Min(_scaleFactor, 1.3));

        System.Diagnostics.Debug.WriteLine($"ResponsiveHelper: Screen {_screenWidth}x{_screenHeight}, Scale={_scaleFactor:F2}");
    }

    /// <summary>
    /// Get scaled font size
    /// </summary>
    public static double FontSize(double baseSize) => baseSize * _scaleFactor;

    /// <summary>
    /// Get scaled spacing/padding
    /// </summary>
    public static double Spacing(double baseSpacing) => baseSpacing * _scaleFactor;

    /// <summary>
    /// Get scaled width
    /// </summary>
    public static double Width(double baseWidth) => baseWidth * _scaleFactor;

    /// <summary>
    /// Get scaled height
    /// </summary>
    public static double Height(double baseHeight) => baseHeight * _scaleFactor;

    /// <summary>
    /// Get safe area top padding for status bar/notch
    /// </summary>
    public static Thickness SafeAreaPadding()
    {
#if ANDROID
        // Android status bar height is typically 24dp, but varies by device
        var statusBarHeight = GetAndroidStatusBarHeight();
        return new Thickness(16, statusBarHeight + 10, 16, 10);
#else
        return new Thickness(16, 44, 16, 10); // iOS safe area
#endif
    }

    /// <summary>
    /// Get Android status bar height in dp
    /// </summary>
    private static double GetAndroidStatusBarHeight()
    {
#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var resourceId = context.Resources?.GetIdentifier("status_bar_height", "dimen", "android") ?? 0;
            if (resourceId > 0 && context.Resources != null)
            {
                var heightPx = context.Resources.GetDimensionPixelSize(resourceId);
                var density = context.Resources.DisplayMetrics?.Density ?? 1.0f;
                return heightPx / density;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting status bar height: {ex.Message}");
        }
#endif
        return 24.0; // Fallback
    }

    /// <summary>
    /// Get adaptive padding for page header with status bar
    /// </summary>
    public static Thickness HeaderPadding()
    {
        var topPadding = GetAndroidStatusBarHeight() + 10;
        return new Thickness(Spacing(14), topPadding, Spacing(14), Spacing(10));
    }

    /// <summary>
    /// Get adaptive padding for regular content
    /// </summary>
    public static Thickness ContentPadding()
    {
        return new Thickness(Spacing(14), Spacing(12), Spacing(14), Spacing(12));
    }

    /// <summary>
    /// Check if device is a tablet (screen width > 600dp)
    /// </summary>
    public static bool IsTablet()
    {
        return (_screenWidth ?? 360) > 600;
    }

    /// <summary>
    /// Check if device is a small phone (screen width < 360dp)
    /// </summary>
    public static bool IsSmallPhone()
    {
        return (_screenWidth ?? 360) < 360;
    }
}
