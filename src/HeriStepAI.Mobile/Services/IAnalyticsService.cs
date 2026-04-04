using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

public record POIVisitRecord(int Id, string Name, int VisitCount, double Rating);

public interface IAnalyticsService
{
    int ShopsVisited { get; }
    double TotalDistanceMeters { get; }
    int ToursCompleted { get; }
    int NarrationCount { get; }

    /// <summary>Visit counts per day: index 0 = Monday, 6 = Sunday (current week).</summary>
    int[] WeeklyActivity { get; }

    /// <summary>Top POIs sorted by visit count descending.</summary>
    List<POIVisitRecord> TopPOIs { get; }

    void RecordPOIVisit(POI poi);
    void RecordNarration();
    void RecordTourCompleted();
    void AddDistance(double meters);
}
