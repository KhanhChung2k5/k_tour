# 📱 Cải Tiến Mobile App - HeriStepAI

## 🎯 Các Yêu Cầu Cải Tiến

### 1️⃣ Giọng Đọc - Chỉ Giữ Nam/Nữ
**Hiện trạng**: Không có voice gender settings
**Yêu cầu**: Thêm chọn giọng Nam hoặc Nữ (loại bỏ Bắc, Trung, Nam)

---

### 2️⃣ Tour Theo FoodType & Price Range
**Hiện trạng**: Tour templates cố định
**Yêu cầu**: Sắp xếp POIs theo FoodType và PriceRange để tạo tour thông minh

---

### 3️⃣ Cải Thiện UI
**Hiện trạng**: UI cơ bản
**Yêu cầu**: Làm đẹp UI, hiện đại hơn

---

### 4️⃣ Test Geofencing Không Cần Di Chuyển
**Hiện trạng**: Phải di chuyển thực tế để test
**Yêu cầu**: Mock location để test thuyết minh tự động

---

## 🔧 Chi Tiết Triển Khai

### 1. Voice Gender Settings

#### **Bước 1: Thêm Voice Preference Service**

**File mới: `Services/VoicePreferenceService.cs`**
```csharp
namespace HeriStepAI.Mobile.Services;

public enum VoiceGender
{
    Male,
    Female
}

public interface IVoicePreferenceService
{
    VoiceGender VoiceGender { get; set; }
    void SaveVoiceGender(VoiceGender gender);
    VoiceGender LoadVoiceGender();
}

public class VoicePreferenceService : IVoicePreferenceService
{
    private const string VoiceGenderKey = "voice_gender";

    public VoiceGender VoiceGender { get; set; } = VoiceGender.Female;

    public VoicePreferenceService()
    {
        VoiceGender = LoadVoiceGender();
    }

    public void SaveVoiceGender(VoiceGender gender)
    {
        VoiceGender = gender;
        Preferences.Set(VoiceGenderKey, (int)gender);
    }

    public VoiceGender LoadVoiceGender()
    {
        var saved = Preferences.Get(VoiceGenderKey, (int)VoiceGender.Female);
        return (VoiceGender)saved;
    }
}
```

#### **Bước 2: Update NarrationService**

**File: `Services/NarrationService.cs`**
```csharp
// Thêm dependency
private readonly IVoicePreferenceService _voicePreference;

public NarrationService(IVoicePreferenceService voicePreference)
{
    _voicePreference = voicePreference;
}

// Update SpeakTextAsync
private async Task SpeakTextAsync(string text, string language, CancellationToken ct = default)
{
    try
    {
        var locales = await TextToSpeech.Default.GetLocalesAsync();

        // Chọn locale dựa trên language và gender
        Locale? locale = null;

        if (language == "vi")
        {
            // Lọc giọng Việt Nam
            var viLocales = locales.Where(l =>
                l.Language.StartsWith("vi", StringComparison.OrdinalIgnoreCase)).ToList();

            // Chọn giọng nam/nữ
            if (_voicePreference.VoiceGender == VoiceGender.Male)
            {
                // Ưu tiên giọng nam
                locale = viLocales.FirstOrDefault(l =>
                    l.Name.Contains("Male") ||
                    l.Name.Contains("Nam") ||
                    l.Id.Contains("male", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                // Ưu tiên giọng nữ
                locale = viLocales.FirstOrDefault(l =>
                    l.Name.Contains("Female") ||
                    l.Name.Contains("Nữ") ||
                    l.Id.Contains("female", StringComparison.OrdinalIgnoreCase));
            }

            // Fallback: bất kỳ giọng Việt nào
            locale ??= viLocales.FirstOrDefault();
        }
        else // English
        {
            var enLocales = locales.Where(l =>
                l.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase)).ToList();

            if (_voicePreference.VoiceGender == VoiceGender.Male)
            {
                locale = enLocales.FirstOrDefault(l =>
                    l.Name.Contains("Male") ||
                    l.Id.Contains("male", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                locale = enLocales.FirstOrDefault(l =>
                    l.Name.Contains("Female") ||
                    l.Id.Contains("female", StringComparison.OrdinalIgnoreCase));
            }

            locale ??= enLocales.FirstOrDefault();
        }

        if (locale != null)
        {
            await TextToSpeech.Default.SpeakAsync(text,
                new Microsoft.Maui.Media.SpeechOptions
                {
                    Locale = locale,
                    Pitch = 1.0f,  // Pitch bình thường
                    Volume = 1.0f  // Volume tối đa
                });
        }
        else
        {
            await TextToSpeech.Default.SpeakAsync(text);
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"TTS error: {ex.Message}");
        await TextToSpeech.Default.SpeakAsync(text);
    }
}
```

