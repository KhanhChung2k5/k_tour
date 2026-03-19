# Cấu trúc Web & App – HeriStepAI

## 1. Tổng quan solution

```
doan/
├── src/
│   ├── HeriStepAI.API/      ← REST API (port 5000)
│   ├── HeriStepAI.Web/      ← Web admin/shopowner (port 5001)
│   └── HeriStepAI.Mobile/   ← App MAUI (Android/iOS)
├── .env                     ← SUPABASE_CONNECTION_STRING, JWT, ...
└── HeriStepAI.sln
```

| Thành phần | Công nghệ | Port (local) | Vai trò |
|------------|-----------|--------------|---------|
| **API** | ASP.NET Core Web API | 5000 | Xác thực (JWT), POI, Analytics, DB chính |
| **Web** | ASP.NET Core MVC | 5001 | Giao diện quản trị (Cookie + gọi API), ShopOwner dùng DB trực tiếp |
| **Mobile** | .NET MAUI | — | App du lịch: bản đồ, POI, thuyết minh, gửi visit lên API |

---

## 2. Cấu trúc Web (HeriStepAI.Web)

### 2.1 Thư mục chính

```
HeriStepAI.Web/
├── Controllers/
│   ├── AuthController.cs    → Đăng nhập/đăng xuất, gọi API auth/login, lưu Cookie + JWT
│   ├── HomeController.cs    → Dashboard (Admin): gọi API analytics/summary, top-pois, poi
│   ├── AnalyticsController.cs → Trang Analytics: gọi API top-pois, poi/{id}/statistics
│   ├── POIController.cs     → Quản lý POI (Admin): CRUD qua API + upload ảnh Supabase
│   └── ShopOwnerController.cs → Dashboard/Edit/Statistics cho chủ quán: dùng DbContext trực tiếp
├── Views/
│   ├── Auth/Login.cshtml
│   ├── Home/Dashboard.cshtml, Index.cshtml
│   ├── Analytics/Index.cshtml
│   ├── POI/Index, Create, Edit, Details
│   ├── ShopOwner/Dashboard, Edit, Statistics
│   └── Shared/_AdminLayout.cshtml
├── Services/
│   └── SupabaseStorageService.cs  → Upload ảnh lên Supabase Storage
├── Program.cs               → Cookie auth, HttpClient("API") → localhost:5000/api/
└── appsettings*.json        → ApiSettings:BaseUrl (fallback local)
```

### 2.2 Xác thực Web

- **Scheme:** Cookie (CookieAuthenticationDefaults).
- **Đăng nhập:** User gửi form → Web gọi **API** `POST api/auth/login` → nhận JWT → Web đăng nhập Cookie + lưu JWT vào cookie `AuthToken`.
- **Gọi API từ Web:** Mỗi request (Dashboard, POI, Analytics) gửi `Authorization: Bearer {token}` lấy từ cookie.
- **Phân quyền:** Admin → Home/Dashboard, POI, Analytics. ShopOwner → ShopOwner/Dashboard, Edit, Statistics.

### 2.3 Nguồn dữ liệu Web

