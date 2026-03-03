using HeriStepAI.Mobile.Helpers;
using HeriStepAI.Mobile.Services;
using HeriStepAI.Mobile.Views;

namespace HeriStepAI.Mobile;

public partial class App : Application
{
    private const string LogTag = "HeriStepAI";

    public static IServiceProvider? Services { get; private set; }

    public App(IServiceProvider serviceProvider, IAuthService authService)
    {
        try
        {
            Services = serviceProvider;
            LogToDebug("App: InitializeComponent...");
            InitializeComponent();

            // Initialize responsive helper for adaptive layouts
            ResponsiveHelper.Initialize();
            LogToDebug($"App: ResponsiveHelper initialized");

            // Pre-warm both auth pages on UI thread so navigation is instant
            var loginPage = serviceProvider.GetRequiredService<LoginPage>();
            _ = serviceProvider.GetRequiredService<RegisterPage>(); // warm up XAML now
            MainPage = loginPage;

            // Run all startup async work on a background thread (never block UI thread)
            _ = Task.Run(() => InitializeAsync(authService, serviceProvider));
        }
        catch (Exception ex)
        {
            var msg = $"App constructor error: {ex}\n{ex.StackTrace}";
            LogToDebug(msg);
            throw;
        }
    }

    private async Task InitializeAsync(IAuthService authService, IServiceProvider serviceProvider)
    {
        try
        {
            // Restore saved session
            var isLoggedIn = await authService.TryRestoreSessionAsync();
            LogToDebug($"App: Session restored = {isLoggedIn}");

            if (isLoggedIn)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MainPage = serviceProvider.GetRequiredService<AppShell>();
                });
            }

            // Trigger initial POI sync in background regardless of login state
            var poiService = serviceProvider.GetService<IPOIService>();
            if (poiService != null)
            {
                await poiService.SyncPOIsFromServerAsync();
                LogToDebug("App: Initial POI sync completed");
            }
        }
        catch (Exception ex)
        {
            LogToDebug($"App: InitializeAsync failed: {ex.Message}");
        }
    }

    private static void LogToDebug(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[{LogTag}] {message}");
    }
}