#### **Bước 3: Update SettingsPageViewModel**

**File: `ViewModels/SettingsPageViewModel.cs`**
```csharp
private readonly IVoicePreferenceService _voicePreference;

[ObservableProperty]
private string selectedVoiceGender = "Nữ";

public SettingsPageViewModel(
    IPOIService poiService,
    ILocationService locationService,
    ILocalizationService localizationService,
    IVoicePreferenceService voicePreference)
{
    _poiService = poiService;
    _locationService = locationService;
    _localizationService = localizationService;
    _voicePreference = voicePreference;

    SelectedLanguage = _localizationService.IsVietnamese ? "Tiếng Việt" : "English";
    SelectedVoiceGender = _voicePreference.VoiceGender == VoiceGender.Male ? "Nam" : "Nữ";
    UpdateGpsStatus();
}

partial void OnSelectedVoiceGenderChanged(string value)
{
    var gender = value == "Nam" ? VoiceGender.Male : VoiceGender.Female;
    _voicePreference.SaveVoiceGender(gender);
}

public List<string> AvailableVoiceGenders { get; } = new() { "Nam", "Nữ" };
```

#### **Bước 4: Update Settings UI**

**File: `Views/SettingsPage.xaml`**
```xml
<!-- Thêm sau Language Picker -->
<Border Style="{StaticResource SettingsBorderStyle}">
    <VerticalStackLayout Spacing="8">
        <Label Text="🎙️ Giọng đọc" Style="{StaticResource SettingsLabelStyle}" />
        <Picker ItemsSource="{Binding AvailableVoiceGenders}"
                SelectedItem="{Binding SelectedVoiceGender}"
                Title="Chọn giọng"
                TextColor="{StaticResource Primary}"
                FontSize="16" />
        <Label Text="Chọn giọng Nam hoặc Nữ cho thuyết minh tự động"
               Style="{StaticResource SettingsDescriptionStyle}" />
    </VerticalStackLayout>
</Border>
```

#### **Bước 5: Register Service**

**File: `MauiProgram.cs`**
```csharp
// Thêm vào ConfigureServices
builder.Services.AddSingleton<IVoicePreferenceService, VoicePreferenceService>();
```

---

### 2. Tour Thông Minh Theo FoodType & Price

#### **Bước 1: Tạo Tour Generator Service**

