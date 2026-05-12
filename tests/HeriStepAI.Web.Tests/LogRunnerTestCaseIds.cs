namespace HeriStepAI.Web.Tests;

/// <summary>
/// Id dùng cho trait xUnit <c>[Trait("LogRunner", Id)]</c> — chạy lọc:
/// <c>dotnet test tests/HeriStepAI.Web.Tests/HeriStepAI.Web.Tests.csproj --filter "LogRunner=LR-001"</c>
/// hoặc nhiều case: <c>--filter "(LogRunner=LR-001)|(LogRunner=LR-002)"</c>
/// </summary>
public static class LogRunnerTestCaseIds
{
    public const string LR001_Overwrite_Empty = "LR-001";
    public const string LR002_Overwrite_Roundtrip = "LR-002";
    public const string LR003_Overwrite_NormalizesNewlines = "LR-003";
    public const string LR004_Read_MissingFile_EmptyString = "LR-004";
    public const string LR005_ReadBytes_MissingFile_Empty = "LR-005";
    public const string LR006_Session_Whitespace_NoBanner = "LR-006";
    public const string LR007_Session_WithId_HasBanner = "LR-007";
    public const string LR008_ReadBytes_Utf8Roundtrip = "LR-008";
    public const string LR009_Sequential_LastWriteWins = "LR-009";
    public const string LR010_Interleaved_ReadWrite = "LR-010";
    public const string LR011_ParallelOverwrites_AllComplete = "LR-011";
    public const string LR012_Dispose_BlocksFurtherWrites = "LR-012";
    public const string LR013_Overwrite_NullText = "LR-013";
    public const string LR014_ReadThenRead_Consistent = "LR-014";
    public const string LR015_LogFilePath_Exposed = "LR-015";

    public static IReadOnlyList<string> All { get; } =
    [
        LR001_Overwrite_Empty,
        LR002_Overwrite_Roundtrip,
        LR003_Overwrite_NormalizesNewlines,
        LR004_Read_MissingFile_EmptyString,
        LR005_ReadBytes_MissingFile_Empty,
        LR006_Session_Whitespace_NoBanner,
        LR007_Session_WithId_HasBanner,
        LR008_ReadBytes_Utf8Roundtrip,
        LR009_Sequential_LastWriteWins,
        LR010_Interleaved_ReadWrite,
        LR011_ParallelOverwrites_AllComplete,
        LR012_Dispose_BlocksFurtherWrites,
        LR013_Overwrite_NullText,
        LR014_ReadThenRead_Consistent,
        LR015_LogFilePath_Exposed
    ];
}
