using Microsoft.Maui.Dispatching;

namespace HeriStepAI.Mobile.Services;

/// <summary>
/// Dịch vụ gửi heartbeat đến server để giữ session active.
/// </summary>
public sealed class HeartbeatService : IDisposable
{
    /// <summary>
    /// Dịch vụ API.
    /// </summary>
    private readonly IApiService _apiService;

    /// <summary>
    /// Timer để gửi heartbeat.
    /// </summary>
    private IDispatcherTimer? _timer;

    /// <summary>
    /// Constructor.
    /// </summary>
    public HeartbeatService(IApiService apiService)
    {
        _apiService = apiService;
    }

    /// <summary>
    /// Bắt đầu gửi heartbeat.
    /// </summary>
    public void Start()
    {
        if (_timer != null) return;

        // Gửi ngay khi mở app, không chờ interval đầu tiên
        _ = _apiService.HeartbeatAsync();

        // Application.Current đôi khi chưa gán trong ctor App — tránh NRE khi CreateTimer
        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.GetForCurrentThread();
        if (dispatcher is null)
        {
            AppLog.Info("HeartbeatService: dispatcher unavailable, retry later");
            return;
        }

        _timer = dispatcher.CreateTimer();
        // Gửi heartbeat mỗi 2 giây (TTL server = 3s)
        _timer.Interval = TimeSpan.FromSeconds(2);
        _timer.Tick += async (_, _) => await _apiService.HeartbeatAsync();
        // Bắt đầu gửi heartbeat
        _timer.Start();
        AppLog.Info("HeartbeatService started");
    }

    /// <summary>
    /// Dừng gửi heartbeat.
    /// </summary>
    public void Stop()
    {
        _timer?.Stop();
        _timer = null;
        AppLog.Info("HeartbeatService stopped");
    }

    public void Dispose() => Stop();
}