**File mới: `Services/TourGeneratorService.cs`**
```csharp
namespace HeriStepAI.Mobile.Services;

public interface ITourGeneratorService
{
    Task<List<Tour>> GenerateSmartToursAsync();
}

public class TourGeneratorService : ITourGeneratorService
{
    private readonly IPOIService _poiService;
    private readonly ILocalizationService _localizationService;

    public TourGeneratorService(IPOIService poiService, ILocalizationService localizationService)
    {
        _poiService = poiService;
        _localizationService = localizationService;
    }

    public async Task<List<Tour>> GenerateSmartToursAsync()
    {
        var allPOIs = await _poiService.GetAllPOIsAsync();
        var foodPOIs = allPOIs.Where(p => p.Category == 2).ToList(); // Category 2 = Food

        var tours = new List<Tour>();

        // Tour 1: Budget-Friendly (Dưới 30k)
        tours.Add(CreateTour(
            id: 1,
            name: "Tour Tiết Kiệm",
            description: "Khám phá ẩm thực đường phố giá rẻ, dưới 30.000đ",
            imageUrl: "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=400",
            pois: foodPOIs.Where(p => p.PriceMax <= 30000).ToList(),
            priceRange: "Dưới 30k"
        ));

        // Tour 2: Mid-Range (30k - 100k)
        tours.Add(CreateTour(
            id: 2,
            name: "Tour Tầm Trung",
            description: "Ẩm thực chất lượng với giá hợp lý 30k-100k",
            imageUrl: "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=400",
            pois: foodPOIs.Where(p => p.PriceMin >= 30000 && p.PriceMax <= 100000).ToList(),
            priceRange: "30k - 100k"
        ));

        // Tour 3: Premium (Trên 100k)
        tours.Add(CreateTour(
            id: 3,
            name: "Tour Cao Cấp",
            description: "Trải nghiệm ẩm thực sang trọng, trên 100.000đ",
            imageUrl: "https://images.unsplash.com/photo-1414235077428-338989a2e8c0?w=400",
            pois: foodPOIs.Where(p => p.PriceMin > 100000).ToList(),
            priceRange: "Trên 100k"
        ));

        // Tour by FoodType
        var foodTypes = Enum.GetValues(typeof(FoodType)).Cast<FoodType>()
            .Where(ft => ft != FoodType.Other).ToList();

        int tourId = 4;
        foreach (var foodType in foodTypes)
        {
            var poisByType = foodPOIs.Where(p => p.FoodType == (int)foodType).ToList();
            if (poisByType.Count >= 3) // Chỉ tạo tour nếu có ít nhất 3 POIs
            {
                tours.Add(CreateTour(
                    id: tourId++,
                    name: $"Tour {GetFoodTypeName(foodType)}",
                    description: $"Khám phá các món {GetFoodTypeName(foodType).ToLower()} đặc sắc",
                    imageUrl: GetFoodTypeImage(foodType),
                    pois: poisByType,
                    priceRange: GetAveragePriceRange(poisByType)
                ));
            }
        }

        return tours.Where(t => t.POIs.Count > 0).ToList();
    }

    private Tour CreateTour(int id, string name, string description, string imageUrl,
        List<POI> pois, string priceRange)
    {
        if (pois.Count == 0)
            return new Tour { Id = id, Name = name, Description = description, POIs = pois };

        var avgPrice = pois.Average(p => (p.PriceMin + p.PriceMax) / 2);
        var totalMinutes = pois.Sum(p => p.EstimatedMinutes);

        return new Tour
        {
            Id = id,
            Name = name,
            Description = description,
            ImageUrl = imageUrl,
            POIs = pois.OrderBy(p => p.Name).ToList(),
            EstimatedDuration = $"{totalMinutes / 60}h{totalMinutes % 60}m",
            TotalStops = pois.Count,
            AveragePrice = $"~{avgPrice:N0}đ",
            PriceRange = priceRange
        };
    }

    private string GetFoodTypeName(FoodType foodType) => foodType switch
    {
        FoodType.Seafood => "Hải Sản",
        FoodType.Vegetarian => "Chay",
        FoodType.Specialty => "Đặc Sản",
        FoodType.Street => "Đường Phố",
        FoodType.Grilled => "Nướng",
        FoodType.Noodles => "Bún/Phở/Mì",
        _ => "Khác"
    };

    private string GetFoodTypeImage(FoodType foodType) => foodType switch
    {
        FoodType.Seafood => "https://images.unsplash.com/photo-1559339352-11d035aa65de?w=400",
        FoodType.Vegetarian => "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=400",
        FoodType.Specialty => "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=400",
        FoodType.Street => "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=400",
        FoodType.Grilled => "https://images.unsplash.com/photo-1529042410759-befb1204b468?w=400",
        FoodType.Noodles => "https://images.unsplash.com/photo-1569718212165-3a8278d5f624?w=400",
        _ => "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=400"
    };

    private string GetAveragePriceRange(List<POI> pois)
    {
        if (pois.Count == 0) return "N/A";
        var avgMin = pois.Average(p => p.PriceMin);
        var avgMax = pois.Average(p => p.PriceMax);
        return $"{avgMin:N0}đ - {avgMax:N0}đ";
    }
}
```

