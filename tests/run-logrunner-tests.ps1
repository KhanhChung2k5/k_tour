<#
.SYNOPSIS
  Chạy test FileLogRunner (xUnit) với lọc theo mã case.

.EXAMPLE
  .\tests\run-logrunner-tests.ps1
  .\tests\run-logrunner-tests.ps1 -Case LR-001
  .\tests\run-logrunner-tests.ps1 -Case LR-001,LR-007,LR-012
  .\tests\run-logrunner-tests.ps1 -List

  Tương đương dotnet:
  dotnet test tests/HeriStepAI.Web.Tests/HeriStepAI.Web.Tests.csproj --filter "LogRunner=LR-001"
  dotnet test tests/HeriStepAI.Web.Tests/HeriStepAI.Web.Tests.csproj --filter "(LogRunner=LR-001)|(LogRunner=LR-002)"
#>
param(
    [Parameter(Position = 0)]
    [string[]] $Case = @(),
    [switch] $List
)

$ErrorActionPreference = 'Stop'
$proj = Join-Path $PSScriptRoot 'HeriStepAI.Web.Tests\HeriStepAI.Web.Tests.csproj'

# Cho phep -Case LR-003,LR-011 (mot tham so) hoac -Case LR-003 -Case LR-011
$ids = [System.Collections.Generic.List[string]]::new()
foreach ($c in $Case) {
    foreach ($p in ($c -split ',')) {
        $t = $p.Trim()
        if ($t.Length -gt 0) { [void]$ids.Add($t) }
    }
}

if ($List) {
    Write-Host "Danh sách test (lọc theo trait LogRunner=LR-xxx):"
    dotnet test $proj --list-tests --filter "FullyQualifiedName~FileLogRunnerTests"
    Write-Host ""
    Write-Host 'Ma case: LR-001 .. LR-015 - xem tests/HeriStepAI.Web.Tests/LogRunnerTestCaseIds.cs'
    exit 0
}

if ($ids.Count -eq 0) {
    dotnet test $proj --filter "FullyQualifiedName~FileLogRunnerTests"
    exit $LASTEXITCODE
}

$filter = ($ids | ForEach-Object { '(LogRunner=' + $_ + ')' }) -join '|'
dotnet test $proj --filter $filter
exit $LASTEXITCODE
