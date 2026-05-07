# HeriStepAI — Tài liệu hoạt động chi tiết từng chức năng

> **Mục đích:** Tài liệu này giải thích cụ thể từng chức năng trong hệ thống HeriStepAI hoạt động như thế nào, dùng công nghệ gì, dữ liệu chạy qua đâu. Được viết để trả lời câu hỏi báo cáo dự án.

---

## Tổng quan kiến trúc hệ thống

```
┌─────────────────────────────────────────────────────────┐
│                   HeriStepAI System                     │
│                                                         │
│  ┌──────────────┐   REST API    ┌──────────────────┐   │
│  │  Mobile App  │◄─────────────►│  HeriStepAI.API  │   │
│  │  (MAUI/C#)   │               │  (.NET 8 / JWT)  │   │
│  └──────────────┘               └────────┬─────────┘   │
│                                          │ EF Core      │
│  ┌──────────────┐   DbContext   ┌────────▼─────────┐   │
│  │ HeriStepAI   │◄─────────────►│   PostgreSQL DB  │   │
│  │   .Web MVC   │               │  (Supabase host) │   │
│  └──────────────┘               └──────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

**3 thành phần chính:**
| Thành phần | Công nghệ | Vai trò |
|---|---|---|
| `HeriStepAI.Mobile` | .NET MAUI / C# | App dành cho khách du lịch (Android/iOS) |
| `HeriStepAI.API` | ASP.NET Core 8 / JWT | REST API, xác thực, analytics |
| `HeriStepAI.Web` | ASP.NET Core MVC | Web quản trị (Admin & ShopOwner) |

---

## Phần 1 — Mobile App (HeriStepAI.Mobile)

### 1.1 Khởi động ứng dụng

**Khi người dùng bấm mở app:**

1. `App.xaml.cs` khởi tạo `ResponsiveHelper` (tính kích thước màn hình để co giãn UI)
2. Set `MainPage = AppShell` — thanh tab 4 mục (Home, Map, Places, Settings) hiện ra
3. **Background task:** Ngay lập tức gọi `POIService.SyncPOIsFromServerAsync()` để cập nhật dữ liệu điểm du lịch từ server về SQLite local

**Dependency Injection (MauiProgram.cs):**
- Toàn bộ services được đăng ký vào DI container khi app khởi động
- **Singleton** (dùng chung toàn app): `AuthService`, `LocationSimulator`, `POIService`, `NarrationService`, `GeofenceService`, `ApiService`, `TourGeneratorService`, `LocalizationService`, `Analytics`
- **Transient** (tạo mới mỗi lần): các ViewModels, Pages

```
MauiProgram.cs → builder.Services.AddSingleton<IPOIService, POIService>()
                                  .AddSingleton<INarrationService, NarrationService>()
                                  ...
```

---

### 1.2 Xác thực — Đăng nhập / Đăng ký

**Màn hình:** `AuthPage.xaml` + `AuthPageViewModel.cs`

**Cách hoạt động — Đăng nhập:**
1. User nhập email + password
2. `AuthPageViewModel` validate phía client (không để trống, email đúng định dạng)
3. Gọi `AuthService.LoginAsync(email, password)`
4. `AuthService` gửi `POST https://heristep.onrender.com/api/auth/login` với body JSON
5. Server trả về `{ token, userId, username, email, fullName }`
6. App lưu JWT vào `SecureStorage` (Android Keystore / iOS Keychain — không thể đọc được bởi app khác)
7. Lưu `has_session = true` vào `Preferences` để lần sau không cần đăng nhập lại
8. Điều hướng sang `AppShell` (màn chính)

**Cách hoạt động — Khôi phục phiên (mở lại app):**
1. `App.xaml.cs` đọc `has_session` từ Preferences
2. Nếu `true` → load JWT từ SecureStorage → không hiện màn login
3. Nếu thiếu thông tin profile → gọi `GET /api/auth/me` ở background để refresh

**Đăng ký:**
- Gửi `POST /api/auth/register-tourist`
- Trả về response tương tự nhưng **KHÔNG tự đăng nhập** → user phải đăng nhập lại
- Lý do: bảo mật, tránh auto-login với session chưa xác nhận

