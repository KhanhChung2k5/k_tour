using HeriStepAI.Mobile.Helpers;
using HeriStepAI.Mobile.Services;

namespace HeriStepAI.Mobile;

public partial class App : Application
{
    private const string LogTag = "HeriStepAI";

    public static IServiceProvider? Services { get; private set; }

    public App(IServiceProvider serviceProvider)
    {
        try
        {
            Services = serviceProvider;
            LogToDebug("App: InitializeComponent...");
            InitializeComponent();

            // Initialize responsive helper for adaptive layouts
            ResponsiveHelper.Initialize();
            LogToDebug($"App: ResponsiveHelper initialized");

            // Trigger initial POI sync from server to SQLite for offline mode
            Task.Run(async () =>
            {
                try
                {
                    var poiService = serviceProvider.GetService<IPOIService>();
                    if (poiService != null)
                    {
                        await poiService.SyncPOIsFromServerAsync();
                        LogToDebug("App: Initial POI sync completed");
                    }
                }
                catch (Exception ex)
                {
                    LogToDebug($"App: Initial POI sync failed: {ex.Message}");
                }
            });

            LogToDebug("App: Creating AppShell...");
            MainPage = serviceProvider.GetRequiredService<AppShell>();
            LogToDebug("App: Started successfully");
        }
        catch (Exception ex)
        {
            var msg = $"App constructor error: {ex}\n{ex.StackTrace}";
            LogToDebug(msg);
            throw;
        }
    }

    private static void LogToDebug(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[{LogTag}] {message}");
    }
}
