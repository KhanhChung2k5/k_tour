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

            // Show an instant amber splash — zero cost, no XAML to parse.
            // InitializeAsync will exclusively decide what page to show next.
            // Do NOT eagerly create AuthPage here — AuthPage.InitializeComponent()
            // is very heavy and would block the main thread causing ANR.
            MainPage = new ContentPage
            {
                BackgroundColor = Color.FromArgb("#E8943A")
            };

            LogToDebug($"App: Starting InitializeAsync...");
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
            // Restore saved session (reads from SecureStorage — async, ~1s)
            var isLoggedIn = await authService.TryRestoreSessionAsync();
            LogToDebug($"App: Session restored = {isLoggedIn}");

            if (isLoggedIn)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Yield(); // tránh ANR: cho main thread vẽ trước
                    MainPage = serviceProvider.GetRequiredService<AppShell>();
                });
            }
            else
            {
                // Session invalid/expired — clear the hint flag
                Preferences.Default.Remove("has_session");

                // If still on splash (fast-path was used but session actually expired),
                // show AuthPage now
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (MainPage is ContentPage)
                    {
                        await Task.Yield();
                        if (MainPage is ContentPage)
                        {
                            var authPage = serviceProvider.GetRequiredService<AuthPage>();
                            MainPage = authPage;
                            LogToDebug("App: AuthPage shown (session expired)");
                        }
                    }
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
