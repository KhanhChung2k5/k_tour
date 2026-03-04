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
            LogToDebug("App: InitializeComponent START");
            var _t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            InitializeComponent();
            var _ms = (System.Diagnostics.Stopwatch.GetTimestamp() - _t0) * 1000 / System.Diagnostics.Stopwatch.Frequency;
            LogToDebug($"App: InitializeComponent DONE in {_ms}ms");

            // Initialize responsive helper for adaptive layouts
            ResponsiveHelper.Initialize();
            LogToDebug($"App: ResponsiveHelper initialized");

            // Show an instant amber splash — zero cost, no XAML to parse.
            // InitializeAsync will exclusively decide what page to show next.
            // Do NOT eagerly create LoginPage here — LoginPage.InitializeComponent()
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
                // No session hint → first launch or explicit logout.
                // InitializeAsync will show LoginPage after TryRestoreSessionAsync completes (~1-2s).
                // Never eagerly create LoginPage here — InitializeComponent() blocks main thread 100+ seconds.
                LogToDebug("App: First launch / no session — waiting for InitializeAsync to show LoginPage.");
                _ = Task.Run(() => InitializeAsync(authService, serviceProvider, fromLoggedInHint: false));
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

            // Show AppShell ONLY when: (1) user was previously logged in AND (2) session restored OK.
            // All other cases → LoginPage. Never skip Auth when has_session is absent.
            if (fromLoggedInHint && isLoggedIn)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MainPage = serviceProvider.GetRequiredService<AppShell>();
                    LogToDebug("App: AppShell shown (session restored).");
                });
            }
            else
            {
                // Covers: expired session (hint=true, login=false), first launch, explicit logout.
                if (fromLoggedInHint) Preferences.Default.Remove("has_session");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (MainPage is ContentPage)
                    {
                        LogToDebug("App: Creating LoginPage on main thread...");
                        var t0 = System.Diagnostics.Stopwatch.GetTimestamp();
                        MainPage = serviceProvider.GetRequiredService<LoginPage>();
                        var ms = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000 / System.Diagnostics.Stopwatch.Frequency;
                        LogToDebug($"App: LoginPage shown in {ms}ms (fromHint={fromLoggedInHint}, loggedIn={isLoggedIn}).");
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
