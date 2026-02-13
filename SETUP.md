# Hướng dẫn Cài đặt và Chạy HeriStepAI

## Yêu cầu hệ thống
- .NET 8.0 SDK
- **IDE/Editor** (bất kỳ):
  - Visual Studio 2022 (Windows/Mac)
  - Visual Studio Code + C# extension
  - JetBrains Rider
  - Cursor (hiện tại bạn đang dùng)
  - Hoặc bất kỳ editor nào hỗ trợ C#
- **Supabase account** (hoặc PostgreSQL database)
- File `.env` với connection string (xem `SUPABASE_SETUP.md`)
- **Không cần API key** - Sử dụng OpenStreetMap miễn phí

### Cho Mobile App (Android):
- Android SDK (tự động cài với .NET MAUI workload)
- Android Emulator hoặc thiết bị Android thật
- Hoặc có thể build APK và cài đặt thủ công

## Cài đặt

### 1. Backend API

**Bước 1: Cấu hình Supabase**
1. Tạo file `.env` trong thư mục gốc (copy từ `.env.example`)
2. Điền thông tin Supabase connection string (xem `SUPABASE_SETUP.md`)

**Bước 2: Cài đặt và chạy**
```bash
cd src/HeriStepAI.API
dotnet restore
dotnet run
```

API sẽ tự động:
- Đọc cấu hình từ file `.env`
- Kết nối đến Supabase PostgreSQL
- Tạo database tables tự động
- Seed dữ liệu mẫu

API sẽ chạy tại: `https://localhost:7001`

**Lưu ý:** Nếu dùng migrations:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

**Tài khoản mặc định:**
- Admin: `admin@heristepai.com` / `admin123`
- Shop Owner: `owner@shop.com` / `owner123`

### 2. Web Admin

```bash
cd src/HeriStepAI.Web
dotnet restore
dotnet run
```

Web sẽ chạy tại: `https://localhost:5001`

**Lưu ý:** Cập nhật `ApiSettings:BaseUrl` trong `appsettings.json` nếu API chạy ở port khác.

### 3. Mobile App

#### Cài đặt .NET MAUI Workload (chỉ cần làm 1 lần)
```bash
dotnet workload install maui
dotnet workload install maui-android  # Cho Android
```

#### Android - Cách 1: Command Line (Khuyến nghị cho Cursor)

**Bước 1: Cập nhật API URL** (nếu cần)
Mở `src/HeriStepAI.Mobile/Services/ApiService.cs` và cập nhật:
```csharp
private readonly string _baseUrl = "https://localhost:7001/api/"; // Hoặc IP của máy
```

**Bước 2: List Android devices/emulators**
```bash
cd src/HeriStepAI.Mobile
dotnet build -t:Run -f net8.0-android
```

Hoặc list devices có sẵn:
```bash
# Windows
adb devices

# Hoặc dùng MAUI CLI
dotnet maui list-devices
```

**Bước 3: Build và chạy**
```bash
# Build APK
dotnet build -f net8.0-android -c Release

# Hoặc build và chạy trên emulator/device
dotnet build -f net8.0-android -t:Run

# Hoặc chỉ build APK để cài thủ công
dotnet publish -f net8.0-android -c Release
# APK sẽ ở: bin/Release/net8.0-android/publish/
```

**Bước 4: Cài APK vào thiết bị**
```bash
# Kết nối thiết bị qua USB (bật USB Debugging)
adb install bin/Release/net8.0-android/publish/com.companyname.heristepai.mobile-Signed.apk
```

#### Android - Cách 2: Visual Studio Code
1. Cài extension: **.NET MAUI**
2. Mở folder `src/HeriStepAI.Mobile`
3. Nhấn `F5` hoặc chọn "Run and Debug"
4. Chọn "Android" và device/emulator

#### Android - Cách 3: JetBrains Rider
1. Mở solution trong Rider
2. Chọn project `HeriStepAI.Mobile`
3. Chọn Android device từ dropdown
4. Nhấn Run

#### Android - Cách 4: Visual Studio 2022
1. Mở solution trong Visual Studio
2. Chọn project `HeriStepAI.Mobile`
3. Chọn Android emulator hoặc thiết bị
4. Nhấn F5 để chạy

#### iOS (Chỉ trên Mac)
```bash
cd src/HeriStepAI.Mobile
dotnet build -f net8.0-ios -t:Run
```

**Lưu ý:** 
- Cần Mac với Xcode đã cài đặt
- Cần Apple Developer account (free hoặc paid)

#### Test trên thiết bị thật (Android)

