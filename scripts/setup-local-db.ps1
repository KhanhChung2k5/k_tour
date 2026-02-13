# Setup Local PostgreSQL for HeriStepAI
# Run this script sau khi cài PostgreSQL

param(
    [string]$DbPassword = "admin123",
    [string]$DbName = "heristepai_db"
)

Write-Host "🐘 HeriStepAI - PostgreSQL Local Setup" -ForegroundColor Cyan
Write-Host ""

# Check if PostgreSQL is installed
$psqlPath = "C:\Program Files\PostgreSQL\16\bin\psql.exe"
if (-not (Test-Path $psqlPath)) {
    Write-Host "❌ PostgreSQL not found at $psqlPath" -ForegroundColor Red
    Write-Host "Please install PostgreSQL 16 from: https://www.postgresql.org/download/windows/" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ PostgreSQL found" -ForegroundColor Green

# Set PGPASSWORD environment variable for passwordless connection
$env:PGPASSWORD = $DbPassword

# Check if database exists
Write-Host "🔍 Checking if database '$DbName' exists..." -ForegroundColor Yellow
$dbExists = & $psqlPath -U postgres -lqt | Select-String -Pattern "^\s*$DbName\s"

if ($dbExists) {
    Write-Host "✅ Database '$DbName' already exists" -ForegroundColor Green
} else {
    Write-Host "📦 Creating database '$DbName'..." -ForegroundColor Yellow
    & $psqlPath -U postgres -c "CREATE DATABASE $DbName;"
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Database created successfully" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed to create database" -ForegroundColor Red
        exit 1
    }
}

# Create .env files from examples
Write-Host ""
Write-Host "📝 Creating .env files..." -ForegroundColor Yellow

$apiEnvPath = "src\HeriStepAI.API\.env"
$webEnvPath = "src\HeriStepAI.Web\.env"
$connectionString = "Host=localhost;Port=5432;Database=$DbName;Username=postgres;Password=$DbPassword;SSL Mode=Prefer"

# API .env
if (-not (Test-Path $apiEnvPath)) {
    @"
SUPABASE_CONNECTION_STRING=$connectionString
JWT_SECRET_KEY=YourSuperSecretKeyForJWTTokenGeneration12345
JWT_ISSUER=HeriStepAI
JWT_AUDIENCE=HeriStepAIUsers
"@ | Out-File -FilePath $apiEnvPath -Encoding UTF8
    Write-Host "✅ Created $apiEnvPath" -ForegroundColor Green
} else {
    Write-Host "⚠️  $apiEnvPath already exists (skipped)" -ForegroundColor Yellow
}

# Web .env
if (-not (Test-Path $webEnvPath)) {
    @"
SUPABASE_CONNECTION_STRING=$connectionString
JWT_SECRET_KEY=YourSuperSecretKeyForJWTTokenGeneration12345
JWT_ISSUER=HeriStepAI
JWT_AUDIENCE=HeriStepAIUsers
API_BASE_URL=http://localhost:5000/api/
"@ | Out-File -FilePath $webEnvPath -Encoding UTF8
    Write-Host "✅ Created $webEnvPath" -ForegroundColor Green
} else {
    Write-Host "⚠️  $webEnvPath already exists (skipped)" -ForegroundColor Yellow
}

# Test connection
Write-Host ""
Write-Host "🔌 Testing database connection..." -ForegroundColor Yellow
$testQuery = "SELECT version();"
$result = & $psqlPath -U postgres -d $DbName -c $testQuery 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Connection successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 PostgreSQL Version:" -ForegroundColor Cyan
    $result | Select-Object -First 3 | ForEach-Object { Write-Host $_ }
} else {
    Write-Host "❌ Connection failed" -ForegroundColor Red
    Write-Host $result
    exit 1
}

# Summary
Write-Host ""
Write-Host "🎉 Setup Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Next Steps:" -ForegroundColor Cyan
Write-Host "1. Run API:   cd src\HeriStepAI.API && dotnet run"
Write-Host "2. Run Web:   cd src\HeriStepAI.Web && dotnet run"
Write-Host "3. Open pgAdmin 4 to manage database"
Write-Host ""
Write-Host "🔗 Connection Info:" -ForegroundColor Cyan
Write-Host "   Host:     localhost"
Write-Host "   Port:     5432"
Write-Host "   Database: $DbName"
Write-Host "   Username: postgres"
Write-Host "   Password: $DbPassword"
Write-Host ""
Write-Host "📚 Documentation:" -ForegroundColor Cyan
Write-Host "   - POSTGRESQL_SETUP.md"
Write-Host "   - MIGRATION_GUIDE.md"
Write-Host ""
