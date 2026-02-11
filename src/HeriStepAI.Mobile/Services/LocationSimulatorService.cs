using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

public interface ILocationSimulatorService
{
    bool IsSimulating { get; }
    void StartSimulation(List<POI> route, int maxSecondsPerPOI = 90);
    void StopSimulation();
    /// <summary>
    /// Gọi khi narration hoàn tất để chuyển sang POI tiếp theo.
    /// Nếu không gọi, simulator tự advance sau maxSecondsPerPOI.
    /// </summary>
    void AdvanceToNext();
    event EventHandler<Location>? LocationChanged;
    event EventHandler? SimulationCompleted;
}

public class LocationSimulatorService : ILocationSimulatorService
{
    private bool _isSimulating;
    private CancellationTokenSource? _cts;
    private int _currentIndex;
    private List<POI>? _route;
    private TaskCompletionSource<bool>? _advanceSignal;

    public bool IsSimulating => _isSimulating;

    public event EventHandler<Location>? LocationChanged;
    public event EventHandler? SimulationCompleted;

    public void AdvanceToNext()
    {
        _advanceSignal?.TrySetResult(true);
    }

    public void StartSimulation(List<POI> route, int maxSecondsPerPOI = 90)
    {
        if (_isSimulating) StopSimulation();

        _route = route;
        _currentIndex = 0;
        _isSimulating = true;
        _cts = new CancellationTokenSource();

        AppLog.Info($"🧪 Starting simulation: {route.Count} POIs, max {maxSecondsPerPOI}s/POI");
        _ = SimulateMovementAsync(maxSecondsPerPOI, _cts.Token);
    }

    public void StopSimulation()
    {
        _cts?.Cancel();
        _advanceSignal?.TrySetCanceled();
        _isSimulating = false;
        _route = null;
        _currentIndex = 0;
        AppLog.Info("🛑 Location simulation stopped");
    }

    private async Task SimulateMovementAsync(int maxSecondsPerPOI, CancellationToken ct)
    {
        if (_route == null || _route.Count == 0) return;

        try
        {
            while (!ct.IsCancellationRequested && _currentIndex < _route.Count)
            {
                var poi = _route[_currentIndex];

                // Tạo advance signal TRƯỚC khi emit location
                // (để khi geofence → narration kết thúc, signal đã sẵn sàng)
                _advanceSignal = new TaskCompletionSource<bool>();

                // Emit location tại POI
                var location = new Location(poi.Latitude, poi.Longitude)
                {
                    Accuracy = 10.0,
                    Timestamp = DateTimeOffset.UtcNow
                };
                LocationChanged?.Invoke(this, location);

                AppLog.Info($"🚶 Simulated [{_currentIndex + 1}/{_route.Count}]: {poi.Name}");

                // Chờ narration xong (AdvanceToNext) HOẶC max timeout
                var timeout = Task.Delay(maxSecondsPerPOI * 1000, ct);
                await Task.WhenAny(_advanceSignal.Task, timeout);

                // Delay nhỏ giữa các POI để map animation kịp render
                await Task.Delay(2000, ct);

                _currentIndex++;
            }

            if (!ct.IsCancellationRequested)
            {
                AppLog.Info($"✅ Simulation completed: visited all {_route.Count} POIs");
                _isSimulating = false;
                SimulationCompleted?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (OperationCanceledException)
        {
            AppLog.Info("Simulation task cancelled");
        }
        catch (Exception ex)
        {
            AppLog.Error($"Simulation error: {ex.Message}");
        }
        finally
        {
            _isSimulating = false;
        }
    }
}
