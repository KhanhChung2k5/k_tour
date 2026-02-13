# Hướng dẫn Build và Test Mobile App (Không dùng Visual Studio)

## Tổng quan

Bạn **KHÔNG CẦN** Visual Studio để build và test Mobile App. Có thể dùng:
- ✅ Command Line (dotnet CLI)
- ✅ Visual Studio Code
- ✅ JetBrains Rider
- ✅ Cursor (với command line)
- ✅ Bất kỳ editor nào + terminal

## Cài đặt .NET MAUI Workload

Chỉ cần làm 1 lần:

```bash
# Cài đặt MAUI workload
dotnet workload install maui
dotnet workload install maui-android

# Kiểm tra đã cài chưa
dotnet workload list
```

## Build và Chạy từ Command Line

### 1. Build APK

```bash
cd src/HeriStepAI.Mobile

# Build Debug
dotnet build -f net8.0-android

# Build Release (để publish)
dotnet build -f net8.0-android -c Release
```

### 2. Publish APK

```bash
# Publish APK
dotnet publish -f net8.0-android -c Release

# APK sẽ ở:
# bin/Release/net8.0-android/publish/com.companyname.heristepai.mobile-Signed.apk
```

### 3. Cài APK vào thiết bị

```bash
# Kết nối thiết bị qua USB (bật USB Debugging)
adb devices

# Cài APK
adb install bin/Release/net8.0-android/publish/*.apk

# Hoặc cài lại (nếu đã có)
adb install -r bin/Release/net8.0-android/publish/*.apk
```

## Chạy trên Emulator

### 1. List emulators có sẵn

```bash
# Dùng Android SDK
emulator -list-avds

# Hoặc dùng MAUI CLI
dotnet maui list-devices
```

### 2. Khởi động emulator

```bash
# Khởi động emulator
emulator -avd <emulator-name>

# Hoặc mở Android Studio > AVD Manager > Start
```

### 3. Chạy app trên emulator

```bash
cd src/HeriStepAI.Mobile

# Build và deploy
dotnet build -f net8.0-android -t:Run

# Hoặc nếu emulator đã chạy
adb install bin/Debug/net8.0-android/*.apk
```

## Test trên Thiết bị Thật

### Bước 1: Bật USB Debugging

1. Vào **Settings** > **About phone**
2. Tap **"Build number"** 7 lần để unlock Developer options
3. Vào **Settings** > **Developer options**
4. Bật **"USB debugging"**
5. Kết nối USB và chấp nhận "Allow USB debugging"

### Bước 2: Kiểm tra kết nối

```bash
adb devices
# Nên thấy device ID, ví dụ:
# List of devices attached
# ABC123XYZ    device
```

### Bước 3: Build và cài đặt

```bash
cd src/HeriStepAI.Mobile
dotnet build -f net8.0-android -c Release
dotnet publish -f net8.0-android -c Release
adb install bin/Release/net8.0-android/publish/*.apk
```

## Sử dụng với Cursor

Cursor **không hỗ trợ trực tiếp** run MAUI app, nhưng bạn có thể:

### Cách 1: Dùng Terminal trong Cursor

1. Mở Terminal trong Cursor (`Ctrl + `` ` hoặc `View > Terminal`)
2. Chạy các lệnh như hướng dẫn trên
3. Build và cài APK thủ công

### Cách 2: Build APK và test thủ công

1. Build APK từ terminal
2. Copy APK sang thiết bị
3. Cài đặt thủ công trên thiết bị

### Cách 3: Dùng Android Studio cho Emulator

1. Cài Android Studio
2. Tạo Android Virtual Device (AVD)
3. Chạy emulator từ Android Studio
4. Dùng `adb install` để cài app vào emulator

## Cấu hình API URL cho Mobile

Khi test trên thiết bị thật, `localhost` sẽ không hoạt động. Cần:

1. **Tìm IP của máy chạy API:**
   ```bash
   # Windows
   ipconfig
   # Tìm IPv4 Address, ví dụ: 192.168.1.100

   # Mac/Linux
   ifconfig
   # Hoặc
   hostname -I
   ```

2. **Cập nhật trong `ApiService.cs`:**
   ```csharp
   private readonly string _baseUrl = "http://192.168.1.100:7001/api/";
   // Thay 192.168.1.100 bằng IP của máy bạn
   ```

3. **Đảm bảo API cho phép connection từ network:**
   - Trong `Program.cs`, CORS đã được cấu hình `AllowAnyOrigin()`
   - Firewall có thể cần allow port 7001

## Debugging

### Xem logs từ app

```bash
# Xem logs real-time
adb logcat | findstr "HeriStepAI"

# Hoặc filter theo tag
adb logcat -s "HeriStepAI:*"
```

### Xem logs từ .NET

```bash
# Logs sẽ hiển thị trong terminal khi build với -t:Run
dotnet build -f net8.0-android -t:Run
```

## Troubleshooting

### Lỗi: "No devices found"

```bash
# Kiểm tra ADB
adb devices

# Restart ADB
adb kill-server
adb start-server

# Kiểm tra USB cable và USB debugging đã bật chưa
```

### Lỗi: "Workload maui not installed"

```bash
dotnet workload install maui
dotnet workload install maui-android
```

### Lỗi: "Android SDK not found"

```bash
# Cài Android SDK
# Windows: Cài Android Studio, SDK sẽ tự động cài
# Hoặc set biến môi trường:
# ANDROID_HOME=C:\Users\YourName\AppData\Local\Android\Sdk
```

### Build chậm lần đầu

- Lần đầu build sẽ chậm vì cần download dependencies
- Các lần sau sẽ nhanh hơn

### APK quá lớn

- APK Debug thường lớn hơn Release
- Dùng `-c Release` để build APK nhỏ hơn
- Có thể dùng `dotnet publish` với các tùy chọn tối ưu

## Tips

1. **Hot Reload**: Không hỗ trợ từ command line, cần IDE
2. **Fast Deployment**: Dùng `-t:Run` thay vì build + install riêng
3. **Multiple Devices**: Có thể cài cùng lúc nhiều thiết bị
4. **Wireless Debugging**: Android 11+ hỗ trợ debug qua WiFi (không cần USB)

## Kết luận

Bạn **KHÔNG CẦN** Visual Studio để phát triển MAUI app. Command line đủ mạnh để:
- ✅ Build APK
- ✅ Deploy lên thiết bị
- ✅ Test và debug
- ✅ Publish release

Chỉ cần terminal và một chút kiên nhẫn! 🚀