1. **Bật USB Debugging trên Android:**
   - Settings > About phone > Tap "Build number" 7 lần
   - Settings > Developer options > Enable "USB debugging"

2. **Kết nối thiết bị và kiểm tra:**
   ```bash
   adb devices
   # Nên thấy device ID
   ```

3. **Build và cài đặt:**
   ```bash
   cd src/HeriStepAI.Mobile
   dotnet build -f net8.0-android -c Release
   dotnet publish -f net8.0-android -c Release
   adb install bin/Release/net8.0-android/publish/*.apk
   ```

#### Troubleshooting Mobile App

**Không thấy Android device:**
```bash
# Kiểm tra ADB
adb devices

# Restart ADB
adb kill-server
adb start-server
```

**Build lỗi:**
```bash
# Clean và rebuild
dotnet clean
dotnet restore
dotnet build -f net8.0-android
```

**Không chạy được trên Cursor:**
- Cursor hiện tại **không hỗ trợ trực tiếp** run MAUI app
- **Giải pháp**: Dùng command line (như hướng dẫn trên)
- Hoặc build APK và cài thủ công vào thiết bị
- Hoặc dùng Android Studio để chạy emulator, rồi dùng `adb` để cài app

## Bản đồ

Ứng dụng sử dụng **Leaflet + OpenStreetMap** - hoàn toàn **miễn phí**, không cần API key:
- ✅ Không cần đăng ký tài khoản
- ✅ Không có giới hạn request
- ✅ Open source và miễn phí vĩnh viễn
- ✅ Hỗ trợ đầy đủ: markers, popups, click events, zoom, pan

## Cấu hình Database

Connection string mặc định sử dụng LocalDB. Có thể thay đổi trong `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=HeriStepAI;Trusted_Connection=True;"
  }
}
```

## API Endpoints

### Authentication
- `POST /api/auth/login` - Đăng nhập
- `POST /api/auth/register` - Đăng ký (Admin only)
- `GET /api/auth/me` - Lấy thông tin user hiện tại

### POI
- `GET /api/poi` - Lấy tất cả POIs
- `GET /api/poi/{id}` - Lấy POI theo ID
- `GET /api/poi/{id}/content/{language}` - Lấy nội dung thuyết minh
- `POST /api/poi` - Tạo POI mới
- `PUT /api/poi/{id}` - Cập nhật POI
- `DELETE /api/poi/{id}` - Xóa POI

### Analytics
- `POST /api/analytics/visit` - Log lượt truy cập
- `GET /api/analytics/top-pois` - Top POIs được truy cập nhiều nhất
- `GET /api/analytics/poi/{id}/statistics` - Thống kê POI
- `GET /api/analytics/poi/{id}/logs` - Lịch sử truy cập POI

## Tính năng chính

### Mobile App
- ✅ GPS tracking real-time
- ✅ Geofencing tự động
- ✅ Map view với Mapbox
- ✅ TTS/Audio narration
- ✅ Offline support với SQLite
- ✅ Đa ngôn ngữ

### Web Admin
- ✅ Dashboard thống kê
- ✅ Quản lý POI
- ✅ Xem analytics theo shop owner
- ✅ Authentication với JWT

### Backend API
- ✅ RESTful API
- ✅ JWT Authentication
- ✅ Role-based authorization
- ✅ Analytics tracking
- ✅ POI management

## Troubleshooting

### API không kết nối được
- Kiểm tra connection string
- Đảm bảo SQL Server LocalDB đã cài đặt
- Kiểm tra firewall settings

### Mobile App không hiển thị map
- Kiểm tra internet connection (cần để load OpenStreetMap tiles)
- Kiểm tra quyền truy cập location
- Kiểm tra WebView có được enable trong project

### GPS không hoạt động
- Kiểm tra quyền location đã được cấp
- Kiểm tra GPS đã bật trên thiết bị
- Kiểm tra AndroidManifest.xml / Info.plist đã có permissions

### Cursor không chạy được Mobile App
- **Bình thường**: Cursor không hỗ trợ trực tiếp run MAUI app
- **Giải pháp**: Dùng command line (xem `MOBILE_APP_GUIDE.md`)
- Build APK và cài thủ công vào thiết bị
- Hoặc dùng Android Studio để chạy emulator

### API không kết nối từ mobile
- Đảm bảo API đang chạy
- Thay `localhost` bằng IP thật của máy (ví dụ: `192.168.1.100`)
- Kiểm tra firewall cho phép port 7001
- Đảm bảo mobile và máy tính cùng mạng WiFi
