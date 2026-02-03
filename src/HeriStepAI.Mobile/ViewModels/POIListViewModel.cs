using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeriStepAI.Mobile.Models;
using HeriStepAI.Mobile.Services;
using Microsoft.Maui.Devices.Sensors;
using System.Collections.ObjectModel;

namespace HeriStepAI.Mobile.ViewModels;

public partial class POIListViewModel : ObservableObject
{
    private readonly IPOIService _poiService;
    private readonly INarrationService _narrationService;
    private readonly ILocationService _locationService;
    private List<POI> _allPOIs = new();
    private Location? _currentLocation;

    [ObservableProperty]
    private ObservableCollection<POI> filteredPOIs = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private int selectedCategory = 0; // 0 = All

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private bool isRefreshing = false;

    [ObservableProperty]
    private string selectedLanguage = "vi";

    public POIListViewModel(
        IPOIService poiService,
        INarrationService narrationService,
        ILocationService locationService)
    {
        _poiService = poiService;
        _narrationService = narrationService;
        _locationService = locationService;

        // Load data on initialization
        Task.Run(async () => await LoadDataAsync());
    }

    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;

            // Get current location (main thread for permission on Android)
            _currentLocation = await MainThread.InvokeOnMainThreadAsync(
                async () => await _locationService.GetCurrentLocationAsync());

            // Sync từ API trước để luôn có dữ liệu mới nhất (kể cả khi đã cập nhật Category trên server)
            await _poiService.SyncPOIsFromServerAsync();

            // Load POIs từ local database
            _allPOIs = await _poiService.GetAllPOIsAsync() ?? new List<POI>();

            // Set placeholder image for POIs without ImageUrl
            var placeholders = new[] {
                "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=400",
                "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=400",
                "https://images.unsplash.com/photo-1552566626-52f8b828add9?w=400",
            };
            for (int i = 0; i < _allPOIs.Count; i++)
            {
                if (string.IsNullOrEmpty(_allPOIs[i].ImageUrl))
                    _allPOIs[i].ImageUrl = placeholders[i % placeholders.Length];
            }

            // Calculate distances
            if (_currentLocation != null)
            {
                foreach (var poi in _allPOIs)
                {
                    poi.DistanceMeters = HaversineDistance(
                        _currentLocation.Latitude,
                        _currentLocation.Longitude,
                        poi.Latitude,
                        poi.Longitude);
                }

                // Sort by distance
                _allPOIs = _allPOIs.OrderBy(p => p.DistanceMeters).ToList();
            }

            // Apply filters
            await MainThread.InvokeOnMainThreadAsync(() => ApplyFilters());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading POIs: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedCategoryChanged(int value)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = _allPOIs.AsEnumerable();

        // Apply category filter
        // Category = 0 (uncategorized) hiển thị trong tất cả tab vì API/DB có thể chưa có Category
        if (SelectedCategory > 0)
        {
            filtered = filtered.Where(p => p.Category == SelectedCategory || p.Category == 0);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.Name.ToLowerInvariant().Contains(searchLower) ||
                (p.Description?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (p.Address?.ToLowerInvariant().Contains(searchLower) ?? false));
        }

        FilteredPOIs.Clear();
        foreach (var poi in filtered)
        {
            FilteredPOIs.Add(poi);
        }
    }

    [RelayCommand]
    private void SelectCategory(string categoryStr)
    {
        if (int.TryParse(categoryStr, out var category))
        {
            SelectedCategory = category;
        }
    }

    [RelayCommand]
    private async Task SelectPOI(POI poi)
    {
        if (poi == null) return;

        // Play narration (load Contents from POIService if needed)
        var langCode = SelectedLanguage switch { "English" => "en", "中文" => "zh", "日本語" => "ja", "한국어" => "ko", _ => "vi" };
        await _narrationService.PlayNarrationAsync(poi, langCode, forcePlay: true);

        // Navigate to Map tab to show the POI location
        try { await Shell.Current.GoToAsync("//MapPage"); } catch { }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        try
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
        catch
        {
            // Ignore navigation errors (e.g. when already on MainPage)
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        IsRefreshing = true;
        try
        {
            await _poiService.SyncPOIsFromServerAsync();
            await LoadDataAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth radius in meters
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}
