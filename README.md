# HeriStepAI - Hệ thống Thuyết minh Tự động cho Du lịch

## Tổng quan
Hệ thống thuyết minh tự động cho khách du lịch khi đi vào các khu vực hàng quán. Ứng dụng sử dụng GPS tracking, geofencing, và TTS để tự động phát thuyết minh khi người dùng vào vùng của một cửa hàng.

## Cấu trúc Dự án

### 1. HeriStepAI.API
Backend API cung cấp:
- Authentication & Authorization
- POI (Point of Interest) Management
- Analytics & Statistics
- Content Management

### 2. HeriStepAI.Web
Web application cho Admin và chủ cửa hàng:
- Dashboard thống kê
- Quản lý POI
- Xem số lượt truy cập địa điểm

### 3. HeriStepAI.Mobile
Mobile app (.NET MAUI) cho khách du lịch:
- GPS tracking real-time
- Geofencing
- Map view với Leaflet + OpenStreetMap (miễn phí)
- TTS/Audio narration
- Offline support

## Công nghệ sử dụng
- .NET 8.0
- ASP.NET Core Web API
- ASP.NET Core MVC
- .NET MAUI
- Entity Framework Core
- SQLite (Mobile)
- SQL Server (Backend)
- Leaflet + OpenStreetMap (miễn phí, không cần API key)
- JWT Authentication

## Cài đặt

### Backend API
```bash
cd src/HeriStepAI.API
dotnet restore
dotnet run
```

### Web Admin
```bash
cd src/HeriStepAI.Web
dotnet restore
dotnet run
```

### Mobile App
```bash
cd src/HeriStepAI.Mobile
dotnet restore
dotnet build
```

## Cấu hình
1. Cập nhật connection string trong `appsettings.json`
2. Cập nhật API URL trong mobile app nếu cần (mặc định: `https://localhost:7001/api/`)
3. Cấu hình JWT settings trong API
4. **Không cần cấu hình bản đồ** - Sử dụng OpenStreetMap miễn phí