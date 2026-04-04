using HeriStepAI.Mobile.Helpers;
using HeriStepAI.Mobile.Services;
using HeriStepAI.Mobile.ViewModels;
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

            // Gate on subscription: show payment page if not active
            var subscription = serviceProvider.GetRequiredService<ISubscriptionService>();
            if (subscription.IsActive)
            {
                MainPage = serviceProvider.GetRequiredService<AppShell>();
            }
            else
            {
                var vm = serviceProvider.GetRequiredService<SubscriptionViewModel>();
                MainPage = new SubscriptionPage(vm);
            }

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
