using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

public interface ILocationSimulatorService
{
    bool IsSimulating { get; }
    void StartSimulation(List<POI> route, int delaySeconds = 10);
    void StopSimulation();
    event EventHandler<Location>? LocationChanged;
}

public class LocationSimulatorService : ILocationSimulatorService
{
    private bool _isSimulating;
    private CancellationTokenSource? _cts;
    private int _currentIndex;
    private List<POI>? _route;

    public bool IsSimulating => _isSimulating;

    public event EventHandler<Location>? LocationChanged;

    public void StartSimulation(List<POI> route, int delaySeconds = 10)
    {
        if (_isSimulating) StopSimulation();

        _route = route;
        _currentIndex = 0;
        _isSimulating = true;
        _cts = new CancellationTokenSource();

        AppLog.Info($"🧪 Starting location simulation with {route.Count} POIs");
        _ = SimulateMovementAsync(delaySeconds, _cts.Token);
    }

    public void StopSimulation()
    {
        _cts?.Cancel();
        _isSimulating = false;
        _route = null;
        _currentIndex = 0;
        AppLog.Info("🛑 Location simulation stopped");
    }

    private async Task SimulateMovementAsync(int delaySeconds, CancellationToken ct)
    {
        if (_route == null || _route.Count == 0) return;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (_currentIndex >= _route.Count)
                {
                    // Restart from beginning
                    _currentIndex = 0;
                    AppLog.Info("🔄 Simulation restarted from beginning");
                }

                var poi = _route[_currentIndex];

                // Tạo location tại POI
                var location = new Location(poi.Latitude, poi.Longitude)
                {
                    Accuracy = 10.0,
                    Timestamp = DateTimeOffset.UtcNow
                };

                LocationChanged?.Invoke(this, location);

                AppLog.Info($"🚶 Simulated location {_currentIndex + 1}/{_route.Count}: {poi.Name} ({poi.Latitude:F6}, {poi.Longitude:F6})");

                // Đợi trước khi chuyển sang POI tiếp theo
                await Task.Delay(delaySeconds * 1000, ct);

                _currentIndex++;
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
    }
}