**Công nghệ:** `HttpClient`, `SecureStorage` (MAUI), `Preferences` (MAUI), JSON serialization

---

### 1.3 Đồng bộ dữ liệu POI (Điểm tham quan)

**POI = Point of Interest** là các địa điểm du lịch, quán ăn... có tọa độ GPS

**Cách hoạt động:**
```
App khởi động
    ↓
POIService.SyncPOIsFromServerAsync()
    ↓
ApiService: GET /api/poi (kèm Bearer JWT)
    ↓
├─ Thành công → xóa SQLite cũ → lưu POIs + POIContents mới
└─ Thất bại/null → giữ nguyên cache SQLite (offline-first)
```

**Cơ sở dữ liệu local:**
- File: `pois.db` trong `FileSystem.AppDataDirectory` của thiết bị
- Bảng `POI`: id, name, description, lat, lng, radius, category, price...
- Bảng `POIContent`: ngôn ngữ + nội dung thuyết minh cho từng POI
- Library: `sqlite-net-pcl`

**Retry logic (ApiService):**
- Thử tối đa **3 lần**, mỗi lần cách nhau 2 giây
- Timeout mỗi request: **45 giây** (vì server dùng Render free tier, có thể cold start)
- Nếu cả 3 lần đều thất bại → dùng dữ liệu SQLite cũ

**Model POI gồm:**
```csharp
Id, Name, Description, Latitude, Longitude, Address, Radius
ImageUrl, Rating, ReviewCount, Category (enum), TourId?
EstimatedMinutes, FoodType, PriceMin, PriceMax (VND)
Contents: List<POIContent> // nội dung theo ngôn ngữ
```

---

### 1.4 GPS & Theo dõi vị trí

**Service:** `LocationService.cs` + `LocationForegroundService.cs` (Android)

**Cách hoạt động:**
1. Xin quyền `LocationWhenInUse` (foreground)
2. Trên Android: xin thêm `LocationAlways` (background)
3. Vòng lặp polling mỗi **5 giây**: `Geolocation.GetLocationAsync(accuracy: Medium)`
4. Mỗi lần có vị trí mới → bắn event `LocationChanged`

**Accuracy Mode:**
- `GeolocationAccuracy.Medium` (mặc định) — sai số ~50-500m, tiết kiệm pin
- `GeolocationAccuracy.Best` (khi cần) — sai số ~10m, tốn pin hơn

**Background (Android):**
- `LocationForegroundService` chạy như Android Foreground Service
- Hiển thị notification "HeriStepAI đang hoạt động" → OS không kill service
- `StartCommandResult.Sticky` → tự restart nếu bị kill

**Công nghệ:** `Microsoft.Maui.Devices.Sensors.Geolocation`, Android Service API

---

### 1.5 Geofence — Phát hiện vào vùng POI

**Service:** `GeofenceService.cs`

**Đây là tính năng cốt lõi của app:** khi user bước vào vòng tròn quanh một POI → tự động kích hoạt thuyết minh.

**Thuật toán Haversine:**
```
distance = 2R × arcsin(√(sin²(Δlat/2) + cos(lat1)×cos(lat2)×sin²(Δlon/2)))
```
Tính khoảng cách thực (mét) giữa 2 tọa độ GPS trên bề mặt Trái Đất.

**Logic xử lý mỗi lần cập nhật GPS:**
```
Nhận tọa độ mới
    ↓
Tính khoảng cách tới TẤT CẢ POIs
    ↓
Tìm POI gần nhất trong bán kính của nó (min radius = 50m)
    ↓
├─ Không có POI nào → reset currentPOI
│
├─ Cùng POI đang ở → bỏ qua (không re-trigger)
│
└─ POI mới
    ├─ Còn cooldown 5 phút? → bỏ qua
    └─ Hết cooldown → TRIGGER!
           ├─ Cập nhật currentPOI
           ├─ Ghi timestamp cooldown
           ├─ Bắn event POIEntered
           └─ → Narration bắt đầu
```

