# 📱 Test Mode Guide - Hướng dẫn Test Geofencing

## 🎯 Mục đích Test Mode

Test Mode được thiết kế để **test tính năng geofencing và tự động thuyết minh** mà KHÔNG CẦN di chuyển thực tế giữa các quán ăn. Điều này cực kỳ hữu ích khi:
- Đang phát triển trên Android Emulator (không thể di chuyển real-time)
- Các quán ở xa nhau hoặc khó tiếp cận
- Muốn test nhanh flow thuyết minh tự động

## 🔧 Cách Test Mode hoạt động

### 1. **LocationSimulatorService**
```csharp
// Khi bật Test Mode
_simulator.StartSimulation(route, delaySeconds: 10);
```

- **Input**: Danh sách 5 quán đầu tiên trong tour
- **Mỗi 10 giây**: Emit 1 event LocationChanged với tọa độ của quán tiếp theo
- **Vòng lặp**: Khi hết danh sách, quay lại quán đầu tiên

### 2. **LocationService nhận Simulated Locations**
```csharp
// LocationService.cs
public async Task<Location?> GetCurrentLocationAsync(...)
{
    // Ưu tiên location giả lập nếu đang test
    if (_simulator.IsSimulating && _lastSimulatedLocation != null)
    {
        return _lastSimulatedLocation;
    }
    // Nếu không, dùng GPS thật
    return await Geolocation.GetLocationAsync(...);
}
```

### 3. **GeofenceService kiểm tra khoảng cách**
```csharp
public POI? CheckGeofence(Location currentLocation)
{
    foreach (var poi in _pois)
    {
        var distance = HaversineDistance(...);
        if (distance <= poi.Radius)  // Mặc định 100m
        {
            return poi;  // Trigger auto narration
        }
    }
    return null;
}
```

### 4. **Auto Narration với Anti-Spam**
```csharp
// NarrationService.cs
public async Task PlayNarrationAsync(POI poi, ...)
{
    // Không phát lại nếu đã phát trong vòng 5 phút
    if (_lastPlayedAt.TryGetValue(poi.Id, out var lastTime)
        && DateTime.UtcNow - lastTime < TimeSpan.FromMinutes(5))
        return;

    // Tự dừng phát hiện tại khi có POI mới
    if (_isPlaying)
    {
        _playCts?.Cancel();
        _narrationQueue.Clear();
    }

    // Phát thuyết minh
    await PlayNarration(...);
}
```

## 🧪 Cách Test trên Android Emulator

### **Option 1: Sử dụng Test Mode (Khuyến nghị)**

1. **Mở MapPage**
2. **Click nút "🧪 Bật Test Mode"** (góc dưới bên phải)
3. **Quan sát log**:
   ```
   🧪 Test Mode started with 5 POIs
   🚶 Simulated location 1/5: Quán A (16.054407, 108.202167)
   🎵 Auto narration triggered for Quán A
   [Sau 10 giây]
   🚶 Simulated location 2/5: Quán B (16.055123, 108.203456)
   🛑 Stopped previous narration (new POI detected)
   🎵 Auto narration triggered for Quán B
   ```

4. **Kết quả mong đợi**:
   - ✅ Mỗi 10 giây, ứng dụng tự động "nhảy" đến quán tiếp theo
   - ✅ Thuyết minh tự động phát khi "đến" quán
   - ✅ Thuyết minh cũ tự động dừng khi có quán mới
   - ✅ KHÔNG phát lại quán đã thuyết minh trong 5 phút

### **Option 2: Set Location thủ công trên Emulator**

1. **Mở Emulator Extended Controls**: `Ctrl + Shift + E` (hoặc `⌘ + Shift + E` trên Mac)
2. **Chọn tab "Location"**
3. **Nhập tọa độ quán**:
   ```
   Quán A: 16.054407, 108.202167
   Quán B: 16.055123, 108.203456
   ```
4. **Click "Send"** để cập nhật GPS

**Lưu ý**: Option này chỉ test được 1 lần, không thể tự động loop như Test Mode.

## ⚠️ Xử lý các quán ở gần nhau (Overlapping POIs)

### **Vấn đề**:
Các quán ở cạnh nhau (cách < 200m) → Geofencing có thể trigger nhiều quán cùng lúc

### **Giải pháp đã implement**:

#### 1. **Priority-based Triggering**
```csharp
// GeofenceService.cs
public POI? CheckGeofence(Location currentLocation)
{
    var triggered = _pois
        .Where(p => Distance(currentLocation, p) <= p.Radius)
        .OrderBy(p => Distance(currentLocation, p))  // Gần nhất
        .ThenByDescending(p => p.Priority)            // Ưu tiên cao nhất
        .FirstOrDefault();

    return triggered;
}
```
**Kết quả**: Chỉ phát quán **GẦN NHẤT** và có **ưu tiên cao nhất**

