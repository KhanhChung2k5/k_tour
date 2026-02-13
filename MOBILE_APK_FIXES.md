# 📱 Mobile APK Fixes - Giải quyết vấn đề khi build APK

Tài liệu này tóm tắt các fix đã thực hiện để giải quyết các vấn đề khi build APK và cài đặt trên máy thật.

## 📋 Danh sách vấn đề đã fix

### ✅ 1. Map không hiển thị trên APK release

**Vấn đề**: Trên emulator map hiển thị bình thường, nhưng khi build APK và cài trên điện thoại thật thì map không load được.

**Nguyên nhân**:
- Thiếu `network_security_config.xml` để allow HTTPS traffic và cleartext cho map tiles
- Code shrinking (ProGuard/R8) có thể strip WebView code khi build release

**Giải pháp đã áp dụng**:

1. **Tạo network security config** tại `Platforms/Android/Resources/xml/network_security_config.xml`:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <network-security-config>
       <base-config cleartextTrafficPermitted="true">
           <trust-anchors>
               <certificates src="system" />
               <certificates src="user" />
           </trust-anchors>
       </base-config>
       <!-- Allow HTTPS domains for OpenStreetMap, Supabase, etc. -->
   </network-security-config>
   ```

2. **Update AndroidManifest.xml** để reference network config:
   ```xml
   <application ... android:networkSecurityConfig="@xml/network_security_config">
   ```

3. **Disable code shrinking** trong `HeriStepAI.Mobile.csproj`:
   ```xml
   <AndroidLinkMode>None</AndroidLinkMode>
   <PublishTrimmed>false</PublishTrimmed>
   ```

**Kết quả**: Map sẽ hiển thị đầy đủ trên APK release khi cài trên máy thật.

---

### ✅ 2. Layout không responsive (mất cân đối trên các màn hình khác nhau)

**Vấn đề**: Layout hiển thị đẹp trên Pixel 8 emulator nhưng trên máy thật bị lệch, thiếu chữ, không cân đối.

**Nguyên nhân**:
- Hardcoded padding values (ví dụ: `Padding="14,44,14,10"`)
- Hardcoded font sizes không adaptive theo màn hình
- Status bar height khác nhau giữa các thiết bị

**Giải pháp đã áp dụng**:

1. **Tạo ResponsiveHelper class** tại `Helpers/ResponsiveHelper.cs`:
   - Tự động detect screen size và calculate scale factor
   - Dynamic status bar height detection cho Android
   - Helper methods: `FontSize()`, `Spacing()`, `HeaderPadding()`, `ContentPadding()`

2. **Initialize ResponsiveHelper** trong `App.xaml.cs`:
   ```csharp
   ResponsiveHelper.Initialize();
   ```

3. **Update tất cả Views** để sử dụng responsive padding:
   - [MapPage.xaml.cs](src/HeriStepAI.Mobile/Views/MapPage.xaml.cs:20)
   - [POIListPage.xaml.cs](src/HeriStepAI.Mobile/Views/POIListPage.xaml.cs:14)
   - [SettingsPage.xaml.cs](src/HeriStepAI.Mobile/Views/SettingsPage.xaml.cs:14)
   - [MainPage.xaml.cs](src/HeriStepAI.Mobile/Views/MainPage.xaml.cs:15)

   Example:
   ```csharp
   TopBar.Padding = ResponsiveHelper.HeaderPadding();
   ```

**Kết quả**: Layout sẽ tự động adapt theo screen size và status bar height của từng thiết bị.

---

### ✅ 3. GPS real-time + Auto narration khi đi ngang POI

**Vấn đề**: Cần đảm bảo GPS tracking hoạt động khi app ở background và tự động thuyết minh khi đi ngang POI.

**Trạng thái hiện tại**:
✅ LocationService đã có polling mỗi 5 giây
✅ GeofenceService đã có logic check proximity và cooldown 5 phút
✅ MapPageViewModel đã subscribe events và trigger auto-narration

**Cải thiện đã áp dụng**:

1. **Tạo Foreground Service** cho Android tại `Platforms/Android/Services/LocationForegroundService.cs`:
   - Keep app alive khi ở background
   - Hiển thị notification "HeriStepAI đang hoạt động"
   - Prevent Android từ kill app khi doze mode

2. **Tạo cross-platform manager** tại `Services/LocationForegroundServiceManager.cs`:
   - Start/Stop foreground service
   - Platform-specific implementation

3. **Update AndroidManifest.xml** với permissions cần thiết:
   ```xml
   <uses-permission android:name="android.permission.FOREGROUND_SERVICE" />
   <uses-permission android:name="android.permission.FOREGROUND_SERVICE_LOCATION" />
   <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
   ```

4. **Auto-start foreground service** trong `MapPageViewModel`:
   ```csharp
   LocationForegroundServiceManager.Start();
   ```

**Logic hoạt động**:
1. Khi mở MapPage → Start location updates + foreground service
2. LocationService poll GPS mỗi 5 giây (hoặc real-time nếu available)
3. Mỗi location update → GeofenceService check proximity
4. Khi vào geofence POI → Trigger auto-narration với cooldown 5 phút
5. Narration service queue POI và play tuần tự

**Kết quả**: GPS tracking hoạt động liên tục ngay cả khi app ở background, tự động phát thuyết minh khi đi ngang POI.

---

### ✅ 4. SQLite offline mode - Sync data và lưu trữ local

**Vấn đề**: Cần đảm bảo lần đầu tải app sẽ sync data từ Supabase về SQLite, và lần sau không có mạng vẫn hiển thị được POI + thuyết minh.

**Trạng thái hiện tại**:
✅ POIService đã có SQLite database
✅ SyncPOIsFromServerAsync() đã có logic sync
✅ Khi API fail/empty, giữ nguyên local data

**Cải thiện đã áp dụng**:

1. **Auto-sync khi app start** trong `App.xaml.cs`:
   ```csharp
   Task.Run(async () =>
   {
       var poiService = serviceProvider.GetService<IPOIService>();
       await poiService.SyncPOIsFromServerAsync();
   });
   ```

2. **Logic offline-first**:
   - Lần đầu mở app: Sync từ Supabase → Save to SQLite
   - Lần sau: Load from SQLite first
   - Có mạng: Background sync để update data mới
   - Không mạng: Vẫn hiển thị data từ SQLite

**Hiện trạng Narration audio**:
⚠️ **Audio files chưa được cache locally** - Hiện tại có 2 loại content:
- **TTS (Text-to-Speech)**: ✅ Hoạt động offline vì chỉ cần text
- **Audio files (AudioUrl)**: ❌ Chưa download và cache local

**TODO để hoàn thiện offline mode**:
- [ ] Implement audio file download service
- [ ] Cache audio files to local storage khi sync POIs
- [ ] Fallback to TTS nếu audio file không có

**Kết quả hiện tại**:
- ✅ POI data hoạt động offline
- ✅ TTS narration hoạt động offline
- ⚠️ Pre-recorded audio chưa hoạt động offline (cần implement caching)

---

## 🔧 Các file đã thay đổi

### Android Platform
- `Platforms/Android/AndroidManifest.xml` - Added permissions và network config
- `Platforms/Android/Resources/xml/network_security_config.xml` - **NEW** Network security config
- `Platforms/Android/Services/LocationForegroundService.cs` - **NEW** Foreground service

### Core Services
- `Services/LocationForegroundServiceManager.cs` - **NEW** Cross-platform service manager
- `Helpers/ResponsiveHelper.cs` - **NEW** Responsive layout helper

### Views
- `Views/MapPage.xaml` - Removed hardcoded padding
- `Views/MapPage.xaml.cs` - Apply responsive padding
- `Views/POIListPage.xaml` - Removed hardcoded padding
- `Views/POIListPage.xaml.cs` - Apply responsive padding
- `Views/SettingsPage.xaml` - Removed hardcoded padding
- `Views/SettingsPage.xaml.cs` - Apply responsive padding
- `Views/MainPage.xaml` - Removed hardcoded padding
- `Views/MainPage.xaml.cs` - Apply responsive padding

### ViewModels
- `ViewModels/MapPageViewModel.cs` - Start foreground service

### App Initialization
- `App.xaml.cs` - Initialize ResponsiveHelper + auto-sync POIs

### Project Configuration
- `HeriStepAI.Mobile.csproj` - Disable code shrinking

---

## 📝 Checklist build APK

Khi build APK release, đảm bảo:

- [x] Network security config đã được include
- [x] AndroidManifest có đủ permissions
- [x] Code shrinking đã disabled
- [x] ResponsiveHelper được initialize
- [x] Foreground service được registered
- [x] POI sync được trigger khi app start

---

## 🧪 Test trên máy thật

### Test Map
1. Cài APK trên máy thật
2. Mở app → Navigate to Map page
3. Kiểm tra map có load được không
4. Kiểm tra markers POI có hiển thị không
5. Test zoom in/out

### Test Responsive Layout
1. Test trên các màn hình khác nhau (small/large)
2. Kiểm tra padding có phù hợp không
3. Kiểm tra text có bị cắt không
4. Rotate màn hình kiểm tra landscape mode

### Test GPS + Auto Narration
1. Enable GPS trên máy thật
2. Mở MapPage
3. Kiểm tra notification "HeriStepAI đang hoạt động" có xuất hiện không
4. Di chuyển gần POI (trong radius 50m-200m)
5. Kiểm tra có tự động phát thuyết minh không
6. Minimize app → kiểm tra background tracking
7. Test cooldown 5 phút (không phát lại POI quá sớm)

### Test Offline Mode
1. Lần đầu: Mở app với WiFi/4G → Wait for sync
2. Tắt WiFi/4G
3. Kill app và mở lại
4. Kiểm tra POI list vẫn hiển thị
5. Test TTS narration (nên work)
6. Test audio file narration (có thể không work - chưa implement cache)

---

## 🚀 Build APK command

```bash
cd "c:\Users\Lenovo\Desktop\Project\HK2 - III\C#\doan\src\HeriStepAI.Mobile"

# Build Release APK
dotnet publish -f net8.0-android -c Release

# APK location:
# bin\Release\net8.0-android\publish\com.companyname.heristepai-Signed.apk
```

---

## 📚 Tài liệu liên quan

- [MOBILE_APP_GUIDE.md](MOBILE_APP_GUIDE.md) - Hướng dẫn tổng quan mobile app
- [TEST_MODE_GUIDE.md](TEST_MODE_GUIDE.md) - Hướng dẫn test mode
- [ANDROID_EMULATOR_TROUBLESHOOTING.md](ANDROID_EMULATOR_TROUBLESHOOTING.md) - Troubleshooting emulator

---

**Cập nhật**: 2026-02-13
**Trạng thái**: ✅ Ready for testing