**Tại sao "closest POI wins"?**
- Khi 2 POI overlap vùng geofence → user đang ở giữa → chọn POI gần hơn
- Tránh ngẫu nhiên trigger POI sai

**Cooldown 5 phút:** Ngăn việc đứng tại chỗ → trigger thuyết minh liên tục

---

### 1.6 Thuyết minh tự động (TTS)

**Service:** `NarrationService.cs`

**Cách hoạt động khi geofence trigger:**
```
POIEntered event
    ↓
NarrationService.PlayNarrationAsync(poi, language, forcePlay=false)
    ↓
Tìm nội dung theo ngôn ngữ:
  1. POIContent với Language = currentLang → TextContent
  2. Fallback → Vietnamese content
  3. Fallback → content đầu tiên có trong db
  4. Fallback → POI.Description
    ↓
TextToSpeech.SpeakAsync(text, locale)
```

**Chọn giọng đọc (Voice Selection):**
1. Lấy danh sách giọng từ OS: `TextToSpeech.GetLocalesAsync()`
2. Lọc theo ngôn ngữ (vi/en/ko/zh/ja/th/fr)
3. Lọc thêm theo giới tính (Male/Female) trong tên giọng
4. Nếu không tìm được giọng phù hợp → dùng giọng đầu tiên của ngôn ngữ đó

**Queue vs Force:**
- **Auto (geofence):** thêm vào queue, đợi narration hiện tại kết thúc
- **Manual (user bấm Listen):** `forcePlay=true` → hủy queue + dừng TTS hiện tại → phát ngay

---

#### Chống spam thuyết minh — 3 lớp bảo vệ độc lập

App có **3 lớp chống spam** hoạt động theo thứ tự từ ngoài vào trong:

**Lớp 1 — Cooldown Geofence (GeofenceService, sớm nhất)**
```csharp
private readonly TimeSpan _cooldownPeriod = TimeSpan.FromMinutes(5);

if (_poiCooldowns.TryGetValue(poi.Id, out var lastTime)
    && DateTime.UtcNow - lastTime < _cooldownPeriod)
    return null; // event POIEntered không được bắn ra
```
- Chặn ngay tại nguồn — NarrationService còn không được gọi
- Reset sau 5 phút hoặc khi user rời khỏi rồi quay lại POI

**Lớp 2 — Queue Deduplication (NarrationService)**
```csharp
if (_currentPOI?.Id == poi.Id) return;       // đang phát POI này rồi
if (_queue.Any(p => p.Id == poi.Id)) return; // đã có trong hàng đợi rồi
```
- Không cho cùng 1 POI xuất hiện 2 lần trong queue
- Không re-trigger POI đang phát dở

**Lớp 3 — Cooldown Narration (NarrationService, sâu nhất)**
```csharp
private readonly TimeSpan _poiCooldown = TimeSpan.FromMinutes(5);

if (_lastPlayedAt.TryGetValue(poi.Id, out var last)
    && DateTime.UtcNow - last < _poiCooldown)
    return; // bỏ qua, không thêm vào queue
```
- Ghi timestamp sau mỗi lần phát xong
- 5 phút tiếp theo: mọi trigger đều bị bỏ qua

**Pipeline đầy đủ:**
```
GPS poll mỗi 5 giây
    ↓
GeofenceService.CheckGeofence()
    ├─ Cùng POI đang ở trong?       → skip (Lớp 1a)
    ├─ Cooldown geofence 5 phút?    → skip (Lớp 1b)
    └─ OK → bắn event POIEntered
                ↓
         NarrationService.PlayNarrationAsync(forcePlay=false)
                ├─ Cooldown narration 5 phút? → skip (Lớp 3)
                ├─ Đang phát POI này?         → skip (Lớp 2a)
                ├─ Đã có trong queue?         → skip (Lớp 2b)
                └─ OK → thêm vào queue → phát tuần tự
```

> **Kết quả:** Dù đứng yên tại 1 POI cả ngày, thuyết minh chỉ phát **tối đa 1 lần mỗi 5 phút**. Dù 2 POI overlap geofence, chỉ POI gần nhất được trigger.

