using System.Text;
using HeriStepAI.Web.Services;
using Xunit;

namespace HeriStepAI.Web.Tests;

public sealed class FileLogRunnerTests
{
    private static string CreateIsolatedLogPath()
    {
        var dir = Path.Combine(Path.GetTempPath(), "HeriStepAI_LogRunnerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "logqueue.txt");
    }

    [Fact]
    [Trait("LogRunner", LogRunnerTestCaseIds.LR001_Overwrite_Empty)]
    public async Task LR001_Overwrite_Empty_ReadsEmpty()
    {
        var path = CreateIsolatedLogPath();
        using var runner = new FileLogRunner(path);
        await runner.OverwriteAsync("");
        var text = await runner.ReadAsync();
        Assert.Equal("", text);
    }

    [Fact]
    [Trait("LogRunner", LogRunnerTestCaseIds.LR002_Overwrite_Roundtrip)]
    public async Task LR002_Overwrite_Roundtrip()
    {
        var path = CreateIsolatedLogPath();
        using var runner = new FileLogRunner(path);
        const string payload = "hello queue";
        await runner.OverwriteAsync(payload);
        Assert.Equal(payload, await runner.ReadAsync());
    }

    [Fact]
    [Trait("LogRunner", LogRunnerTestCaseIds.LR003_Overwrite_NormalizesNewlines)]
    public async Task LR003_Overwrite_NormalizesNewlines()
    {
        var path = CreateIsolatedLogPath();
        using var runner = new FileLogRunner(path);
        await runner.OverwriteAsync("a\nb\r\nc");
        var text = await runner.ReadAsync();
        var nl = Environment.NewLine;
        Assert.Equal($"a{nl}b{nl}c", text);
    }

    [Fact]
    [Trait("LogRunner", LogRunnerTestCaseIds.LR004_Read_MissingFile_EmptyString)]
    public async Task LR004_Read_MissingFile_ReturnsEmptyString()
    {
        var path = CreateIsolatedLogPath();
        File.Delete(path);
        using var runner = new FileLogRunner(path);
        Assert.Equal("", await runner.ReadAsync());
    }

    [Fact]
    [Trait("LogRunner", LogRunnerTestCaseIds.LR005_ReadBytes_MissingFile_Empty)]
    public async Task LR005_ReadBytes_MissingFile_ReturnsEmpty()
    {
        var path = CreateIsolatedLogPath();
        File.Delete(path);
        using var runner = new FileLogRunner(path);
        Assert.Empty(await runner.ReadBytesAsync());
    }

    [Fact]
    [Trait("LogRunner", LogRunnerTestCaseIds.LR006_Session_Whitespace_NoBanner)]
    public async Task LR006_Session_Whitespace_NoBanner()
    {
        var path = CreateIsolatedLogPath();
        using var runner = new FileLogRunner(path);
        await runner.OverwriteAsync("body", "   ");
        var text = await runner.ReadAsync();
        Assert.Equal("body", text);
        Assert.DoesNotContain("session:", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("LogRunner", LogRunnerTestCaseIds.LR007_Session_WithId_HasBanner)]
    public async Task LR007_Session_WithId_HasBannerPrefix()
    {
        var path = CreateIsolatedLogPath();
        using var runner = new FileLogRunner(path);
        await runner.OverwriteAsync("tail", "sim-1");
        var text = await runner.ReadAsync();
        Assert.StartsWith("===== session:sim-1 |", text, StringComparison.Ordinal);
        Assert.Contains("tail", text, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("LogRunner", LogRunnerTestCaseIds.LR008_ReadBytes_Utf8Roundtrip)]
    public async Task LR008_ReadBytes_MatchesUtf8File()
    {
        var path = CreateIsolatedLogPath();
        using var runner = new FileLogRunner(path);
        const string unicode = "Việt Nam — queue";
        await runner.OverwriteAsync(unicode);
        var bytes = await runner.ReadBytesAsync();
        Assert.Equal(unicode, Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    [Trait("LogRunner", LogRunnerTestCaseIds.LR009_Sequential_LastWriteWins)]
    public async Task LR009_Sequential_LastWriteWins()
    {
        var path = CreateIsolatedLogPath();
        using var runner = new FileLogRunner(path);
        for (var i = 0; i < 50; i++)
            await runner.OverwriteAsync($"step-{i}");
        Assert.Equal("step-49", await runner.ReadAsync());
    }

    [Fact]
    [Trait("LogRunner", LogRunnerTestCaseIds.LR010_Interleaved_ReadWrite)]
    public async Task LR010_Interleaved_ReadWrite_Consistent()
    {
        var path = CreateIsolatedLogPath();
        using var runner = new FileLogRunner(path);
        await runner.OverwriteAsync("v1");
        Assert.Equal("v1", await runner.ReadAsync());
        await runner.OverwriteAsync("v2");
        Assert.Equal("v2", await runner.ReadAsync());
        var bytes = await runner.ReadBytesAsync();
        Assert.Equal("v2", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    [Trait("LogRunner", LogRunnerTestCaseIds.LR011_ParallelOverwrites_AllComplete)]
    public async Task LR011_ParallelOverwrites_AllComplete()
    {
        var path = CreateIsolatedLogPath();
        using var runner = new FileLogRunner(path);
        const int n = 32;
        var allowed = new HashSet<string>(Enumerable.Range(0, n).Select(i => $"id{i:D4}"));
        await Task.WhenAll(Enumerable.Range(0, n).Select(i => runner.OverwriteAsync($"id{i:D4}")));
        var final = await runner.ReadAsync();
        Assert.Contains(final, allowed);
    }

    [Fact]
    [Trait("LogRunner", LogRunnerTestCaseIds.LR012_Dispose_BlocksFurtherWrites)]
    public async Task LR012_Dispose_SubsequentOverwriteCanceled()
    {
        var path = CreateIsolatedLogPath();
        var runner = new FileLogRunner(path);
        await runner.OverwriteAsync("ok");
        runner.Dispose();
        var ex = await Record.ExceptionAsync(() => runner.OverwriteAsync("next"));
        Assert.NotNull(ex);
        Assert.True(ex is OperationCanceledException or ObjectDisposedException,
            $"Expected cancel/dispose-related exception, got {ex.GetType().Name}: {ex.Message}");
    }

    [Fact]
    [Trait("LogRunner", LogRunnerTestCaseIds.LR013_Overwrite_NullText)]
    public async Task LR013_Overwrite_NullText_ReadsEmpty()
    {
        var path = CreateIsolatedLogPath();
        using var runner = new FileLogRunner(path);
        await runner.OverwriteAsync(null!);
        Assert.Equal("", await runner.ReadAsync());
    }

    [Fact]
    [Trait("LogRunner", LogRunnerTestCaseIds.LR014_ReadThenRead_Consistent)]
    public async Task LR014_ReadThenRead_SameContent()
    {
        var path = CreateIsolatedLogPath();
        using var runner = new FileLogRunner(path);
        await runner.OverwriteAsync("stable");
        var a = await runner.ReadAsync();
        var b = await runner.ReadAsync();
        Assert.Equal(a, b);
    }

    [Fact]
    [Trait("LogRunner", LogRunnerTestCaseIds.LR015_LogFilePath_Exposed)]
    public void LR015_LogFilePath_MatchesConstructor()
    {
        var path = CreateIsolatedLogPath();
        using var runner = new FileLogRunner(path);
        Assert.Equal(path, runner.LogFilePath);
    }
}
