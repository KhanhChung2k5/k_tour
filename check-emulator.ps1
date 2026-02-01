Write-Host "=== Android Emulator Check ===" -ForegroundColor Green

# Check virtualization
Write-Host "`n1. Checking Virtualization..." -ForegroundColor Yellow
try {
    $virt = (Get-CimInstance Win32_ComputerSystem).HypervisorPresent
    if ($virt) {
        Write-Host "   ✓ Virtualization: Enabled" -ForegroundColor Green
    } else {
        Write-Host "   ✗ Virtualization: Disabled (Cần enable trong BIOS)" -ForegroundColor Red
    }
} catch {
    Write-Host "   ⚠ Could not check virtualization" -ForegroundColor Yellow
}

# Check Android SDK
Write-Host "`n2. Checking Android SDK..." -ForegroundColor Yellow
$sdkPath = "$env:LOCALAPPDATA\Android\Sdk"
if (Test-Path $sdkPath) {
    Write-Host "   ✓ SDK Path: $sdkPath" -ForegroundColor Green
} else {
    Write-Host "   ✗ SDK not found at default location" -ForegroundColor Red
    Write-Host "   Expected: $sdkPath" -ForegroundColor Gray
}

# Check Emulator
Write-Host "`n3. Checking Emulator..." -ForegroundColor Yellow
$emulatorPath = "$sdkPath\emulator\emulator.exe"
if (Test-Path $emulatorPath) {
    Write-Host "   ✓ Emulator found" -ForegroundColor Green
    try {
        $version = & $emulatorPath -version 2>&1 | Select-Object -First 1
        Write-Host "   Version: $version" -ForegroundColor Gray
    } catch {
        Write-Host "   ⚠ Could not get version" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ✗ Emulator not found" -ForegroundColor Red
    Write-Host "   Expected: $emulatorPath" -ForegroundColor Gray
}

# Check AVDs
Write-Host "`n4. Checking AVDs..." -ForegroundColor Yellow
$avdPath = "$env:USERPROFILE\.android\avd"
if (Test-Path $avdPath) {
    $avds = Get-ChildItem $avdPath -Directory -ErrorAction SilentlyContinue
    if ($avds) {
        Write-Host "   Found $($avds.Count) AVD(s):" -ForegroundColor Green
        $avds | ForEach-Object { Write-Host "   - $($_.Name)" -ForegroundColor Gray }
    } else {
        Write-Host "   ⚠ AVD folder exists but no AVDs found" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ✗ No AVDs found" -ForegroundColor Red
    Write-Host "   Expected: $avdPath" -ForegroundColor Gray
}

# Check RAM
Write-Host "`n5. Checking System Resources..." -ForegroundColor Yellow
$os = Get-CimInstance Win32_OperatingSystem
$totalRAM = [math]::Round($os.TotalVisibleMemorySize / 1MB, 2)
$freeRAM = [math]::Round($os.FreePhysicalMemory / 1MB, 2)
Write-Host "   Total RAM: $totalRAM GB" -ForegroundColor Gray
Write-Host "   Free RAM: $freeRAM GB" -ForegroundColor $(if ($freeRAM -gt 4) { "Green" } else { "Yellow" })

Write-Host "`n=== Recommendations ===" -ForegroundColor Cyan
if (-not $virt) {
    Write-Host "1. Enable Virtualization in BIOS" -ForegroundColor Yellow
}
if (-not (Test-Path $emulatorPath)) {
    Write-Host "2. Install Android Emulator from Android Studio SDK Manager" -ForegroundColor Yellow
}
if ($freeRAM -lt 4) {
    Write-Host "3. Free up RAM (need at least 4GB free)" -ForegroundColor Yellow
}
if (-not (Test-Path $avdPath) -or (Get-ChildItem $avdPath -Directory -ErrorAction SilentlyContinue).Count -eq 0) {
    Write-Host "4. Create a new AVD in Android Studio" -ForegroundColor Yellow
}

Write-Host "`n=== Done ===" -ForegroundColor Green