**Công nghệ:** `Microsoft.Maui.Media.TextToSpeech`, native TTS engine của Android/iOS

---

### 1.7 Bản đồ (Map)

**View:** `MapPage.xaml` + `MapPage.xaml.cs`
**ViewModel:** `MapPageViewModel.cs`

**Cách hoạt động:**

App **KHÔNG dùng** SDK bản đồ native (Google Maps SDK, Apple Maps SDK). Thay vào đó:

```
MAUI WebView (HTML container)
    ↑
    │  LoadDataWithBaseURL(html_string)
    │
Leaflet.js + OpenStreetMap tiles
(thư viện JavaScript bản đồ open-source)
```

**Tại sao dùng WebView + Leaflet?**
- Không cần API key Google Maps (tốn phí)
- OpenStreetMap miễn phí, không giới hạn request
- Leaflet.js nhẹ, đầy đủ tính năng: zoom, pan, popup, markers

**Android WebView config (MapPage.xaml.cs):**
```csharp
webView.Settings.JavaScriptEnabled = true;
webView.Settings.DomStorageEnabled = true;
webView.Settings.SetGeolocationEnabled(true);
webView.Settings.MixedContentMode = MixedContentHandling.AlwaysAllow;
webView.Settings.UserAgentString = "Mozilla/5.0 ... Chrome/...";
// User-Agent giả Chrome để OpenStreetMap không block
```

**Markers trên bản đồ:**
- 📍 **POI marker:** icon cam (teardrop)
- 🔵 **Vị trí hiện tại:** vòng tròn xanh với shadow
- 💚 **Nearest POI:** icon xanh lá + viền vàng + hiệu ứng pulse
- ⭕ **Geofence circle:** vòng tròn đứt nét xung quanh POI

**Communication JS ↔ C#:**
- C# → JS: `EvaluateJavaScriptAsync("map.panTo([lat, lng])")`
  (cập nhật marker vị trí realtime không cần reload map)
- JS → C#: URL scheme `poi://select?id=123`
  `WebViewClient.ShouldOverrideUrlLoading()` intercept → parse POI id → trigger C# code

**Khi chọn Tour:**
- `TourSelectionService.SelectedTour` được set
- `MapPage.LoadPOIsAsync()` chỉ hiện POI trong tour → lọc ra bản đồ

**Bottom Sheet (panel kéo lên):**
- Drag gesture: kéo ≥ 30% chiều cao → expand; kéo xuống → collapse
- Auto-mở khi POI được chọn
- Hiển thị: tên, mô tả, địa chỉ, giá, nút Navigate

---

### 1.8 Danh sách POI (Places)

**View:** `POIListPage.xaml` + `POIListPageViewModel.cs`

**Cách hoạt động:**
1. Load POIs từ SQLite → hiển thị danh sách
2. Tính khoảng cách từ vị trí hiện tại đến từng POI (Haversine)
3. Sắp xếp theo khoảng cách gần nhất

**Tìm kiếm realtime:**
- Search box binding `SearchQuery`
- Filter: `poi.Name.Contains(query) || poi.Description.Contains(query) || poi.Address.Contains(query)`
- Cập nhật list ngay khi gõ (không cần bấm nút)

**Filter theo danh mục:**
- Chips: Tất cả / Tham quan / Ẩm thực / Nghỉ dưỡng / Mua sắm / Giải trí / Di tích / Thiên nhiên
- Chọn category → filter lại ObservableCollection

**Pull-to-refresh:**
- Kéo xuống → `SyncPOIsFromServerAsync()` → reload list

---

### 1.9 Chi tiết POI (POI Detail)

**View:** `POIDetailPage.xaml` + `POIDetailViewModel.cs`

**Nhận dữ liệu:** Query parameter `POI` từ navigation (pass object)

