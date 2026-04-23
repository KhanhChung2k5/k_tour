using HeriStepAI.API.Data;
using HeriStepAI.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HeriStepAI.API.Services;

public class VisitLogWorker : BackgroundService
{
    private readonly VisitLogQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VisitLogWorker> _logger;

    private const int BatchSize    = 10;
    private const int FlushMs      = 500;   // flush sau 500ms dù chưa đủ batch

    public VisitLogWorker(VisitLogQueue queue, IServiceScopeFactory scopeFactory,
        ILogger<VisitLogWorker> logger)
    {
        _queue       = queue;
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<VisitLogItem>(BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            batch.Clear();

            // Đợi item đầu tiên (blocking)
            try
            {
                var first = await _queue.Reader.ReadAsync(stoppingToken);
                batch.Add(first);
            }
            catch (OperationCanceledException) { break; }

            // Gom thêm items trong FlushMs hoặc đủ BatchSize
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(FlushMs);
            try
            {
                while (batch.Count < BatchSize)
                {
                    var item = await _queue.Reader.ReadAsync(cts.Token);
                    batch.Add(item);
                }
            }
            catch (OperationCanceledException) { /* timeout hoặc stop */ }

            if (batch.Count == 0) continue;

            await FlushBatch(batch, stoppingToken);
        }
    }

    private async Task FlushBatch(List<VisitLogItem> batch, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var now = DateTime.UtcNow;
            var logs = batch.Select(i => new VisitLog
            {
                POId       = i.PoiId,
                UserId     = i.UserId,
                Latitude   = i.Latitude,
                Longitude  = i.Longitude,
                VisitTime  = now,
                VisitType  = i.VisitType
            }).ToList();

            db.VisitLogs.AddRange(logs);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("[VisitLogWorker] Batch INSERT {Count} logs OK", batch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VisitLogWorker] Batch INSERT FAILED ({Count} logs dropped)", batch.Count);
        }
    }
}
