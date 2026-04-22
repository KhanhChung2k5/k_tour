namespace HeriStepAI.Mobile.Services;

public sealed class HeartbeatService : IDisposable
{
    private readonly IApiService _apiService;
    private IDispatcherTimer? _timer;

    public HeartbeatService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public void Start()
    {
        if (_timer != null) return;

        // Gửi ngay khi mở app, không chờ interval đầu tiên
        _ = _apiService.HeartbeatAsync();

        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(5);
        _timer.Tick += async (_, _) => await _apiService.HeartbeatAsync();
        _timer.Start();
        AppLog.Info("HeartbeatService started");
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer = null;
        AppLog.Info("HeartbeatService stopped");
    }

    public void Dispose() => Stop();
}
