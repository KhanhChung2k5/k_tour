# Cách xem Log khi App crash trên Android

## 1. Chạy logcat TRƯỚC khi mở app

Mở **2 terminal**:

### Terminal 1: Chạy logcat (giữ chạy)
```powershell
adb logcat -s HeriStepAI:* AndroidRuntime:E *:S
```
- Chỉ hiển thị log từ app HeriStepAI và lỗi Android
- Giữ terminal này mở trong khi test

### Terminal 2: Build và chạy app
```powershell
cd src/HeriStepAI.Mobile
dotnet build -f net8.0-android -c Debug
dotnet build -t:Run -f net8.0-android
```
**Lưu ý:** Phải `cd` vào thư mục `HeriStepAI.Mobile` trước. Nếu build từ thư mục gốc với `-f net8.0-android`, sẽ lỗi vì solution gồm cả API/Web (không hỗ trợ Android).

## 2. Cách khác: Ghi log ra file

```powershell
adb logcat -s HeriStepAI:* AndroidRuntime:E *:S > crash_log.txt
```
Sau khi app crash, nhấn Ctrl+C để dừng. Mở `crash_log.txt` để xem.

## 3. Xem toàn bộ log (nhiều thông tin hơn)

```powershell
adb logcat | Select-String -Pattern "HeriStepAI|FATAL|AndroidRuntime|Exception"
```

## 4. Đảm bảo Emulator/Device đã kết nối

```powershell
adb devices
```
Phải thấy device hoặc emulator trong list.

## 5. Nếu gặp lỗi "closed" khi deploy

Lỗi `error : closed` thường do:
- **Emulator chưa boot xong** – Đợi emulator hiện màn hình home rồi mới chạy
- **ADB mất kết nối** – Chạy `adb kill-server` rồi `adb start-server`
- **App crash ngay khi cài** – Xem logcat để biết nguyên nhân

### Khởi động emulator trước:
1. Mở Android Studio > AVD Manager > Start emulator
2. Đợi emulator khởi động xong
3. Chạy logcat (Terminal 1)
4. Chạy app (Terminal 2)

## 6. App đã bật crash logging

- `MainApplication` ghi mọi unhandled exception ra logcat với tag `HeriStepAI`
- Nếu thấy `=== HeriStepAI App Started ===` nghĩa là app đã chạy qua bước khởi tạo
- Nếu crash, tìm dòng `E/HeriStepAI` hoặc `E/AndroidRuntime` trong log

## 7. Trang lỗi thay vì crash

- Nếu một trang (MainPage, MapPage, POIListPage, SettingsPage) không tải được, app sẽ hiển thị **"Lỗi tải [tên trang]"** kèm thông báo lỗi thay vì crash
- Kiểm tra logcat với tag `[CRASH] Failed to create [tên trang]` để biết chi tiết