**Mô tả đa ngôn ngữ (`LocalizedDescription`):**
```csharp
// Tìm content theo ngôn ngữ đang chọn
var content = poi.Contents.FirstOrDefault(c => c.Language == currentLang);
if (content != null) return content.TextContent;
// Fallback về tiếng Việt
return poi.Contents.FirstOrDefault(c => c.Language == "vi")?.TextContent
    ?? poi.Description;
```

**Nút Listen:**
- `PlayNarrationAsync(poi, language, forcePlay: true)` → phát TTS ngay lập tức

**Nút Directions:**
- Gọi `Map.Default.OpenAsync(location)` → mở app bản đồ native (Google Maps / Apple Maps)

---

### 1.10 Tour tự động (Auto-generated Tours)

**Service:** `TourGeneratorService.cs`

**App không lưu Tour trong database** — Tour được tạo **on-the-fly** từ dữ liệu POI hiện có.

**Các loại tour được tạo:**

| Tour | Điều kiện | Nội dung |
|---|---|---|
| **Hải sản 🦞** | ≥ 2 POI FoodType=Seafood | Chỉ POI hải sản |
| **Món chay 🥗** | ≥ 2 POI FoodType=Vegetarian | Chỉ POI chay |
| **Ăn nhanh ⚡** | ≥ 3 POI EstimatedMinutes ≤ 30 | POIs thăm nhanh |
| **Giá rẻ 💰** | ≥ 2 POI PriceMin < 50,000đ | POI bình dân |
| **Cao cấp 👑** | ≥ 2 POI PriceMin ≥ 150,000đ | POI cao cấp |
| **Top rated ⭐** | ≥ 3 POI Rating ≥ 4.5 | POI được đánh giá cao |
| **Đường phố 🍜** | ≥ 2 POI FoodType=Street | Street food |
| ...+ các loại khác | theo FoodType enum | Nhóm theo loại ẩm thực |

**Thông tin tổng hợp Tour:**
```csharp
EstimatedMinutes = sum(poi.EstimatedMinutes) của tất cả POI trong tour
PriceMin = min(poi.PriceMin), PriceMax = max(poi.PriceMax)
Rating = average(poi.Rating)
ReviewCount = sum(poi.ReviewCount)
```

**Tour card trên MainPage:** Hiển thị tên, mô tả, số POI, thời gian ước tính, khoảng giá

---

### 1.11 Bắt đầu Tour

**Flow:**
```
TourDetailPage: bấm "Bắt đầu Tour"
    ↓
TourSelectionService.SelectedTour = tour
    ↓
Shell.GoToAsync("//MapPage")
    ↓
MapPage.LoadPOIsAsync()
    ├─ Có SelectedTour? → chỉ load POI của tour
    └─ Không? → load tất cả POI
```

**`TourSelectionService`** là singleton đơn giản, chỉ lưu 1 property:
```csharp
public Tour? SelectedTour { get; set; }
```

---

### 1.12 Giả lập vị trí (Location Simulator)

**Service:** `LocationSimulatorService.cs`
**Dùng để:** Test chức năng geofence + narration mà không cần đi thực địa

**Cách hoạt động:**
```
User bấm "🧪 Test"
    ↓
Lấy danh sách POIs hiện tại làm lộ trình
    ↓
Vòng lặp:
  1. Phát event LocationChanged với tọa độ POI hiện tại
  2. Chờ NarrationService báo "đã xong" (AdvanceToNext)
  3. Hoặc timeout 90 giây
  4. Delay 2s
  5. Chuyển sang POI tiếp theo
    ↓
Khi hết POI → SimulationCompleted → tự dừng
```

**Trên bản đồ:** Marker di chuyển realtime đến từng POI theo lộ trình

---

### 1.13 Analytics & Thống kê

**Service:** `LocalAnalyticsService.cs`

**Không upload lên server** — tất cả lưu local bằng `Preferences` (key-value storage của MAUI).

**Dữ liệu theo dõi:**

