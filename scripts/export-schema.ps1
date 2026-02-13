# Export PostgreSQL Schema for sharing with team

param(
    [string]$DbPassword = "admin123",
    [string]$DbName = "heristepai_db",
    [string]$OutputFile = "database_schema.sql"
)

Write-Host "📤 Exporting database schema..." -ForegroundColor Cyan

$pgDumpPath = "C:\Program Files\PostgreSQL\16\bin\pg_dump.exe"

if (-not (Test-Path $pgDumpPath)) {
    Write-Host "❌ pg_dump not found" -ForegroundColor Red
    exit 1
}

# Set password
$env:PGPASSWORD = $DbPassword

# Export schema only (no data)
& $pgDumpPath -U postgres -d $DbName --schema-only -f $OutputFile

if ($LASTEXITCODE -eq 0) {
    $fileSize = (Get-Item $OutputFile).Length / 1KB
    Write-Host "✅ Schema exported successfully" -ForegroundColor Green
    Write-Host "📁 File: $OutputFile ($([math]::Round($fileSize, 2)) KB)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "💡 Share this file with your teammate:" -ForegroundColor Yellow
    Write-Host "   git add $OutputFile"
    Write-Host "   git commit -m 'Update database schema'"
    Write-Host "   git push"
} else {
    Write-Host "❌ Export failed" -ForegroundColor Red
    exit 1
}
