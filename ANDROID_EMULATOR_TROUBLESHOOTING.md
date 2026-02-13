# Khắc phục Lỗi Android Emulator

## Lỗi: "The emulator process for AVD Pixel_8 has terminated"

Đây là lỗi phổ biến khi emulator không thể khởi động. Dưới đây là các cách khắc phục:

## 1. Kiểm tra Virtualization (Quan trọng nhất)

### Windows:
1. Mở **Task Manager** (Ctrl + Shift + Esc)
2. Vào tab **Performance** > **CPU**
3. Kiểm tra **"Virtualization"** có bật không

Nếu chưa bật:
1. Restart máy
2. Vào **BIOS/UEFI** (thường nhấn F2, F10, F12, hoặc Del khi khởi động)
3. Tìm **Virtualization Technology** hoặc **Intel VT-x** / **AMD-V**
4. **Enable** nó
5. Save và restart

### Kiểm tra bằng PowerShell:
```powershell
systeminfo | findstr /C:"Hyper-V"
```

## 2. Kiểm tra Hyper-V và HAXM Conflict

### Windows:
Nếu bạn dùng **Hyper-V** (Windows Pro/Enterprise):
- Emulator sẽ dùng **Windows Hypervisor Platform** (WHPX)
- Không cần HAXM

Nếu bạn **KHÔNG** dùng Hyper-V:
- Cần cài **Intel HAXM** hoặc **Android Emulator Hypervisor Driver**

### Cài đặt:
1. Mở **Android Studio**
2. **Tools** > **SDK Manager**
3. Tab **SDK Tools**
4. Tích **"Android Emulator Hypervisor Driver for AMD Processors"** (nếu dùng AMD)
   hoặc **"Intel x86 Emulator Accelerator (HAXM installer)"** (nếu dùng Intel)
5. Click **Apply** và cài đặt

## 3. Kiểm tra RAM và Resources

Emulator cần ít nhất:
- **RAM**: 4GB trống (khuyến nghị 8GB+)
- **Disk space**: 2GB+ cho AVD

### Kiểm tra:
```powershell
# Xem RAM available
Get-CimInstance Win32_OperatingSystem | Select-Object FreePhysicalMemory

# Xem disk space
Get-PSDrive C
```

### Giải pháp:
- Đóng các ứng dụng không cần thiết
- Giảm RAM allocation cho emulator:
  1. Android Studio > **Tools** > **AVD Manager**
  2. Click **Edit** (biểu tượng bút chì) trên AVD
  3. **Show Advanced Settings**
  4. Giảm **RAM** xuống 2048 MB hoặc 1536 MB

## 4. Kiểm tra Graphics Driver

### Cập nhật Graphics Driver:
1. **NVIDIA**: Vào NVIDIA GeForce Experience
2. **AMD**: Vào AMD Radeon Software
3. **Intel**: Vào Intel Driver & Support Assistant

### Thử Graphics Mode khác:
1. Android Studio > **Tools** > **AVD Manager
2. Click **Edit** trên AVD
3. **Show Advanced Settings**
4. Thử đổi **Graphics**:
   - **Automatic** → **Software - GLES 2.0**
   - Hoặc **Hardware - GLES 2.0**

## 5. Xóa và Tạo lại AVD

Đôi khi AVD bị corrupt:

1. **Xóa AVD cũ:**
   - Android Studio > **Tools** > **AVD Manager**
   - Click **Delete** (thùng rác) trên AVD Pixel_8

2. **Tạo AVD mới:**
   - Click **Create Virtual Device**
   - Chọn device (ví dụ: Pixel 5)
   - Chọn system image (khuyến nghị: **API 33** hoặc **API 34**)
   - **Finish**

## 6. Kiểm tra Android SDK và Emulator

### Cập nhật:
1. Android Studio > **Tools** > **SDK Manager**
2. Tab **SDK Platforms**: Đảm bảo có Android API 33/34
3. Tab **SDK Tools**: Đảm bảo có:
   - ✅ Android Emulator
   - ✅ Android SDK Platform-Tools
   - ✅ Android SDK Build-Tools

### Kiểm tra bằng command line:
```bash
# Kiểm tra emulator có cài chưa
emulator -version

# List AVDs
emulator -list-avds
```

## 7. Kiểm tra Logs để tìm lỗi cụ thể

### Xem logs:
1. Android Studio > **View** > **Tool Windows** > **Logcat**
2. Hoặc mở terminal và chạy:
```bash
# Windows
%LOCALAPPDATA%\Android\Sdk\emulator\emulator.exe -avd Pixel_8 -verbose

# Hoặc xem log file
type %USERPROFILE%\.android\avd\Pixel_8.avd\hardware-qemu.ini.log
```

### Tìm lỗi phổ biến trong logs:
- `HAX is not working`: Cần cài HAXM hoặc enable virtualization
- `Could not load`: Driver hoặc SDK issue
- `Out of memory`: Không đủ RAM

## 8. Thử Cold Boot

1. Android Studio > **Tools** > **AVD Manager**
2. Click **▼** (dropdown) trên AVD
3. Chọn **Cold Boot Now**

