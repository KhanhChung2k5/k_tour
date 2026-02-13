# Cấu trúc Dự án HeriStepAI

## Tổng quan
Dự án HeriStepAI là một hệ thống thuyết minh tự động cho khách du lịch, bao gồm 3 thành phần chính:

1. **Backend API** - ASP.NET Core Web API
2. **Web Admin** - ASP.NET Core MVC
3. **Mobile App** - .NET MAUI

## Cấu trúc Thư mục

```
doan/
├── src/
│   ├── HeriStepAI.API/              # Backend API
│   │   ├── Controllers/             # API Controllers
│   │   │   ├── AuthController.cs
│   │   │   ├── POIController.cs
│   │   │   └── AnalyticsController.cs
│   │   ├── Data/                    # Database Context
│   │   │   └── ApplicationDbContext.cs
│   │   ├── Models/                  # Data Models
│   │   │   ├── User.cs
│   │   │   ├── POI.cs
│   │   │   ├── POIContent.cs
│   │   │   ├── VisitLog.cs
│   │   │   └── Analytics.cs
│   │   ├── Services/                # Business Logic
│   │   │   ├── AuthService.cs
│   │   │   ├── POIService.cs
│   │   │   ├── AnalyticsService.cs
│   │   │   └── SeedService.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── HeriStepAI.Web/              # Web Admin Dashboard
│   │   ├── Controllers/
│   │   │   ├── HomeController.cs
│   │   │   ├── AuthController.cs
│   │   │   └── AnalyticsController.cs
│   │   ├── Views/
│   │   │   ├── Home/
│   │   │   ├── Auth/
│   │   │   └── Analytics/
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   └── HeriStepAI.Mobile/           # Mobile App (.NET MAUI)
│       ├── Models/                  # Data Models
│       │   └── POI.cs
│       ├── Services/                # Services
│       │   ├── LocationService.cs
│       │   ├── GeofenceService.cs
│       │   ├── NarrationService.cs
│       │   ├── POIService.cs
│       │   └── ApiService.cs
│       ├── ViewModels/              # MVVM ViewModels
│       │   ├── MainPageViewModel.cs
│       │   ├── MapPageViewModel.cs
│       │   └── SettingsPageViewModel.cs
│       ├── Views/                   # XAML Views
│       │   ├── MainPage.xaml
│       │   ├── MapPage.xaml
│       │   └── SettingsPage.xaml
│       ├── Converters/              # Value Converters
│       ├── Resources/                # Resources
│       ├── Platforms/                # Platform-specific code
│       │   ├── Android/
│       │   └── iOS/
│       ├── MauiProgram.cs
│       └── App.xaml
│
├── HeriStepAI.sln                   # Solution file
├── README.md                        # Tổng quan dự án
├── SETUP.md                         # Hướng dẫn cài đặt
└── .gitignore                       # Git ignore rules
```

## Kiến trúc

### Backend API
- **Framework**: ASP.NET Core 8.0
- **Database**: SQL Server (LocalDB)
- **Authentication**: JWT Bearer
- **ORM**: Entity Framework Core

### Web Admin
- **Framework**: ASP.NET Core MVC 8.0
- **Authentication**: JWT + Cookies
- **UI**: Bootstrap 5 + Chart.js

### Mobile App
- **Framework**: .NET MAUI 8.0
- **Platforms**: Android, iOS
- **Database**: SQLite (offline)
- **Maps**: Leaflet + OpenStreetMap (miễn phí)
- **Location**: MAUI Essentials Geolocation
- **TTS**: Platform-native (Android TextToSpeech, iOS AVSpeechSynthesizer)

## Luồng hoạt động

### 1. GPS Tracking
```
Mobile App → LocationService → GetCurrentLocationAsync()
         → LocationChanged Event
         → GeofenceService.CheckGeofence()
```

### 2. Geofencing
```
Location Update → Check Distance to POIs
              → If within radius → Trigger POIEntered Event
              → Log Visit → Play Narration
```

### 3. Narration
```
POI Entered → Get Content (by language)
           → Check ContentType (TTS/Audio)
           → Play via NarrationService
           → Queue Management (prevent duplicates)
```

### 4. Analytics
```
Visit Event → API Log Visit
          → Store in Database
          → Aggregate Statistics
          → Display in Web Dashboard
```

## Database Schema

### Users
- Id, Username, Email, PasswordHash, Role, CreatedAt, IsActive

### POIs
- Id, Name, Description, Latitude, Longitude, Radius, Priority, OwnerId, ImageUrl, MapLink, IsActive

### POIContents
- Id, POId, Language, TextContent, AudioUrl, ContentType

### VisitLogs
- Id, POId, UserId, VisitTime, Latitude, Longitude, VisitType, DurationSeconds

### Analytics
- Id, POId, Date, VisitCount, UniqueVisitors, AverageDuration

## API Endpoints

### Authentication
- `POST /api/auth/login`
- `POST /api/auth/register` (Admin only)
- `GET /api/auth/me`

### POI Management
- `GET /api/poi` - List all POIs
- `GET /api/poi/{id}` - Get POI details
- `GET /api/poi/{id}/content/{language}` - Get narration content
- `POST /api/poi` - Create POI
- `PUT /api/poi/{id}` - Update POI
- `DELETE /api/poi/{id}` - Delete POI
- `GET /api/poi/my-pois` - Get owner's POIs

### Analytics
- `POST /api/analytics/visit` - Log visit
- `GET /api/analytics/top-pois` - Top visited POIs
- `GET /api/analytics/poi/{id}/statistics` - POI statistics
- `GET /api/analytics/poi/{id}/logs` - Visit logs

## Tính năng chính

### ✅ Đã hoàn thành
- [x] Backend API với JWT Authentication
- [x] POI Management (CRUD)
- [x] Multi-language content support
- [x] Analytics & Statistics
- [x] Web Admin Dashboard
- [x] Mobile App với GPS tracking
- [x] Geofencing engine
- [x] Map integration (Leaflet + OpenStreetMap - miễn phí)
- [x] TTS/Audio narration
- [x] Offline support (SQLite)
- [x] Visit logging
- [x] Role-based access control

### 🔄 Cần cải thiện
- [ ] Real TTS implementation (hiện tại là mock)
- [ ] Audio file download & caching
- [ ] Background location service (Android/iOS native)
- [ ] Push notifications
- [ ] QR Code scanning
- [ ] Advanced analytics charts
- [ ] POI image upload
- [ ] Multi-tour support

## Bảo mật

- JWT token-based authentication
- Password hashing với BCrypt
- Role-based authorization (Admin, ShopOwner, Tourist)
- HTTPS enforcement
- CORS configuration
- SQL injection protection (EF Core)

## Performance

- SQLite caching cho mobile (offline)
- Connection pooling
- Async/await patterns
- Efficient geofencing với Haversine formula
- Debounce & cooldown cho geofence triggers
