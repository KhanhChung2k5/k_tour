# Flow hoạt động chi tiết – Hệ thống HeriStepAI

Tài liệu mô tả từng bước luồng hoạt động của Web (Admin/ShopOwner), App Mobile và API, kèm điều kiện rẽ nhánh và nguồn dữ liệu.

---

## 1. Tổng quan các thành phần

| Thành phần | Địa chỉ (local) | Giao thức / Auth | Vai trò |
|------------|------------------|------------------|---------|
| **Người dùng Web** | Trình duyệt → localhost:5001 | Cookie + JWT (trong cookie) | Admin: Dashboard, POI, Analytics. ShopOwner: Dashboard, Edit POI, Thống kê. |
| **Người dùng App** | Điện thoại / máy ảo | JWT (SecureStorage) | Khách du lịch: xem bản đồ, POI, thuyết minh, ghi visit. |
| **HeriStepAI.Web** | :5001 | Cookie authentication | Render MVC, gọi API (Bearer token), ShopOwner đọc DB trực tiếp. |
| **HeriStepAI.API** | :5000 | JWT Bearer | REST: auth, POI, analytics; ghi/đọc PostgreSQL. |
| **PostgreSQL** | Supabase / localhost:5432 | Connection string | Users, POIs, POIContents, VisitLogs. |
| **Supabase Storage** | (URL từ .env) | API key | Lưu ảnh POI (upload từ Web). |

---

## 2. Flow đăng nhập và phân quyền (Web)

### 2.1 Vào trang chủ khi chưa đăng nhập

1. User mở **http://localhost:5001** (hoặc https nếu cấu hình).
2. Route mặc định: **Home/Index**.
3. **HomeController.Index()** kiểm tra `User.Identity?.IsAuthenticated`.
4. Nếu **false** → `RedirectToAction("Login", "Auth")` → trình duyệt chuyển tới **/Auth/Login**.
5. **AuthController.Login() [GET]** trả về view **Login.cshtml** (form email, password).

### 2.2 Gửi form đăng nhập (Web)