#### **Bước 2: Update Tour Model**

**File: `Models/Tour.cs`**
```csharp
// Thêm các property mới
public string? PriceRange { get; set; }
public string? AveragePrice { get; set; }
public int TotalStops { get; set; }
public string? EstimatedDuration { get; set; }
```

#### **Bước 3: Update MainPageViewModel**

**File: `ViewModels/MainPageViewModel.cs`**
```csharp
private readonly ITourGeneratorService _tourGenerator;

// Update constructor
public MainPageViewModel(
    IPOIService poiService,
    ILocationService locationService,
    ITourSelectionService tourSelectionService,
    ILocalizationService localizationService,
    ITourGeneratorService tourGenerator)
{
    _poiService = poiService;
    _locationService = locationService;
    _tourSelectionService = tourSelectionService;
    _localizationService = localizationService;
    _tourGenerator = tourGenerator;

    _ = LoadDataAsync();
}

private async Task LoadDataAsync()
{
    try
    {
        await _poiService.SyncPOIsFromServerAsync();

        // Generate smart tours
        var smartTours = await _tourGenerator.GenerateSmartToursAsync();
        Tours = new ObservableCollection<Tour>(smartTours);

        // Load nearby POIs
        var allPOIs = await _poiService.GetAllPOIsAsync();
        var userLocation = await _locationService.GetCurrentLocationAsync();

        if (userLocation != null)
        {
            var nearby = allPOIs
                .Where(p => CalculateDistance(userLocation.Latitude, userLocation.Longitude,
                    p.Latitude, p.Longitude) <= 5.0)
                .OrderBy(p => CalculateDistance(userLocation.Latitude, userLocation.Longitude,
                    p.Latitude, p.Longitude))
                .Take(10)
                .ToList();

            NearbyPOIs = new ObservableCollection<POI>(nearby);
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"LoadData error: {ex.Message}");
    }
}
```

#### **Bước 4: Register Service**

**File: `MauiProgram.cs`**
```csharp
builder.Services.AddSingleton<ITourGeneratorService, TourGeneratorService>();
```

---

### 3. Cải Thiện UI

#### **Color Scheme Hiện Đại**

**File: `Resources/Styles/Colors.xaml`**
```xml
<!-- Modern Color Palette -->
<Color x:Key="Primary">#6366F1</Color>          <!-- Indigo -->
<Color x:Key="PrimaryDark">#4F46E5</Color>
<Color x:Key="PrimaryLight">#818CF8</Color>

<Color x:Key="Secondary">#F59E0B</Color>        <!-- Amber -->
<Color x:Key="SecondaryDark">#D97706</Color>
<Color x:Key="SecondaryLight">#FCD34D</Color>

<Color x:Key="Accent">#10B981</Color>           <!-- Emerald -->
<Color x:Key="AccentDark">#059669</Color>

<Color x:Key="Success">#10B981</Color>
<Color x:Key="Warning">#F59E0B</Color>
<Color x:Key="Danger">#EF4444</Color>
<Color x:Key="Info">#3B82F6</Color>

<Color x:Key="BackgroundLight">#F9FAFB</Color>
<Color x:Key="BackgroundDark">#111827</Color>

<Color x:Key="SurfaceLight">#FFFFFF</Color>
<Color x:Key="SurfaceDark">#1F2937</Color>

<Color x:Key="TextPrimary">#111827</Color>
<Color x:Key="TextSecondary">#6B7280</Color>
<Color x:Key="TextTertiary">#9CA3AF</Color>
```

#### **Modern Card Style**

