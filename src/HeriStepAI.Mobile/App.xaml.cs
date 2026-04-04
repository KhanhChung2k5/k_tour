using HeriStepAI.Mobile.Helpers;
using HeriStepAI.Mobile.Services;
using HeriStepAI.Mobile.Views;

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
            InitializeComponent();
            ResponsiveHelper.Initialize();

            // Go straight to AppShell — no login required
            MainPage = serviceProvider.GetRequiredService<AppShell>();

            // Sync POIs in background
            _ = Task.Run(async () =>
            {
                try
                {
                    var poiService = serviceProvider.GetService<IPOIService>();
                    if (poiService != null)
                        await poiService.SyncPOIsFromServerAsync();
                }
                catch (Exception ex)
                {
                    LogToDebug($"App: POI sync failed: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            LogToDebug($"App constructor error: {ex.Message}");
            throw;
        }
    }

    private static void LogToDebug(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[{LogTag}] {message}");
        AppLog.Info(message);
    }
}
