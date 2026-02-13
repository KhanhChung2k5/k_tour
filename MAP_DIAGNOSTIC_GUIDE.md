# Map Diagnostic Guide

## Changes Made

I've added comprehensive diagnostics to help identify why the map is not loading:

### 1. **JavaScript Console Capture**
   - Added `WebChromeClient` to capture all JavaScript console messages
   - All `console.log()`, `console.error()`, and `console.warn()` from the map will now appear in debug output
   - Location: [MapPage.xaml.cs:87-105](src/HeriStepAI.Mobile/Views/MapPage.xaml.cs#L87-L105)

### 2. **Detailed Initialization Logging**
   - Added step-by-step logging throughout the entire map initialization process:
     - ✅ WebView configured
     - ✅ HTML generated (with character count)
     - ✅ HTML preview (first 500 chars)
     - ✅ BaseURL loading
     - ✅ Map object created
     - ✅ Tile layers created
     - ✅ Tiles added to map
     - ✅ Current location marker added
     - ✅ POI markers added
     - ✅ Loading overlay hidden

### 3. **Visual Loading Indicator**
   - Added a spinner overlay that shows "Đang tải bản đồ..." while the map loads
   - The overlay automatically hides after 1 second once initialization completes
   - If there's an error, the overlay shows a user-friendly error message

### 4. **Enhanced Error Handling**
   - Wrapped entire JavaScript in try-catch block
   - Errors display:
     - In the debug console (with full stack trace)
     - On the map UI (user-friendly message)
   - Tile loading errors are captured and logged
   - Automatic fallback from OpenStreetMap to CartoDB if OSM fails

## How to Test

### Step 1: Clean and Rebuild
```bash
# Navigate to the Mobile project
cd "src/HeriStepAI.Mobile"

# Clean the project
dotnet clean

# Rebuild
dotnet build
```

### Step 2: Deploy to Android Device/Emulator
```bash
# Deploy and run on Android
dotnet build -t:Run -f net8.0-android
```

### Step 3: View Debug Output
While the app is running, monitor the debug output in Visual Studio Output window or use ADB logcat:

```bash
# Using ADB to view debug logs
adb logcat | findstr "MapPage\|JS Console\|MapWebViewClient"
```

### Step 4: Navigate to Map Page
1. Open the app
2. Navigate to the Map page (usually the map icon in bottom navigation)
3. Watch for the loading spinner

## What to Look For

### ✅ Success Indicators
You should see logs like this:
```
[MapPage] 🗺️ Generating map HTML for X POIs
[MapPage] ✅ HTML generated (XXXXX chars)
[MapPage] ✅ WebView configured for Android
[MapPage] ✅ Loading map HTML with BaseURL
[MapWebViewClient] ✅ Page loaded: https://heristepai.app/
[JS Console] ℹ️ LOG: [Map] Starting map initialization...
[JS Console] ℹ️ LOG: [Map] Leaflet version: 1.9.4
[JS Console] ℹ️ LOG: [Map] ✅ Map object created
[JS Console] ℹ️ LOG: [Map] ✅ Tile layers created
[JS Console] ℹ️ LOG: [Map] ✅ OSM tiles added to map
[JS Console] ℹ️ LOG: [Map] ✅ OSM tile loaded successfully
[JS Console] ℹ️ LOG: [Map] ✅ All POI markers added
[JS Console] ℹ️ LOG: [Map] ✅ Loading overlay hidden - map ready!
```

### ❌ Error Indicators
If you see errors, they will help us identify the problem:

**Leaflet Not Loading:**
```
[JS Console] ❌ ERROR: L is not defined
[JS Console] ❌ ERROR: [Map] Leaflet version: NOT LOADED
```
→ CDN blocked or unreachable

**Tile Loading Errors:**
```
[JS Console] ❌ ERROR: [Map] OSM tile error
[JS Console] ℹ️ LOG: [Map] Switching to CartoDB...
```
→ OpenStreetMap blocked, but CartoDB should work

**Critical JavaScript Error:**
```
[JS Console] ❌ ERROR: [Map] ❌ CRITICAL ERROR during initialization
```
→ JavaScript syntax or runtime error

**Resource Loading Blocked:**
```
[MapWebViewClient] ❌ Error loading resource: https://unpkg.com/leaflet@1.9.4/dist/leaflet.js
```
→ Network/CORS issue

## Possible Issues and Solutions

### Issue 1: Leaflet CDN Blocked
**Symptoms:** `L is not defined`, `NOT LOADED`

**Solution:** Add Leaflet files locally instead of CDN
- Download Leaflet CSS and JS
- Embed directly in HTML string

### Issue 2: Tiles Not Loading
**Symptoms:** Map appears gray, tile errors in console

**Solution:**
- Check if CartoDB fallback works
- Try alternative tile servers (Stamen, Mapbox, etc.)

### Issue 3: HTML Not Generating
**Symptoms:** No HTML preview in logs, or very short character count

**Solution:**
- Check if POIs are being loaded from API
- Verify ViewModel has data

### Issue 4: WebView Handler Not Ready
**Symptoms:** `⚠️ Handler not ready, retrying in 500ms...`

**Solution:**
- Increase delay in OnAppearing
- Check MAUI WebView handler initialization

## Next Steps

After running the app:

1. **Copy all debug output** from the moment you navigate to the map page
2. **Take a screenshot** of what you see on screen (spinner, error, or blank)
3. **Share both** so I can identify the exact issue

The comprehensive logging will tell us exactly where the map initialization is failing.

## Alternative Approaches (If Current Approach Fails)

If the diagnostics reveal fundamental issues with WebView + Leaflet, we can consider:

1. **MAUI Community Toolkit Maps** - Native map controls
2. **Microsoft.Maui.Controls.Maps** - Official MAUI maps
3. **Mapsui** - .NET native map library
4. **Google Maps SDK** - Native Google Maps
5. **Offline tiles** - Bundle map tiles with the app

These alternatives are documented in [MAP_ALTERNATIVES.md](MAP_ALTERNATIVES.md).