#### 2. **Anti-Spam Cooldown (5 phút)**
```csharp
// NarrationService.cs
private readonly Dictionary<int, DateTime> _lastPlayedAt = new();
private readonly TimeSpan _poiCooldown = TimeSpan.FromMinutes(5);
```
**Kết quả**: Không phát lại quán đã thuyết minh trong 5 phút

#### 3. **Auto-Stop khi có POI mới**
```csharp
// NarrationService.cs
if (_isPlaying || _narrationQueue.Count > 0)
{
    _playCts?.Cancel();  // Dừng ngay lập tức
    _narrationQueue.Clear();  // Xóa hàng đợi
}
```
**Kết quả**: Thuyết minh cũ dừng ngay khi phát hiện quán mới

#### 4. **Radius tùy chỉnh theo từng POI**
```csharp
// POI.cs
public double Radius { get; set; }  // Mặc định 100m

// Với các quán ở khu đông, giảm radius
Quán A: Radius = 50m   // Khu chật
Quán B: Radius = 100m  // Bình thường
Quán C: Radius = 150m  // Khu rộng
```

### **Ví dụ thực tế**:

**Scenario**: 3 quán ở Hòa Khánh (cách nhau 100-200m)

```
User di chuyển theo route:
┌────────────────────────────────────────┐
│ 15:00 - Đến gần Quán A (50m)          │
│ ✅ Phát thuyết minh Quán A             │
├────────────────────────────────────────┤
│ 15:10 - Di chuyển về phía Quán B      │
│ ⏭️ Quán A vẫn trong cooldown (5 phút)  │
│ ❌ KHÔNG phát lại Quán A               │
├────────────────────────────────────────┤
│ 15:15 - Đến Quán B (60m)               │
│ ✅ Phát thuyết minh Quán B             │
│ 🛑 Dừng thuyết minh Quán A (nếu đang) │
├────────────────────────────────────────┤
│ 15:20 - Đi qua Quán A lần 2            │
│ ⏭️ Quán A vẫn trong cooldown           │
│ ❌ KHÔNG phát lại                      │
└────────────────────────────────────────┘
```

## 📊 Monitoring & Debugging

### **Debug Logs**
```csharp
// Bật debug log trong AppSettings
AppLog.Info($"🧪 Test Mode started");
AppLog.Info($"🚶 Simulated location: {poi.Name}");
AppLog.Info($"📍 Geofence triggered for: {poi.Name}");
AppLog.Info($"🎵 Auto narration: {poi.Name}");
AppLog.Info($"⏭️ Skipped (cooldown): {poi.Name}");
```

### **UI Indicators**
- **Test Mode button**: `🧪 Bật Test Mode` → `🛑 Tắt Test Mode`
- **Location updates**: Xem current location trên map
- **Narration status**: IsPlaying property

## 🎯 Best Practices cho Test Mode

1. **Test với ít quán trước** (2-3 quán) để verify logic
2. **Kiểm tra cooldown** bằng cách chờ 5 phút
3. **Test overlap** với các quán gần nhau (<200m)
4. **Verify audio stop** khi chuyển quán
5. **Test loop** để đảm bảo không memory leak

## 🚀 Tính năng nâng cao (Future)

### **Real-time Emulator GPS Control**
```csharp
// Thay vì set location tĩnh, gọi ADB command
public class EmulatorGPSController
{
    public async Task SetLocation(double lat, double lng)
    {
        await Shell.ExecuteAsync($"adb emu geo fix {lng} {lat}");
    }
}
```

### **GPX Route Import**
```csharp
// Import GPX file từ Google Maps hoặc OpenStreetMap
public class GPXRouteSimulator
{
    public async Task SimulateGPXRoute(string gpxFilePath)
    {
        var waypoints = await GPXParser.Parse(gpxFilePath);
        foreach (var point in waypoints)
        {
            LocationChanged?.Invoke(this, point);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
```

---

## 📝 Tóm tắt

**Test Mode giải quyết 3 vấn đề chính:**

1. ✅ **Không cần di chuyển thực tế** → Simulator tự động "nhảy" giữa các quán
2. ✅ **Xử lý quán gần nhau** → Priority + Cooldown + Auto-Stop
3. ✅ **Không phát trùng** → Cooldown 5 phút per POI

**Cách dùng đơn giản nhất**:
1. Mở MapPage
2. Click "🧪 Bật Test Mode"
3. Quan sát thuyết minh tự động phát mỗi 10 giây
4. Click "🛑 Tắt Test Mode" khi xong
