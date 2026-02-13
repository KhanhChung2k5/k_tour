# Các Lựa chọn Bản đồ Miễn phí cho HeriStepAI

## ✅ Đã chọn: Leaflet + OpenStreetMap

Ứng dụng hiện tại sử dụng **Leaflet + OpenStreetMap** - giải pháp hoàn toàn miễn phí và không cần API key.

### Ưu điểm:
- ✅ **Hoàn toàn miễn phí** - Không có giới hạn request
- ✅ **Không cần API key** - Sử dụng ngay không cần đăng ký
- ✅ **Open source** - Mã nguồn mở, cộng đồng lớn
- ✅ **Đầy đủ tính năng**: Markers, Popups, Click events, Zoom, Pan
- ✅ **Nhẹ và nhanh** - Leaflet là thư viện nhẹ nhất
- ✅ **Tương thích tốt** - Hoạt động tốt với WebView trong MAUI

### Cách sử dụng:
Không cần cấu hình gì! Code đã được tích hợp sẵn trong `MapPage.xaml.cs`.

## Các lựa chọn khác (nếu cần)

### 1. Google Maps JavaScript API
- **Free tier**: $200 credit/tháng (khoảng 28,000 requests)
- **Cần**: API key từ Google Cloud Console
- **Ưu điểm**: Chất lượng bản đồ tốt, nhiều tính năng
- **Nhược điểm**: Có giới hạn free, cần thanh toán khi vượt quá

### 2. MapTiler
- **Free tier**: 100,000 requests/tháng
- **Cần**: API key
- **Ưu điểm**: Bản đồ đẹp, có nhiều style
- **Nhược điểm**: Có giới hạn free

### 3. Here Maps
- **Free tier**: 250,000 requests/tháng
- **Cần**: API key
- **Ưu điểm**: Tốt cho navigation
- **Nhược điểm**: Cần đăng ký và verify

### 4. Mapbox (đã loại bỏ)
- **Free tier**: 50,000 requests/tháng
- **Cần**: Access token
- **Nhược điểm**: Giới hạn thấp, tốn phí khi vượt quá

## So sánh

| Tính năng | OpenStreetMap | Google Maps | MapTiler | Here Maps |
|-----------|---------------|-------------|----------|-----------|
| **Miễn phí** | ✅ Vô hạn | ⚠️ $200/tháng | ⚠️ 100k/tháng | ⚠️ 250k/tháng |
| **API Key** | ❌ Không cần | ✅ Cần | ✅ Cần | ✅ Cần |
| **Chất lượng** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Tính năng** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Dễ sử dụng** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ |

## Kết luận

**Leaflet + OpenStreetMap** là lựa chọn tốt nhất cho dự án này vì:
1. Hoàn toàn miễn phí, không có giới hạn
2. Không cần đăng ký hay API key
3. Đủ tính năng cho nhu cầu hiện tại
4. Dễ tích hợp và maintain

Nếu trong tương lai cần tính năng nâng cao hơn (như 3D maps, indoor maps, etc.), có thể cân nhắc chuyển sang Google Maps hoặc MapTiler.
