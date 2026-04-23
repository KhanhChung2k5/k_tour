using System.Threading.Channels;
using HeriStepAI.API.Models;

namespace HeriStepAI.API.Services;

public class VisitLogQueue
{
    private readonly Channel<VisitLogItem> _channel =
        Channel.CreateBounded<VisitLogItem>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

    public bool Enqueue(VisitLogItem item) =>
        _channel.Writer.TryWrite(item);

    public ChannelReader<VisitLogItem> Reader => _channel.Reader;
}

public record VisitLogItem(
    int PoiId,
    string? UserId,
    double? Latitude,
    double? Longitude,
    VisitType VisitType
);