| Chỉ số | Key | Mô tả |
|---|---|---|
| Quán đã ghé | `a_shops` | Tổng lượt POI đã visit |
| Quãng đường | `a_dist` | Tổng mét đã đi (bộ lọc: bỏ qua jump > 500m) |
| Tour hoàn thành | `a_tours` | Số tour đã kết thúc |
| Nghe thuyết minh | `a_narr` | Số lần TTS phát |
| Hoạt động tuần | `a_day_0`..`a_day_6` | Số visit mỗi ngày trong tuần (reset mỗi thứ 2) |
| Top POIs | `a_top_pois` | JSON danh sách top 10 POI được ghé nhiều nhất |

**Biểu đồ thanh tuần (Bar chart):**
- Chiều cao bar = (visit ngày đó / max visit trong tuần) × 55px
- Ngày hôm nay tô màu Accent (vàng), ngày khác màu Primary (nâu)
- Không dùng thư viện chart — tự build bằng `BoxView` có `HeightRequest` dynamic

**Top POIs:** Serialize/Deserialize JSON từ Preferences, giữ top 10, sort khi đọc

---

### 1.14 Đa ngôn ngữ (Localization)

**Service:** `LocalizationService.cs`

**7 ngôn ngữ hỗ trợ:** Tiếng Việt, English, 한국어, 中文, 日本語, ภาษาไทย, Français

**Cách hoạt động:**
```csharp
// Từ điển tĩnh trong code (không dùng resource file)
Dictionary<string, Dictionary<string, string>> Translations = {
    ["AppTitle"] = { ["vi"]="HERISTEP AI", ["en"]="HERISTEP AI", ... },
    ["CatFood"]  = { ["vi"]="Ẩm thực", ["en"]="Food", ["ko"]="음식", ... },
    ...
}

// Lấy text theo ngôn ngữ hiện tại
public string GetString(string key) {
    return Translations[key][_currentLanguage]
        ?? Translations[key]["vi"]  // fallback Vietnamese
        ?? key;                     // fallback key name
}
```

**Khi đổi ngôn ngữ:**
1. `LocalizationService.SetLanguage("en")`
2. Bắn `LanguageChanged` event
3. Tất cả ViewModel đăng ký event → gọi `OnPropertyChanged(nameof(Lbl...))` cho từng label
4. UI tự cập nhật thông qua data binding

**Lưu trữ:** `Preferences.Set("AppLanguage", "en")` — giữ sau khi tắt app

---

### 1.15 Cài đặt (Settings)

**View:** `SettingsPage.xaml` + `SettingsPageViewModel.cs`

**Các cài đặt:**
1. **Thông báo thuyết minh:** Toggle (UI-only, chưa implement service)
2. **Ngôn ngữ:** Picker → gọi `LocalizationService.SetLanguage()` → toàn app đổi ngôn ngữ ngay
3. **Âm lượng & Giọng đọc:** Picker Nam/Nữ → gọi `VoicePreferenceService.SaveVoiceGender()` → NarrationService dùng khi TTS

**Section Profile (mock/gamification):**
- Tên: "Khách" (localized: "Guest")
- Cấp độ, huy hiệu: hardcoded placeholder (chưa connect backend)
- Progress bar: fixed 72% (placeholder)

**GPS Status:** Đọc từ `LocationService.IsLocationEnabled` — hiển thị ON/OFF

**Đồng bộ dữ liệu:** Nút "Sync Data" → `POIService.SyncPOIsFromServerAsync()` → hiện loading + thông báo kết quả

---

## Phần 2 — API Backend (HeriStepAI.API)

### 2.1 Xác thực (Authentication)

**Controller:** `AuthController.cs`
**Endpoint:** `POST /api/auth/login`, `POST /api/auth/register-tourist`, `GET /api/auth/me`

**Flow đăng nhập:**
```
POST /api/auth/login { email, password }
    ↓
Tìm User trong PostgreSQL
    ↓
BCrypt.Verify(password, hashedPassword) ← kiểm tra hash
    ↓
Tạo JWT với claims: userId, email, role
    ↓
Return { token, userId, username, email, fullName }
```

**JWT Claims:**
- `userId`, `email`, `role` (1=Admin, 2=ShopOwner, 3=Tourist)
- Ký bằng secret key trong `.env`
- TTL: cấu hình qua environment variable

