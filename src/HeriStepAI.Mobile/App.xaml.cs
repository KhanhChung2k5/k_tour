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

            // has_session = 1 chỉ khi user đã đăng nhập thành công lần trước.
            // Không có → luôn hiển thị Đăng ký/Đăng nhập, không tin TryRestoreSession để tránh bỏ qua Auth.
            var hasSessionHint = Preferences.Default.Get("has_session", "");
            LogToDebug($"App: has_session = '{hasSessionHint}'");

            if (!string.IsNullOrEmpty(hasSessionHint))
            {
                LogToDebug("App: Fast path — restore session, then show AppShell or Auth.");
                _ = Task.Run(() => InitializeAsync(authService, serviceProvider, fromLoggedInHint: true));
            }
            else
            {
                LogToDebug("App: First launch / no session — show AuthPage, never skip to AppShell from restore.");
                _ = Task.Run(() => InitializeAsync(authService, serviceProvider, fromLoggedInHint: false));
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(350);
                    if (MainPage is ContentPage)
                    {
                        await Task.Yield();
                        if (MainPage is ContentPage)
                        {
                            MainPage = serviceProvider.GetRequiredService<AuthPage>();
                            LogToDebug("App: AuthPage shown (first launch).");
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

    private async Task InitializeAsync(IAuthService authService, IServiceProvider serviceProvider, bool fromLoggedInHint)
    {
        try
        {
            var isLoggedIn = await authService.TryRestoreSessionAsync();
            LogToDebug($"App: Session restored = {isLoggedIn}, fromLoggedInHint = {fromLoggedInHint}");

            // Chỉ chuyển sang AppShell khi (1) có hint đã đăng nhập VÀ (2) restore session thành công.
            // Khi không có has_session (first launch) thì không bao giờ skip Auth dù TryRestoreSession = true.
            if (fromLoggedInHint && isLoggedIn)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MainPage = serviceProvider.GetRequiredService<AppShell>();
                    LogToDebug("App: AppShell shown (session restored).");
                });
            }
            else if (fromLoggedInHint && !isLoggedIn)
            {
                Preferences.Default.Remove("has_session");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (MainPage is ContentPage)
                    {
                        MainPage = serviceProvider.GetRequiredService<AuthPage>();
                        LogToDebug("App: AuthPage shown (session expired).");
                    }
                });
            }

            // POI sync chạy nền, không chờ — tránh trì hoãn hiển thị Đăng nhập (API có thể timeout 2–3 phút)
            var poiService = serviceProvider.GetService<IPOIService>();
            if (poiService != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await poiService.SyncPOIsFromServerAsync();
                        LogToDebug("App: Initial POI sync completed");
                    }
                    catch (Exception ex)
                    {
                        LogToDebug($"App: Initial POI sync failed: {ex.Message}");
                    }
                });
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
        AppLog.Info(message);
    }
}