| Trang / Chức năng | Nguồn dữ liệu |
|-------------------|----------------|
| Auth/Login | Chỉ gọi API `auth/login`. |
| Home/Dashboard | API: `analytics/summary`, `analytics/top-pois?count=10`, `poi`. |
| Analytics | API: `analytics/top-pois?count=1000`, `poi`, `analytics/poi/{id}/statistics`. |
| POI (Index/Create/Edit/Details) | API: `poi`, `poi/{id}`; upload ảnh qua SupabaseStorageService. |
| ShopOwner/* | **DbContext (ApplicationDbContext)** trực tiếp: POIs, VisitLogs (cùng DB với API). |

Web có reference tới **HeriStepAI.API** (DbContext, Models) nhưng **không** load API Controllers (chỉ dùng cho DbContext + types). API chạy riêng process (port 5000).

---

## 3. Cấu trúc App Mobile (HeriStepAI.Mobile)

### 3.1 Thư mục / màn hình chính

```
HeriStepAI.Mobile/
├── App.xaml.cs              → Khởi động: TryRestoreSession → AppShell hoặc LoginPage; sync POI nền
├── AppShell.xaml             → TabBar: Trang chủ, Bản đồ, Địa điểm, Cài đặt
├── Views/
│   ├── LoginPage, RegisterPage, AuthPage
│   ├── MainPage.xaml         → Trang chủ (tour, gợi ý)
│   ├── MapPage.xaml          → Bản đồ POI, geofence, gửi visit
│   ├── POIListPage.xaml      → Danh sách địa điểm
│   ├── POIDetailPage.xaml    → Chi tiết POI, thuyết minh
│   ├── TourDetailPage.xaml
│   └── SettingsPage.xaml
├── Services/
│   ├── MobileAuthService.cs  → Login/Register/Token: BaseAddress = API (DEBUG: 10.0.2.2:5000 / 127.0.0.1:5000)
│   ├── ApiService.cs         → GET poi, POST analytics/visit; cùng BaseAddress
│   ├── POIService.cs         → Cache SQLite + sync từ API
│   ├── GeofenceService.cs    → Theo dõi vị trí, trigger thuyết minh + log visit
│   └── ...
└── Platforms/Android/        → Foreground service cho location
```

### 3.2 Xác thực App

- **Lưu trữ:** JWT và user session trong **SecureStorage** (TokenKey, UserKey).
- **Đăng nhập:** Gọi API `POST api/auth/login` (MobileAuthService) → lưu token + user → chuyển sang AppShell.
- **Gọi API:** ApiService và MobileAuthService dùng cùng base URL; request có `Authorization: Bearer {token}` (từ AuthService.GetToken()).

### 3.3 Base URL API (App)

- **DEBUG:** Android emulator: `http://10.0.2.2:5000/api/`; iOS simulator: `http://127.0.0.1:5000/api/`; còn lại: `http://localhost:5000/api/`.
- **Release:** `https://heristep.onrender.com/api/`.

---

## 4. API (HeriStepAI.API)

### 4.1 Endpoints chính

| Nhóm | Endpoint | Mô tả |
|------|----------|--------|
| Auth | POST api/auth/login, register-tourist | Trả JWT |
| Auth | GET api/auth/me | Profile (cần JWT) |
| POI | GET api/poi, GET api/poi/{id} | Danh sách / chi tiết POI |
| POI | POST/PUT/DELETE api/poi | CRUD (Admin, JWT) |
| Analytics | POST api/analytics/visit | Ghi lượt ghé (AllowAnonymous, app gọi) |
| Analytics | GET api/analytics/summary | Tổng lượt + Geofence (Admin) |
| Analytics | GET api/analytics/top-pois | Top POI theo lượt ghé |
| Analytics | GET api/analytics/poi/{id}/statistics, logs | Thống kê theo POI |

### 4.2 DB & Auth API

- **DB:** PostgreSQL (Supabase hoặc local); EF Core, bảng: Users, POIs, POIContents, VisitLogs, __EFMigrationsHistory.
- **Auth:** JWT Bearer; secret/issuer từ .env hoặc appsettings.

---

## 5. Flow hoạt động tổng thể

### 5.1 Web (Admin / ShopOwner)

1. User mở **localhost:5001** → redirect **/Auth/Login** nếu chưa đăng nhập.
2. Nhập email/mật khẩu → Web **POST api/auth/login** → nhận JWT → Cookie + cookie `AuthToken`.
3. Redirect theo role:
   - **Admin** → **Home/Dashboard** (gọi API analytics/summary, top-pois, poi).
   - **ShopOwner** → **ShopOwner/Dashboard** (đọc POIs + VisitLogs từ DbContext).
4. Các trang Admin (Dashboard, POI, Analytics) luôn gọi API với Bearer token từ cookie. ShopOwner dùng DbContext (cùng DB với API).

### 5.2 App Mobile (Khách du lịch)

1. Mở app → splash → **TryRestoreSession** (SecureStorage).
2. Nếu có session hợp lệ → **AppShell** (tab Trang chủ, Bản đồ, Địa điểm, Cài đặt); đồng thời **sync POI** từ API (nền).
3. Nếu không → **LoginPage**; đăng nhập/đăng ký qua API → lưu token → **AppShell**.
4. Trên **MapPage**: hiển thị POI, geofence; khi vào vùng POI → phát thuyết minh + **POST api/analytics/visit** (POId, VisitType, …).
5. **POIListPage / POIDetailPage**: dữ liệu từ cache SQLite (đã sync từ API) hoặc API.

### 5.3 Đồng bộ số liệu Dashboard vs Analytics

- **Dashboard (Web):** Tổng lượt ghé + Tự động nhận diện lấy từ **api/analytics/summary** (cùng nguồn với trang Analytics).
- **Analytics (Web):** Chi tiết từ **api/analytics/top-pois** + **api/analytics/poi/{id}/statistics**.
- **ShopOwner:** Thống kê từ **DbContext** (VisitLogs, POIs) – cùng DB với API nên dữ liệu nhất quán.

---

## 6. Luồng dữ liệu tóm tắt

```
[User Web]  →  localhost:5001  →  Cookie + JWT  →  Gọi API localhost:5000  →  [API]  →  PostgreSQL
[User App]  →  MAUI app        →  JWT (SecureStorage)  →  Gọi API (10.0.2.2 / 127.0.0.1 / Render)  →  [API]  →  PostgreSQL
[ShopOwner Web]  →  localhost:5001/ShopOwner  →  DbContext (Web process)  →  Cùng PostgreSQL
```

- **API** là nguồn chính cho Auth, POI, Analytics (visit log).
- **Web** vừa gọi API (Admin) vừa đọc DB trực tiếp (ShopOwner).
- **App** chỉ gọi API; POI cache local (SQLite) để offline/nhanh.