**BCrypt:** Mật khẩu không lưu plaintext — chỉ lưu hash. Verify bằng `BCrypt.Net-Next`

---

### 2.2 Quản lý POI (API)

**Controller:** `POIController.cs`

| Endpoint | Phương thức | Auth | Mô tả |
|---|---|---|---|
| `GET /api/poi` | GET | Tourist+ | Lấy tất cả POI active |
| `GET /api/poi/{id}` | GET | Tourist+ | Chi tiết 1 POI + Contents |
| `POST /api/poi` | POST | Admin | Tạo POI mới |
| `PUT /api/poi/{id}` | PUT | Admin/Owner | Sửa POI |
| `DELETE /api/poi/{id}` | DELETE | Admin | Xóa POI |
| `GET /api/poi/{id}/content/{lang}` | GET | Any | Nội dung theo ngôn ngữ |
| `GET /api/poi/my-pois` | GET | ShopOwner | POI của owner |

**Upload ảnh:** Từ Web Admin → `SupabaseStorageService` → Supabase Storage bucket → URL lưu vào `POI.ImageUrl`

**ORM:** Entity Framework Core với Npgsql (PostgreSQL provider)

---

### 2.3 Analytics (API)

**Endpoint:** `POST /api/analytics/visit`

**Body:**
```json
{
  "poiId": 5,
  "userId": "123",
  "latitude": 16.0544,
  "longitude": 108.2022,
  "visitType": 1
}
```
`visitType`: 1=Geofence, 2=MapClick, 3=QRCode

**Database:** Insert vào bảng `VisitLogs`
```sql
INSERT INTO VisitLogs (POId, UserId, VisitTime, Latitude, Longitude, VisitType)
VALUES (@poiId, @userId, NOW(), @lat, @lng, @type)
```

**Các endpoint analytics khác:**
- `GET /api/analytics/summary` → tổng visits, phân loại theo VisitType
- `GET /api/analytics/top-pois?count=10` → top POI nhiều visit nhất (GROUP BY)
- `GET /api/poi/{id}/statistics` → thống kê visit theo từng POI

---

## Phần 3 — Web Admin (HeriStepAI.Web)

### 3.1 Đăng nhập Web

**Pattern:** Cookie Authentication + JWT Bearer

```
Browser → POST /Auth/Login (form)
    ↓
Web Controller: gọi POST /api/auth/login
    ↓
Nhận JWT → lưu vào Cookie "AuthToken"
    ↓
Admin (Role=1) → redirect /Home/Dashboard
ShopOwner (Role=2) → redirect /ShopOwner/Dashboard
```

Mọi request tiếp theo: Web đọc JWT từ cookie → gắn vào `Authorization: Bearer` header khi gọi API

### 3.2 Admin Dashboard

**Controller:** `HomeController.cs`

Gọi **3 API song song** (Task.WhenAll) để load nhanh:
- `GET /api/analytics/summary` → tổng quan visits
- `GET /api/analytics/top-pois?count=10` → top 10 POI
- `GET /api/poi` → danh sách POI

### 3.3 POI CRUD (Admin)

**Controller:** `POIController.cs` (Web)

Mọi CRUD đều proxy qua API:
```csharp
// Web POI controller
var response = await _httpClient.PostAsync("api/poi", content);
// _httpClient là HttpClient có base URL = API server
```

**Tạo POI:** có thể đồng thời tạo ShopOwner mới hoặc gán owner hiện có

### 3.4 ShopOwner Dashboard

**Khác biệt:** ShopOwner **truy cập thẳng PostgreSQL** qua DbContext (không qua API)

```csharp
// ShopOwner controller dùng DbContext trực tiếp
var poi = await _context.POIs.FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == currentUserId);
```

Lý do: ShopOwner chỉ xem/sửa dữ liệu của mình → đơn giản hóa flow

---

## Phần 4 — Database Schema

**Công nghệ:** PostgreSQL (host trên Supabase)

```sql
Users         id, email, password_hash, full_name, role, ...
POIs          id, name, description, lat, lng, radius, category, ...
              owner_id (FK Users), tour_id?, price_min, price_max
POIContents   id, po_id (FK POIs), language, text_content, audio_url
VisitLogs     id, po_id, user_id, visit_time, lat, lng, visit_type
```

