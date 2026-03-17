# Chạy script này khi app bị "isn't responding" để lưu logcat (cần cắm máy + adb)
# Usage: .\scripts\capture-anr-logcat.ps1
# Output: logcat_anr_YYYYMMDD_HHmmss.txt

$out = "logcat_anr_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
$outPath = Join-Path $PSScriptRoot $out
Write-Host "Dang ghi logcat vao $outPath (Ctrl+C de dung)..."
adb logcat -d -v threadtime *:V | Out-File -FilePath $outPath -Encoding utf8
Write-Host "Da luu. Tim ANR: Select-String -Path $out -Pattern 'ANR|not responding|am_anr'"
adb logcat -c
adb logcat -v threadtime *:V | Tee-Object -FilePath $outPath -Append
