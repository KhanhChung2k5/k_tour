# SQLite và luồng dữ liệu trong HeriStepAI Mobile

## SQLite là gì?

**SQLite** là một thư viện database nhúng (embedded database) dạng file:

- **Không cần server riêng** – chạy trực tiếp trong app
- **Mỗi DB là 1 file** – ví dụ: `pois.db`
- **Nhẹ, nhanh** – phù hợp cho mobile
- **SQL chuẩn** – dùng câu lệnh SQL quen thuộc

Trong app, SQLite lưu dữ liệu **trên thiết bị** (bộ nhớ trong), không cần internet để đọc/ghi.

---

## SQLite được dùng như thế nào trong app?

### 1. Vị trí file database

```
FileSystem.AppDataDirectory/pois.db
```

- Trên Android: `/data/data/com.companyname.heristepai/files/pois.db`
- Thư mục này là riêng tư, chỉ app truy cập được.

### 2. Các bảng

| Bảng       | Mô tả              |
|-----------|--------------------|
| `POI`     | Danh sách địa điểm |
| `POIContent` | Nội dung thuyết minh (theo ngôn ngữ) |

### 3. Luồng dữ liệu (Data flow)

```
┌─────────────┐      HTTP GET /api/poi      ┌──────────────┐
│   API       │  ◄───────────────────────   │  ApiService  │
│  (Server)   │                              │              │
│  PostgreSQL │  ────────────────────────►  │  Parse JSON  │
└─────────────┘         JSON response       └──────┬───────┘
                                                   │
                                                   ▼
┌─────────────┐     SyncPOIsFromServerAsync  ┌──────────────┐
│  SQLite     │  ◄────────────────────────  │  POIService  │
│  pois.db    │   Insert/Replace POI,       │              │
│  (Local)    │   POIContent                └──────┬───────┘
└──────┬──────┘                                    │
       │                                           │
       │ GetAllPOIsAsync()                         │
       ▼                                           │
┌─────────────┐     LoadDataAsync()          ┌─────┴───────┐
│ POIListPage │  ◄────────────────────────  │POIListViewModel│
│  (UI)       │   FilteredPOIs              └──────────────┘
└─────────────┘
```

**Tóm tắt:**

1. **Sync**: `SyncPOIsFromServerAsync()` gọi API → nhận JSON → ghi vào SQLite (thay toàn bộ dữ liệu local).
2. **Hiển thị**: `GetAllPOIsAsync()` đọc từ SQLite → trả về `List<POI>` → UI bind vào `FilteredPOIs`.

### 4. Điều kiện để có địa điểm trên màn hình

1. **API đang chạy** tại `http://10.0.2.2:5000` (emulator) hoặc `http://<IP-máy>:5000` (thiết bị thật).
2. **Sync thành công** – API trả về 200 và danh sách POI (không rỗng, không lỗi).
3. **Có dữ liệu trong SQLite** – sau sync, `GetAllPOIsAsync()` phải trả về ít nhất 1 POI.

---

## Kiểm tra nhanh

Chạy logcat với tag HeriStepAI:

```powershell
adb logcat -s HeriStepAI:*
```

Sau khi mở tab **Địa điểm** và kéo xuống refresh, mong đợi:

- `ApiService GET poi -> OK` (hoặc mã lỗi)
- `ApiService Deserialized X POIs` (X > 0 nếu sync thành công)
- hoặc `POIService Sync skipped: API returned null` nếu API lỗi / không kết nối được.