**File: `Resources/Styles/Styles.xaml`**
```xml
<!-- Modern Card Style -->
<Style x:Key="ModernCard" TargetType="Border">
    <Setter Property="BackgroundColor" Value="{AppThemeBinding Light={StaticResource SurfaceLight}, Dark={StaticResource SurfaceDark}}" />
    <Setter Property="StrokeThickness" Value="0" />
    <Setter Property="Padding" Value="16" />
    <Setter Property="Margin" Value="16,8" />
    <Setter Property="Shadow">
        <Shadow Brush="{StaticResource Black}"
                Opacity="0.1"
                Radius="16"
                Offset="0,4" />
    </Setter>
    <Setter Property="StrokeShape">
        <RoundRectangle CornerRadius="16" />
    </Setter>
</Style>

<!-- Gradient Header Style -->
<Style x:Key="GradientHeader" TargetType="Border">
    <Setter Property="Background">
        <LinearGradientBrush StartPoint="0,0" EndPoint="1,1">
            <GradientStop Color="{StaticResource Primary}" Offset="0.0" />
            <GradientStop Color="{StaticResource PrimaryDark}" Offset="1.0" />
        </LinearGradientBrush>
    </Setter>
    <Setter Property="Padding" Value="20" />
    <Setter Property="StrokeThickness" Value="0" />
</Style>

<!-- Modern Button Style -->
<Style x:Key="PrimaryButton" TargetType="Button">
    <Setter Property="BackgroundColor" Value="{StaticResource Primary}" />
    <Setter Property="TextColor" Value="{StaticResource White}" />
    <Setter Property="FontAttributes" Value="Bold" />
    <Setter Property="FontSize" Value="16" />
    <Setter Property="Padding" Value="20,12" />
    <Setter Property="CornerRadius" Value="12" />
    <Setter Property="Shadow">
        <Shadow Brush="{StaticResource Primary}"
                Opacity="0.3"
                Radius="8"
                Offset="0,4" />
    </Setter>
</Style>
```

#### **Tour Card với Gradient Overlay**

**File: `Views/MainPage.xaml`**
```xml
<!-- Modern Tour Card -->
<Border Style="{StaticResource ModernCard}" Margin="12,6">
    <Grid RowDefinitions="180,Auto,Auto">
        <!-- Image with Gradient Overlay -->
        <Border Grid.Row="0" StrokeThickness="0">
            <Border.StrokeShape>
                <RoundRectangle CornerRadius="12" />
            </Border.StrokeShape>
            <Grid>
                <Image Source="{Binding ImageUrl}"
                       Aspect="AspectFill" />
                <!-- Gradient Overlay -->
                <Border StrokeThickness="0">
                    <Border.Background>
                        <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
                            <GradientStop Color="Transparent" Offset="0.0" />
                            <GradientStop Color="#AA000000" Offset="1.0" />
                        </LinearGradientBrush>
                    </Border.Background>
                </Border>
                <!-- Badge -->
                <Border BackgroundColor="{StaticResource Accent}"
                        Padding="12,6"
                        HorizontalOptions="End"
                        VerticalOptions="Start"
                        Margin="12">
                    <Border.StrokeShape>
                        <RoundRectangle CornerRadius="8" />
                    </Border.StrokeShape>
                    <Label Text="{Binding TotalStops, StringFormat='{0} điểm'}"
                           TextColor="{StaticResource White}"
                           FontSize="12"
                           FontAttributes="Bold" />
                </Border>
                <!-- Tour Info -->
                <VerticalStackLayout VerticalOptions="End"
                                     Margin="16"
                                     Spacing="4">
                    <Label Text="{Binding Name}"
                           TextColor="{StaticResource White}"
                           FontSize="18"
                           FontAttributes="Bold" />
                    <HorizontalStackLayout Spacing="8">
                        <Label Text="⏱️"
                               TextColor="{StaticResource White}"
                               FontSize="12" />
                        <Label Text="{Binding EstimatedDuration}"
                               TextColor="{StaticResource White}"
                               FontSize="12" />
                        <Label Text="💰"
                               TextColor="{StaticResource White}"
                               FontSize="12"
                               Margin="8,0,0,0" />
                        <Label Text="{Binding PriceRange}"
                               TextColor="{StaticResource White}"
                               FontSize="12" />
                    </HorizontalStackLayout>
                </VerticalStackLayout>
            </Grid>
        </Border>

        <!-- Description -->
        <Label Grid.Row="1"
               Text="{Binding Description}"
               TextColor="{StaticResource TextSecondary}"
               FontSize="14"
               Margin="0,12,0,0"
               LineBreakMode="TailTruncation"
               MaxLines="2" />

        <!-- Action Button -->
        <Button Grid.Row="2"
                Text="Bắt đầu tour"
                Style="{StaticResource PrimaryButton}"
                Margin="0,12,0,0"
                Command="{Binding Source={RelativeSource AncestorType={x:Type viewmodels:MainPageViewModel}}, Path=SelectTourCommand}"
                CommandParameter="{Binding .}" />
    </Grid>
</Border>
```

