<#
.SYNOPSIS
  Chay test chon POI geofence (HeriStepAI.Geo.Tests) theo ma GF-xxx.

.EXAMPLE
  .\tests\run-geofence-tests.ps1
  .\tests\run-geofence-tests.ps1 -Case GF-001
  .\tests\run-geofence-tests.ps1 -Case GF-001,GF-002
  .\tests\run-geofence-tests.ps1 -List
#>
param(
    [Parameter(Position = 0)]
    [string[]] $Case = @(),
    [switch] $List
)

$ErrorActionPreference = 'Stop'
$proj = Join-Path $PSScriptRoot 'HeriStepAI.Geo.Tests\HeriStepAI.Geo.Tests.csproj'

$ids = [System.Collections.Generic.List[string]]::new()
foreach ($c in $Case) {
    foreach ($p in ($c -split ',')) {
        $t = $p.Trim()
        if ($t.Length -gt 0) { [void]$ids.Add($t) }
    }
}

if ($List) {
    dotnet test $proj --list-tests --filter "FullyQualifiedName~GeofenceSelectionTests"
    Write-Host ""
    Write-Host "Ma case: GF-001 .. GF-010 - xem tests/HeriStepAI.Geo.Tests/GeofenceTestCaseIds.cs"
    exit 0
}

if ($ids.Count -eq 0) {
    dotnet test $proj --filter "FullyQualifiedName~GeofenceSelectionTests"
    exit $LASTEXITCODE
}

$filter = ($ids | ForEach-Object { '(Geofence=' + $_ + ')' }) -join '|'
dotnet test $proj --filter $filter
exit $LASTEXITCODE
