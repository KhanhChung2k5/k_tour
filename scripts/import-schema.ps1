# Import PostgreSQL Schema from file

param(
    [string]$DbPassword = "admin123",
    [string]$DbName = "heristepai_db",
    [string]$InputFile = "database_schema.sql"
)

Write-Host "📥 Importing database schema..." -ForegroundColor Cyan

if (-not (Test-Path $InputFile)) {
    Write-Host "❌ File not found: $InputFile" -ForegroundColor Red
    exit 1
}

$psqlPath = "C:\Program Files\PostgreSQL\16\bin\psql.exe"

if (-not (Test-Path $psqlPath)) {
    Write-Host "❌ psql not found" -ForegroundColor Red
    exit 1
}

# Set password
$env:PGPASSWORD = $DbPassword

# Import schema
Write-Host "📦 Importing from $InputFile..." -ForegroundColor Yellow
& $psqlPath -U postgres -d $DbName -f $InputFile

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Schema imported successfully" -ForegroundColor Green
    Write-Host ""
    Write-Host "💡 Next steps:" -ForegroundColor Cyan
    Write-Host "   cd src\HeriStepAI.API"
    Write-Host "   dotnet run"
} else {
    Write-Host "❌ Import failed" -ForegroundColor Red
    exit 1
}
