using HeriStepAI.Mobile.Models;
using System.Text.Json;

namespace HeriStepAI.Mobile.Services;

/// <summary>
/// Stores analytics data locally using Preferences (persists across app restarts).
/// No account required — works for guest users.
/// </summary>
public class LocalAnalyticsService : IAnalyticsService
{
    // Preference keys
    private const string KeyShops = "a_shops";
    private const string KeyDistance = "a_dist";
    private const string KeyTours = "a_tours";
    private const string KeyNarrations = "a_narr";
    private const string KeyWeekStart = "a_week_start"; // stored as "yyyy-MM-dd"
    private const string KeyDayPrefix = "a_day_";       // a_day_0 .. a_day_6
    private const string KeyTopPois = "a_top_pois";     // JSON

    public int ShopsVisited => Preferences.Default.Get(KeyShops, 0);
    public double TotalDistanceMeters => Preferences.Default.Get(KeyDistance, 0.0);
    public int ToursCompleted => Preferences.Default.Get(KeyTours, 0);
    public int NarrationCount => Preferences.Default.Get(KeyNarrations, 0);

    public int[] WeeklyActivity
    {
        get
        {
            EnsureWeekReset();
            var result = new int[7];
            for (int i = 0; i < 7; i++)
                result[i] = Preferences.Default.Get(KeyDayPrefix + i, 0);
            return result;
        }
    }

    public List<POIVisitRecord> TopPOIs
    {
        get
        {
            var json = Preferences.Default.Get(KeyTopPois, "[]");
            try
            {
                var list = JsonSerializer.Deserialize<List<POIVisitRecord>>(json) ?? new();
                return list.OrderByDescending(p => p.VisitCount).Take(3).ToList();
            }
            catch
            {
                return new();
            }
        }
    }

    public void RecordPOIVisit(POI poi)
    {
        // Total count
        Preferences.Default.Set(KeyShops, ShopsVisited + 1);

        // Weekly day count
        EnsureWeekReset();
        int dayIndex = GetTodayIndex();
        string dayKey = KeyDayPrefix + dayIndex;
        Preferences.Default.Set(dayKey, Preferences.Default.Get(dayKey, 0) + 1);

        // Top POIs
        UpdateTopPOIs(poi);
    }

    public void RecordNarration()
    {
        Preferences.Default.Set(KeyNarrations, NarrationCount + 1);
    }

    public void RecordTourCompleted()
    {
        Preferences.Default.Set(KeyTours, ToursCompleted + 1);
    }

    public void AddDistance(double meters)
    {
        if (meters <= 0 || meters > 500) return; // ignore teleports / noise
        Preferences.Default.Set(KeyDistance, TotalDistanceMeters + meters);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void EnsureWeekReset()
    {
        var thisMonday = GetThisMonday().ToString("yyyy-MM-dd");
        var stored = Preferences.Default.Get(KeyWeekStart, "");
        if (stored != thisMonday)
        {
            for (int i = 0; i < 7; i++)
                Preferences.Default.Set(KeyDayPrefix + i, 0);
            Preferences.Default.Set(KeyWeekStart, thisMonday);
        }
    }

    private static DateOnly GetThisMonday()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        int diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return today.AddDays(-diff);
    }

    private static int GetTodayIndex()
    {
        // Monday = 0, Sunday = 6
        return ((int)DateTime.Now.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
    }

    private void UpdateTopPOIs(POI poi)
    {
        var json = Preferences.Default.Get(KeyTopPois, "[]");
        List<POIVisitRecord> list;
        try { list = JsonSerializer.Deserialize<List<POIVisitRecord>>(json) ?? new(); }
        catch { list = new(); }

        var existing = list.FirstOrDefault(p => p.Id == poi.Id);
        if (existing != null)
        {
            list.Remove(existing);
            list.Add(existing with { VisitCount = existing.VisitCount + 1 });
        }
        else
        {
            list.Add(new POIVisitRecord(poi.Id, poi.Name, 1, poi.Rating ?? 0.0));
        }

        // Keep top 10 to avoid unbounded growth
        list = list.OrderByDescending(p => p.VisitCount).Take(10).ToList();
        Preferences.Default.Set(KeyTopPois, JsonSerializer.Serialize(list));
    }
}
