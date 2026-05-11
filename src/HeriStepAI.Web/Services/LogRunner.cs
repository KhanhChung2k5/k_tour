using System.Text;
using System.Threading.Channels;

namespace HeriStepAI.Web.Services;

public interface ILogRunner
{
    string LogFilePath { get; }
    Task OverwriteAsync(string text, string? sessionId = null);
    Task<string> ReadAsync();
    Task<byte[]> ReadBytesAsync();
}

public sealed class FileLogRunner : ILogRunner, IDisposable
{
    private readonly string _logPath;
    private readonly Channel<ILogCommand> _channel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _worker;

    public FileLogRunner()
    {
        _logPath = Path.Combine(Path.GetTempPath(), "HeriStepAI", "logqueue.txt");
        _channel = Channel.CreateUnbounded<ILogCommand>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        _worker = Task.Run(ProcessAsync);
    }

    public string LogFilePath => _logPath;

    public async Task OverwriteAsync(string text, string? sessionId = null)
    {
        var command = new OverwriteCommand(text, sessionId);
        await _channel.Writer.WriteAsync(command, _cts.Token);
        await command.Done.Task;
    }

    public async Task<string> ReadAsync()
    {
        var command = new ReadTextCommand();
        await _channel.Writer.WriteAsync(command, _cts.Token);
        return await command.Done.Task;
    }

    public async Task<byte[]> ReadBytesAsync()
    {
        var command = new ReadBytesCommand();
        await _channel.Writer.WriteAsync(command, _cts.Token);
        return await command.Done.Task;
    }

    private async Task ProcessAsync()
    {
        await foreach (var command in _channel.Reader.ReadAllAsync(_cts.Token))
        {
            switch (command)
            {
                case OverwriteCommand c:
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
                        var normalized = (c.Text ?? string.Empty).Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
                        var payload = string.IsNullOrWhiteSpace(c.SessionId)
                            ? normalized
                            : $"===== session:{c.SessionId} | {DateTime.Now:yyyy-MM-dd HH:mm:ss} ====={Environment.NewLine}{normalized}";
                        await File.WriteAllTextAsync(_logPath, payload, Encoding.UTF8, _cts.Token);
                        c.Done.TrySetResult();
                    }
                    catch (Exception ex)
                    {
                        c.Done.TrySetException(ex);
                    }
                    break;

                case ReadTextCommand c:
                    try
                    {
                        if (!File.Exists(_logPath))
                            c.Done.TrySetResult(string.Empty);
                        else
                            c.Done.TrySetResult(await File.ReadAllTextAsync(_logPath, Encoding.UTF8, _cts.Token));
                    }
                    catch (Exception ex)
                    {
                        c.Done.TrySetException(ex);
                    }
                    break;

                case ReadBytesCommand c:
                    try
                    {
                        if (!File.Exists(_logPath))
                            c.Done.TrySetResult(Array.Empty<byte>());
                        else
                            c.Done.TrySetResult(await File.ReadAllBytesAsync(_logPath, _cts.Token));
                    }
                    catch (Exception ex)
                    {
                        c.Done.TrySetException(ex);
                    }
                    break;
            }
        }
    }

    public void Dispose()
    {
        _channel.Writer.TryComplete();
        _cts.Cancel();
        try { _worker.Wait(500); } catch { }
        _cts.Dispose();
    }

    private interface ILogCommand;

    private sealed class OverwriteCommand : ILogCommand
    {
        public OverwriteCommand(string text, string? sessionId)
        {
            Text = text;
            SessionId = sessionId;
        }

        public string Text { get; }
        public string? SessionId { get; }
        public TaskCompletionSource Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class ReadTextCommand : ILogCommand
    {
        public TaskCompletionSource<string> Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class ReadBytesCommand : ILogCommand
    {
        public TaskCompletionSource<byte[]> Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