## 9. Kiểm tra Firewall và Antivirus

Đôi khi firewall/antivirus block emulator:

1. **Tạm thời disable** antivirus
2. **Allow** Android Studio và emulator trong firewall
3. Thử chạy lại

## 10. Giải pháp Nhanh (Quick Fixes)

### Fix 1: Restart Android Studio
```bash
# Đóng hoàn toàn Android Studio
# Restart lại
```

### Fix 2: Invalidate Caches
1. Android Studio > **File** > **Invalidate Caches...**
2. Chọn **Invalidate and Restart**

### Fix 3: Reinstall Emulator
1. **Tools** > **SDK Manager** > **SDK Tools**
2. Bỏ tích **Android Emulator**
3. Click **Apply** (uninstall)
4. Tích lại và cài lại

### Fix 4: Dùng Command Line
```bash
# Khởi động emulator từ command line để xem lỗi chi tiết
cd %LOCALAPPDATA%\Android\Sdk\emulator
emulator -avd Pixel_8 -verbose
```

## 11. Thử Emulator khác

Nếu Pixel_8 vẫn lỗi, thử tạo AVD với:
- **Device**: Pixel 5 (nhẹ hơn)
- **API Level**: 33 (ổn định hơn 34)
- **Graphics**: Software - GLES 2.0

## 12. Kiểm tra System Requirements

### Yêu cầu tối thiểu:
- **Windows**: Windows 10 64-bit
- **RAM**: 8GB (khuyến nghị 16GB)
- **Disk**: 10GB trống
- **CPU**: Intel hoặc AMD với virtualization support

## 13. Lỗi cụ thể và Giải pháp

### "HAXM is not installed"
```bash
# Cài HAXM từ Android Studio SDK Manager
# Hoặc download từ: https://github.com/intel/haxm/releases
```

### "VT-x is disabled in BIOS"
- Vào BIOS và enable Virtualization Technology

### "Emulator: ERROR: x86 emulation currently requires hardware acceleration"
- Enable virtualization trong BIOS
- Cài HAXM hoặc Windows Hypervisor Platform

### "PANIC: Missing emulator engine program"
- Reinstall Android Emulator từ SDK Manager

## 14. Alternative: Dùng Thiết bị Thật

Nếu emulator vẫn không chạy, dùng thiết bị Android thật:

1. **Bật USB Debugging** trên thiết bị
2. Kết nối qua USB
3. Chạy:
```bash
adb devices
# Nên thấy device
```

## 15. Kiểm tra nhanh bằng Script

Tạo file `check-emulator.ps1`:

```powershell
Write-Host "=== Android Emulator Check ===" -ForegroundColor Green

# Check virtualization
Write-Host "`n1. Checking Virtualization..." -ForegroundColor Yellow
$virt = (Get-CimInstance Win32_ComputerSystem).HypervisorPresent
if ($virt) {
    Write-Host "   ✓ Virtualization: Enabled" -ForegroundColor Green
} else {
    Write-Host "   ✗ Virtualization: Disabled (Cần enable trong BIOS)" -ForegroundColor Red
}

# Check Android SDK
Write-Host "`n2. Checking Android SDK..." -ForegroundColor Yellow
$sdkPath = "$env:LOCALAPPDATA\Android\Sdk"
if (Test-Path $sdkPath) {
    Write-Host "   ✓ SDK Path: $sdkPath" -ForegroundColor Green
} else {
    Write-Host "   ✗ SDK not found" -ForegroundColor Red
}

# Check Emulator
Write-Host "`n3. Checking Emulator..." -ForegroundColor Yellow
$emulatorPath = "$sdkPath\emulator\emulator.exe"
if (Test-Path $emulatorPath) {
    Write-Host "   ✓ Emulator found" -ForegroundColor Green
    & $emulatorPath -version
} else {
    Write-Host "   ✗ Emulator not found" -ForegroundColor Red
}

# Check AVDs
Write-Host "`n4. Checking AVDs..." -ForegroundColor Yellow
$avdPath = "$env:USERPROFILE\.android\avd"
if (Test-Path $avdPath) {
    $avds = Get-ChildItem $avdPath -Directory
    Write-Host "   Found $($avds.Count) AVD(s):" -ForegroundColor Green
    $avds | ForEach-Object { Write-Host "   - $($_.Name)" }
} else {
    Write-Host "   ✗ No AVDs found" -ForegroundColor Red
}

Write-Host "`n=== Done ===" -ForegroundColor Green
```

Chạy:
```powershell
.\check-emulator.ps1
```

## Kết luận

Thử các bước theo thứ tự:
1. ✅ Enable Virtualization trong BIOS
2. ✅ Cài HAXM hoặc Windows Hypervisor Platform
3. ✅ Cập nhật Graphics Driver
4. ✅ Giảm RAM allocation cho AVD
5. ✅ Tạo lại AVD mới
6. ✅ Xem logs để tìm lỗi cụ thể

Nếu vẫn không được, dùng **thiết bị Android thật** thay vì emulator!