1. User nhập email + mật khẩu, bấm "Đăng nhập".
2. Form **POST /Auth/Login** (action = Auth/Login, method post).
3. **AuthController.Login(string email, string password) [POST]**:
   - Tạo `HttpClient` từ factory với name **"API"** (BaseAddress = http://localhost:5000/api/).
   - Body JSON: `{ "Email": email, "Password": password }`.
   - Gửi **POST api/auth/login** tới API (có retry nếu 429).
4. **API AuthController** nhận request:
   - Kiểm tra email/password (BCrypt), tìm user trong bảng **Users**.
   - Tạo JWT (claims: Id, Email, Role) → trả JSON `{ "token": "...", "userId", "email", "fullName", "username" }`.
5. **Web AuthController** nhận response:
   - Nếu **200**: parse JWT, giải mã payload → lấy claims (Id, Email, Role, …).
   - Tạo **ClaimsIdentity** + **ClaimsPrincipal** với scheme Cookie.
   - `SignInAsync(Cookie...)` → ghi cookie xác thực (ASP.NET).
   - Ghi thêm cookie **AuthToken** = chuỗi JWT (HttpOnly, dùng cho các request gọi API sau này).
   - Kiểm tra **Role**:
     - Nếu **ShopOwner** hoặc **"2"** → `RedirectToAction("Dashboard", "ShopOwner")`.
     - Ngược lại (Admin) → `RedirectToAction("Dashboard", "Home")`.
   - Nếu **401** hoặc lỗi: trả lại view Login với `ViewBag.Error`.
6. Trình duyệt chuyển tới **/Home/Dashboard** (Admin) hoặc **/ShopOwner/Dashboard** (ShopOwner).

### 2.3 Các request sau khi đã đăng nhập (Web)

- Mỗi request tới Web (Dashboard, POI, Analytics, …) đều kèm **cookie xác thực** (ASP.NET Cookie).
- Middleware **UseAuthentication** đọc cookie → gán **User** (ClaimsPrincipal).
- Controller cần auth dùng ** [Authorize]**; có thể ** [Authorize(Roles = "Admin")]**.
- Khi Web cần **gọi API**, nó lấy JWT từ cookie **AuthToken** và gửi header **Authorization: Bearer {token}**.

---

## 3. Flow Dashboard Admin (Web)

1. User (đã đăng nhập Admin) vào **/Home/Dashboard**.
2. **HomeController.Dashboard()**:
   - Nếu role ShopOwner → redirect ShopOwner/Dashboard (đã mô tả trên).
   - Tạo HttpClient **"API"**, đính **Authorization: Bearer {AuthToken từ cookie}**.
   - Gọi đồng thời:
     - **GET api/analytics/summary** → JSON `{ TotalVisits, Geofence, MapClick, QRCode }`.
     - **GET api/analytics/top-pois?count=10** → JSON `{ "poiId": count, ... }` (top 10).
     - **GET api/poi** → JSON mảng POI (để đếm tổng, active, tên).
   - Gán **ViewBag.TotalVisits**, **ViewBag.GeofenceVisits** từ summary; **ViewBag.TopPOIs** từ top-pois; **ViewBag.TotalPOIs**, **ViewBag.ActivePOIs**, **ViewBag.POINames** từ poi.
3. **API** xử lý:
   - **analytics/summary**: AnalyticsService.GetVisitSummaryAsync() → đếm VisitLogs (total, theo VisitType) → trả JSON.
   - **analytics/top-pois**: GroupBy POId, Count, OrderByDescending, Take(10) → trả dictionary.
   - **poi**: POIService trả danh sách POI từ DB.
4. View **Dashboard.cshtml** render thẻ thống kê (tổng POI, tổng lượt ghé, tự động nhận diện, POI đang mở) và biểu đồ top 10 từ **ViewBag**.

---

## 4. Flow trang Analytics (Web)

1. User (Admin) vào **/Analytics**.
2. **AnalyticsController.Index()**:
   - **GET api/analytics/top-pois?count=1000** → tất cả POI có lượt ghé.
   - **GET api/poi** → danh sách POI (để map id → tên và gán 0 cho POI không có visit).
   - Với mỗi POI có visit > 0: **GET api/analytics/poi/{id}/statistics** → TotalVisits, VisitsByType (Geofence, MapClick, …).
   - Cộng tổng **totalGeofence**, **totalManual** từ các statistics → **ViewBag.TotalVisits**, **ViewBag.TotalGeofence**, **ViewBag.TotalManual**, **ViewBag.AllPOIs**, **ViewBag.GeofenceByPOI**, **ViewBag.ManualByPOI**.
3. View **Analytics/Index.cshtml** hiển thị thẻ tổng, donut phân loại, bar chart theo địa điểm.

---

## 5. Flow quản lý POI – Admin (Web)

1. **Danh sách**: **/POI** → POIController.Index() → **GET api/poi** (Bearer) → view Index với danh sách.
2. **Tạo mới**: **/POI/Create** (GET form) → POST /POI/Create:
   - Web gửi dữ liệu POI (tên, mô tả, tọa độ, …) tới **API** (POST api/poi).
   - Ảnh (nếu có): upload qua **SupabaseStorageService** (Supabase Storage), lấy URL → gửi ImageUrl trong body API.
   - API lưu POI vào DB.
3. **Sửa / Xem**: **/POI/Edit/{id}**, **/POI/Details/{id}** → gọi **GET api/poi/{id}**, hiển thị form hoặc chi tiết.

---

## 6. Flow ShopOwner (Web) – dùng DB trực tiếp

1. User đăng nhập với role **ShopOwner** → redirect **/ShopOwner/Dashboard**.
2. **ShopOwnerController** nhận **ApplicationDbContext** (EF Core, connection string cùng với API).
3. **Dashboard**:
   - Lấy **userId** từ claim (NameIdentifier).
   - Query **POIs** where OwnerId == userId, include **Contents**.
   - Query **VisitLogs** where POId thuộc danh sách POI của user.
   - GroupBy POId → TotalVisits, UniqueVisitors, LastVisit, GeofenceVisits, ManualVisits.
   - Trả view với ViewBag.TotalPOIs, TotalVisits, ActivePOIs và danh sách POI kèm thống kê.
4. **Edit / Statistics**: Đọc/sửa **POIs**, **POIContents**, **VisitLogs** qua DbContext (không gọi API). Upload ảnh vẫn qua SupabaseStorageService.

---

## 7. Flow App Mobile – Khởi động và đăng nhập

1. User mở app (MAUI).
2. **App** constructor:
   - Hiển thị splash (ContentPage màu amber).
   - Đọc **Preferences "has_session"** (đặt khi đăng nhập thành công lần trước).
   - Gọi **InitializeAsync(authService)** (chạy bất đồng bộ).
3. **InitializeAsync**:
   - **MobileAuthService.TryRestoreSessionAsync()**:
     - Đọc **SecureStorage** TokenKey, UserKey.
     - Nếu thiếu hoặc token quá ngắn → return false.
     - Deserialize UserSession, gán **CurrentUser**, set **Authorization** header cho HttpClient.
     - (Tùy chọn) gọi **GET api/auth/me** để refresh tên/email.
   - Nếu **has_session && session restored** → MainPage = **AppShell** (tab: Trang chủ, Bản đồ, Địa điểm, Cài đặt).
   - Ngược lại → MainPage = **LoginPage** (có thể xóa "has_session" nếu session restore thất bại).
4. **Đăng nhập trong app**:
   - User nhập email/password trên LoginPage → **MobileAuthService.LoginAsync()**.
   - **POST api/auth/login** (BaseAddress từ GetBaseUrl(): DEBUG = 10.0.2.2:5000 / 127.0.0.1:5000, Release = Render).
   - Nhận JSON token + user → **SaveSessionAsync**: ghi SecureStorage (TokenKey, UserKey), Preferences "has_session", set Bearer header.
   - UI chuyển sang **AppShell**.
5. **Đồng thời**: **POIService.SyncPOIsFromServerAsync()** chạy nền (ApiService.GetAllPOIsAsync() → lưu SQLite local) để có dữ liệu POI cho bản đồ và danh sách.

---

## 8. Flow App Mobile – Bản đồ và ghi visit

1. User mở tab **Bản đồ** (MapPage).
2. **MapPage** load POI từ **POIService** (cache SQLite đã sync từ API) hoặc gọi API.
3. **GeofenceService** (hoặc logic tương đương) theo dõi vị trí (GPS).
4. Khi user **vào vùng** một POI (trong bán kính):
   - Trigger phát **thuyết minh** (TTS hoặc audio từ POIContent).
   - Gọi **ApiService.LogVisitAsync(poiId, userId, lat, lon, VisitType.Geofence)**:
     - **POST api/analytics/visit** với body `{ POId, UserId, Latitude, Longitude, VisitType }`.
     - Header **Authorization: Bearer {token}** nếu đã đăng nhập (UserId có thể null nếu anonymous).
5. **API AnalyticsController.LogVisit** [AllowAnonymous]:
   - Fire-and-forget: trả **202 Accepted** ngay, ghi DB trong background (LogVisitAsync → VisitLogs).
   - Tránh app phải chờ DB (tránh timeout).

---

## 9. Flow tổng hợp dữ liệu (Ai ghi / Ai đọc)

| Dữ liệu | Ghi bởi | Đọc bởi |
|---------|---------|---------|
| **Users** | API (register, seed) | API (login, auth/me) |
| **POIs, POIContents** | API (Admin CRUD), Web ShopOwner (Edit qua DbContext) | API (GET poi), Web (Admin qua API; ShopOwner qua DbContext), App (API → cache SQLite) |
| **VisitLogs** | API (POST analytics/visit từ App/Web) | API (summary, top-pois, statistics), Web ShopOwner (DbContext) |
| **Ảnh POI** | Web (SupabaseStorageService) | Hiển thị qua URL (Supabase Storage) |

---

## 10. Trình tự theo thời gian (ví dụ Admin dùng Web)

1. User mở **localhost:5001** → redirect **/Auth/Login**.
2. GET /Auth/Login → trả form.
3. POST /Auth/Login (email, password) → Web gọi POST api/auth/login → API kiểm tra Users, trả JWT.
4. Web ghi Cookie + AuthToken, redirect GET /Home/Dashboard.
5. GET /Home/Dashboard → Web gọi GET api/analytics/summary, GET api/analytics/top-pois?count=10, GET api/poi (kèm Bearer token) → render Dashboard.
6. User click Analytics → GET /Analytics → Web gọi api/analytics/top-pois?count=1000, api/poi, nhiều lần api/analytics/poi/{id}/statistics → render Analytics.
7. User click Quản lý POI → GET /POI → Web gọi GET api/poi → render danh sách. Tạo/sửa POI → POST/PUT api/poi; ảnh upload Supabase, URL gửi trong body.

---

## 11. Ghi chú cho sơ đồ draw.io

- **Actor:** Người dùng Web (Admin), Người dùng Web (ShopOwner), Người dùng App (Khách).
- **Hệ thống:** Web (:5001), API (:5000), PostgreSQL, Supabase Storage.
- **Luồng chính:**  
  - Trình duyệt → Web → API (auth, poi, analytics) → PostgreSQL.  
  - Trình duyệt → Web (ShopOwner) → PostgreSQL (DbContext).  
  - App → API (auth, poi, analytics/visit) → PostgreSQL.  
  - Web (upload ảnh) → Supabase Storage.
- **Auth:** Web dùng Cookie + JWT trong cookie; App dùng JWT trong SecureStorage; API chỉ kiểm tra Bearer JWT.

---

## 12. Import file vào draw.io để vẽ sơ đồ

1. Mở **draw.io** (https://app.diagrams.net/ hoặc desktop).
2. **File → Open from → Device** (hoặc **Open**), chọn file **`docs/system-flow-drawio.drawio`**.
3. File chứa sẵn:
   - **Actor:** Người dùng Web (Admin), Người dùng Web (ShopOwner), Người dùng App (Khách).
   - **Hệ thống:** HeriStepAI.Web (:5001), HeriStepAI.API (:5000), PostgreSQL, Supabase Storage, khối App.
   - **Mũi tên:** Luồng đăng nhập/request Web→API, Web→DB (ShopOwner), Web→Storage, App→API, API→DB; có nhãn ngắn gọn.
   - **Chú thích:** Ô ghi chú ở dưới cùng.
4. Bạn có thể kéo thả, thêm shape, đổi màu, tách/ghép luồng theo từng use case trong tài liệu flow chi tiết ở trên.