**Không dùng bảng `Analytics`** — bảng này là legacy, tất cả analytics tính từ `VisitLogs`

---

## Phần 5 — Deployment

| Thành phần | Hosting | URL |
|---|---|---|
| API | Render.com (free tier) | `https://heristep.onrender.com` |
| Database | Supabase PostgreSQL | cloud managed |
| File Storage | Supabase Storage | bucket cho ảnh POI |
| Mobile App | APK sideload / Debug build | Android |

**Lưu ý Render free tier:**
- Server "ngủ" sau 15 phút không có request
- Cold start: 20-30 giây lần đầu → app có retry 3 lần × 45s timeout để handle

---

## Phần 6 — Câu hỏi thường gặp khi báo cáo

**Q: App có cần internet không?**
> Có thể dùng offline một phần. POI đã sync → hiển thị được. Narration vẫn hoạt động (TTS local). Nhưng cần internet để: đăng nhập lần đầu, sync dữ liệu mới, ghi visit log.

**Q: Làm sao biết user đang ở đâu?**
> GPS (MAUI Geolocation API), poll mỗi 5 giây. Dùng thuật toán Haversine để tính khoảng cách đến POI.

**Q: Thuyết minh hoạt động thế nào?**
> Khi user bước vào bán kính của POI (mặc định 50m), GeofenceService trigger → NarrationService lấy nội dung từ POIContent theo ngôn ngữ đang chọn → phát TTS qua engine của thiết bị.

**Q: Data có được bảo mật không?**
> JWT token lưu trong SecureStorage (Android Keystore / iOS Keychain) — encrypted by OS. Password hash bằng BCrypt. Server HTTPS.

**Q: Bản đồ dùng gì?**
> Leaflet.js + OpenStreetMap, chạy trong WebView. Không dùng Google Maps (tốn phí, cần API key).

**Q: Multi-language hoạt động thế nào?**
> Từ điển tĩnh trong LocalizationService (7 ngôn ngữ). Đổi ngôn ngữ → event → tất cả UI label refresh qua data binding. Nội dung thuyết minh (tiếng Anh, tiếng Hàn...) cần Admin nhập vào POIContent.

**Q: Analytics lưu ở đâu?**
> Có 2 lớp: (1) Local trên thiết bị dùng Preferences (không cần internet), (2) Server VisitLogs trong PostgreSQL (khi có internet, fire-and-forget).

**Q: Tour được quản lý thế nào?**
> Tour được **tạo động** từ POI data — không lưu tour vào database. TourGeneratorService gom POI theo category/giá/rating → tạo tour. Admin không cần quản lý tour thủ công.

**Q: Simulator để làm gì?**
> Tính năng test cho developer: giả lập đi qua các POI theo thứ tự, trigger geofence + narration + analytics mà không cần di chuyển ngoài thực địa.

---

## Tóm tắt công nghệ sử dụng

| Hạng mục | Công nghệ |
|---|---|
| **Mobile framework** | .NET MAUI (C#) — cross-platform Android/iOS |
| **MVVM pattern** | CommunityToolkit.Mvvm (source generators) |
| **Local database** | SQLite-net-pcl |
| **Secure storage** | MAUI SecureStorage (Android Keystore) |
| **Maps** | Leaflet.js + OpenStreetMap (trong WebView) |
| **Text-to-Speech** | MAUI TextToSpeech API (native TTS engine) |
| **GPS** | MAUI Geolocation API |
| **Networking** | HttpClient (.NET) + JSON deserialization |
| **Backend** | ASP.NET Core 8 Web API |
| **ORM** | Entity Framework Core + Npgsql |
| **Database** | PostgreSQL (Supabase) |
| **Authentication** | JWT Bearer + BCrypt |
| **File storage** | Supabase Storage |
| **Web Admin** | ASP.NET Core MVC + Razor Views |
| **Hosting** | Render.com (API) + Supabase (DB + Storage) |
