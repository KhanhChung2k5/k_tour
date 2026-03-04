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

            // Show an instant inline splash page — no XAML file to parse, nearly zero cost.
            MainPage = new ContentPage
            {
                BackgroundColor = Color.FromArgb("#E8943A")
            };

            // Quick sync check (Preferences = ~0ms) to decide startup path:
            // - has_session=1 → user was logged in last time → skip AuthPage, wait for AppShell
            // - has_session=0 → user not logged in → show AuthPage
            var hasSessionHint = Preferences.Default.Get("has_session", "");
            LogToDebug($"App: has_session hint = '{hasSessionHint}'");

            if (!string.IsNullOrEmpty(hasSessionHint))
            {
                // FAST PATH: logged-in user — InitializeAsync will switch to AppShell.
                // AuthPage is NEVER created → no 4-second XAML block → no ANR.
                _ = Task.Run(() => InitializeAsync(authService, serviceProvider));
            }
            else
            {
                // FIRST-TIME / LOGGED-OUT: show AuthPage after session check confirms no session.
                _ = Task.Run(() => InitializeAsync(authService, serviceProvider));
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(400); // give session check a head start
                    if (MainPage is ContentPage)
                    {
                        await Task.Yield(); // let splash paint to avoid ANR
                        if (MainPage is ContentPage)
                        {
                            var authPage = serviceProvider.GetRequiredService<AuthPage>();
                            MainPage = authPage;
                            LogToDebug("App: AuthPage shown");
                        }
                    }
                });
            }
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
