# So sánh Flow hiện tại với Yêu cầu

## Luồng hoạt động (đã áp dụng)

| Bước | Yêu cầu | Hiện trạng |
|------|---------|------------|
| 1 | App tải danh sách POI (lat/lng, bán kính, ưu tiên, nội dung) | ✅ POIService sync từ API, lưu SQLite offline |
| 2 | Background service cập nhật vị trí khi user di chuyển | ✅ LocationService polling 5s (foreground) |
| 3 | Geofence Engine: POI gần nhất/ưu tiên trong bán kính → gửi sự kiện | ✅ Haversine, sắp xếp theo Priority, cooldown 5 phút |
| 4 | Narration Engine: kiểm tra đang phát? đã phát X phút? → quyết định phát | ✅ Per-POI cooldown 5 phút, tự dừng khi có POI mới |
| 5 | Ghi log đã phát, tránh lặp | ✅ _lastPlayedAt dictionary trong NarrationService |

## Tính năng đã tinh chỉnh

### 1. GPS Tracking
- **Tối ưu pin**: GetCurrentLocationAsync dùng `GeolocationAccuracy.Medium` mặc định
- **Foreground**: LocationService polling mỗi 5 giây
- *Lưu ý*: Background tracking cần Android Foreground Service (chưa triển khai)

### 2. Geofence
- POI: lat/lng, Radius, Priority ✅
- Tự động phát khi vào vùng ✅
- Cooldown 5 phút chống spam ✅
- Haversine formula ✅

### 3. Narration (Thuyết minh tự động)
- **TTS**: TextToSpeech.Default (native Android/iOS) ✅
- **Audio file**: TODO – cần MediaManager
- **Hàng đợi**: Queue, không phát trùng ✅
- **Tự dừng khi có POI mới**: Stop + clear queue ✅
- **Per-POI cooldown**: Không phát lại POI trong 5 phút ✅

### 4. Map View
- Hiển thị vị trí user ✅
- Hiển thị tất cả POI ✅
- **Highlight POI gần nhất** (marker xanh, viền vàng) ✅
- Xem chi tiết POI (popup + nút) ✅

### 5. Quản lý dữ liệu POI
- Danh sách POI, mô tả, ảnh (ImageUrl), MapLink ✅
- TextContent (TTS) + AudioUrl ✅

### 6. Chưa triển khai
- **QR Code kích hoạt**: Quét → nghe ngay, không cần GPS
- **Tour management**: Chọn tour, nhiều tuyến
- **Giọng thuyết minh**: Miền Bắc/Miền Nam (cần locale khác)
- **Analytics**: Heatmap, thời gian trung bình nghe
- **Background location**: Foreground Service Android
