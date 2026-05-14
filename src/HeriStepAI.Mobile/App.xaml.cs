using HeriStepAI.Mobile.Helpers;
using HeriStepAI.Mobile.Services;
using HeriStepAI.Mobile.ViewModels;
using HeriStepAI.Mobile.Views;

namespace HeriStepAI.Mobile;

public partial class App : Application
{
    private const string LogTag = "HeriStepAI";
    private readonly HeartbeatService _heartbeat;

    public static IServiceProvider? Services { get; private set; }

    public App(IServiceProvider serviceProvider)
    {
        try
        {
            Services = serviceProvider;
            InitializeComponent();
            ResponsiveHelper.Initialize();

            _heartbeat = serviceProvider.GetRequiredService<HeartbeatService>();

            // Gate on subscription: show payment page if not active
            var subscription = serviceProvider.GetRequiredService<ISubscriptionService>();
            if (subscription.IsActive)
            {
                MainPage = serviceProvider.GetRequiredService<AppShell>();
                // Không gọi Start() ở đây: Application.Current/Dispatcher có thể chưa sẵn sàng → OnStart
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

            // Gửi năng lực thiết bị lên server (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    var apiService = serviceProvider.GetService<IApiService>();
                    if (apiService != null)
                        await apiService.PushDeviceProfileAsync();
                }
                catch (Exception ex)
                {
                    LogToDebug($"App: PushDeviceProfile failed: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            LogToDebug($"App constructor error: {ex.Message}");
            throw;
        }
    }

    /// <summary>Bắt đầu heartbeat khi đã có gói — gọi từ OnStart/OnResume hoặc sau khi chuyển sang AppShell.</summary>
    public static void TryStartHeartbeatForActiveSubscription()
    {
        if (Services?.GetService<ISubscriptionService>() is not { IsActive: true }) return;
        try
        {
            Services.GetRequiredService<HeartbeatService>().Start();
        }
        catch (Exception ex)
        {
            LogToDebug($"TryStartHeartbeat: {ex.Message}");
        }
    }

    protected override void OnStart()
    {
        base.OnStart();
        TryStartHeartbeatForActiveSubscription();
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        _heartbeat.Stop();
    }

    protected override void OnResume()
    {
        base.OnResume();
        TryStartHeartbeatForActiveSubscription();
    }

    private static void LogToDebug(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[{LogTag}] {message}");
        AppLog.Info(message);
    }
}