---

### 4. Test Geofencing Mà Không Cần Di Chuyển

#### **Bước 1: Tạo Location Simulator**

**File mới: `Services/LocationSimulatorService.cs`**
```csharp
namespace HeriStepAI.Mobile.Services;

public interface ILocationSimulatorService
{
    bool IsSimulating { get; }
    void StartSimulation(List<POI> route);
    void StopSimulation();
    event EventHandler<Location>? LocationChanged;
}

public class LocationSimulatorService : ILocationSimulatorService
{
    private bool _isSimulating;
    private CancellationTokenSource? _cts;
    private int _currentIndex;
    private List<POI>? _route;

    public bool IsSimulating => _isSimulating;

    public event EventHandler<Location>? LocationChanged;

    public void StartSimulation(List<POI> route)
    {
        if (_isSimulating) StopSimulation();

        _route = route;
        _currentIndex = 0;
        _isSimulating = true;
        _cts = new CancellationTokenSource();

        _ = SimulateMovementAsync(_cts.Token);
    }

    public void StopSimulation()
    {
        _cts?.Cancel();
        _isSimulating = false;
        _route = null;
        _currentIndex = 0;
    }

    private async Task SimulateMovementAsync(CancellationToken ct)
    {
        if (_route == null || _route.Count == 0) return;

        try
        {
            while (!ct.IsCancellationRequested && _currentIndex < _route.Count)
            {
                var poi = _route[_currentIndex];

                // Tạo location tại POI
                var location = new Location(poi.Latitude, poi.Longitude)
                {
                    Accuracy = 10.0,
                    Timestamp = DateTimeOffset.UtcNow
                };

                LocationChanged?.Invoke(this, location);

                AppLog.Info($"🚶 Simulating at: {poi.Name} ({poi.Latitude}, {poi.Longitude})");

                // Đợi 10 giây trước khi chuyển sang POI tiếp theo
                await Task.Delay(10000, ct);

                _currentIndex++;
            }

            // Khi hết route, quay lại đầu
            if (_currentIndex >= _route.Count)
            {
                _currentIndex = 0;
                await SimulateMovementAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            AppLog.Info("Simulation stopped");
        }
    }
}
```

#### **Bước 2: Update LocationService**

**File: `Services/LocationService.cs`**
```csharp
private readonly ILocationSimulatorService _simulator;

public LocationService(ILocationSimulatorService simulator)
{
    _simulator = simulator;

    // Listen to simulator events
    _simulator.LocationChanged += OnSimulatedLocationChanged;
}

private void OnSimulatedLocationChanged(object? sender, Location e)
{
    _currentLocation = e;
    LocationChanged?.Invoke(this, e);
}

public async Task<Location?> GetCurrentLocationAsync()
{
    // Nếu đang simulate, trả về simulated location
    if (_simulator.IsSimulating && _currentLocation != null)
    {
        return _currentLocation;
    }

    // Nếu không, lấy GPS thật
    try
    {
        var location = await Geolocation.GetLocationAsync(new GeolocationRequest
        {
            DesiredAccuracy = GeolocationAccuracy.High,
            Timeout = TimeSpan.FromSeconds(10)
        });

        if (location != null)
        {
            _currentLocation = location;
        }

        return _currentLocation;
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"GetLocation error: {ex.Message}");
        return _currentLocation;
    }
}
```

#### **Bước 3: Thêm Test Mode UI**

**File: `ViewModels/MapPageViewModel.cs`**
```csharp
private readonly ILocationSimulatorService _simulator;

[ObservableProperty]
private bool isTestMode = false;

[ObservableProperty]
private string testModeButtonText = "🧪 Bật Test Mode";

public MapPageViewModel(
    IPOIService poiService,
    ILocationService locationService,
    INarrationService narrationService,
    ILocalizationService localizationService,
    ITourSelectionService tourSelectionService,
    ILocationSimulatorService simulator)
{
    _poiService = poiService;
    _locationService = locationService;
    _narrationService = narrationService;
    _localizationService = localizationService;
    _tourSelectionService = tourSelectionService;
    _simulator = simulator;

    _ = InitializeMapAsync();
}

[RelayCommand]
private void ToggleTestMode()
{
    if (!IsTestMode)
    {
        // Bật test mode
        var route = POIs.Take(5).ToList(); // Test với 5 POIs đầu tiên
        _simulator.StartSimulation(route);
        IsTestMode = true;
        TestModeButtonText = "🛑 Tắt Test Mode";

        Shell.Current.DisplayAlert("Test Mode",
            $"Đang simulate di chuyển qua {route.Count} điểm. Thuyết minh sẽ tự động phát khi đến mỗi điểm.",
            "OK");
    }
    else
    {
        // Tắt test mode
        _simulator.StopSimulation();
        IsTestMode = false;
        TestModeButtonText = "🧪 Bật Test Mode";
    }
}
```

#### **Bước 4: Add Test Mode Button**

**File: `Views/MapPage.xaml`**
```xml
<!-- Test Mode Button -->
<Button Text="{Binding TestModeButtonText}"
        Style="{StaticResource SecondaryButton}"
        Command="{Binding ToggleTestModeCommand}"
        BackgroundColor="{Binding IsTestMode, Converter={StaticResource BoolToColorConverter}, ConverterParameter='#EF4444|#6366F1'}"
        HorizontalOptions="Center"
        VerticalOptions="Start"
        Margin="16" />
```

#### **Bước 5: Register Service**

**File: `MauiProgram.cs`**
```csharp
builder.Services.AddSingleton<ILocationSimulatorService, LocationSimulatorService>();
```

---

## 📋 Checklist Triển Khai

### ✅ Voice Gender Settings
- [ ] Tạo `VoicePreferenceService`
- [ ] Update `NarrationService` với voice gender logic
- [ ] Update `SettingsPageViewModel`
- [ ] Update `SettingsPage.xaml` UI
- [ ] Test giọng Nam/Nữ

### ✅ Smart Tours
- [ ] Tạo `TourGeneratorService`
- [ ] Update `Tour` model
- [ ] Update `MainPageViewModel`
- [ ] Test tours theo price range
- [ ] Test tours theo food type

### ✅ Modern UI
- [ ] Update `Colors.xaml`
- [ ] Update `Styles.xaml`
- [ ] Update `MainPage.xaml` với modern cards
- [ ] Update `MapPage.xaml`
- [ ] Test dark mode

### ✅ Location Simulator
- [ ] Tạo `LocationSimulatorService`
- [ ] Update `LocationService`
- [ ] Update `MapPageViewModel`
- [ ] Add Test Mode button
- [ ] Test simulation với route

---

## 🚀 Kết Quả Mong Đợi

1. **Voice**: Chọn giọng Nam/Nữ, không còn Bắc/Trung/Nam
2. **Tours**: Tour thông minh theo giá và loại món ăn
3. **UI**: Modern, đẹp mắt với gradient, shadow, rounded corners
4. **Testing**: Test geofencing ngay trên máy mà không cần di chuyển

---

## 📝 Ghi Chú

- Tất cả code đã được optimize cho MAUI .NET 8
- UI responsive, hỗ trợ dark mode
- Location simulator có thể customize speed và route
- Voice selection dựa trên system TTS locales available

