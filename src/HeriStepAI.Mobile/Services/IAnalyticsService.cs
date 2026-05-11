using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

public record POIVisitRecord(int Id, string Name, int VisitCount, double Rating);

public interface IAnalyticsService
{
    /// <summary>Số lượt ghé thăm cửa hàng</summary>
    int ShopsVisited { get; }
    /// <summary>Tổng khoảng cách đã đi</summary>
    double TotalDistanceMeters { get; }
    /// <summary>Số lượt tour đã hoàn thành</summary>
    int ToursCompleted { get; }
    int NarrationCount { get; }

    /// <summary>Visit counts per day: index 0 = Monday, 6 = Sunday (current week).</summary>
    int[] WeeklyActivity { get; }

    /// <summary>Top POIs sorted by visit count descending.</summary>
    List<POIVisitRecord> TopPOIs { get; }
    /// <summary>Danh sách TOP 10 địa điểm được ghé nhiều nhất</summary>

    void RecordPOIVisit(POI poi);
    /// <summary>Ghi lại lần nghe thuyết minh</summary>
    void RecordNarration();
    /// <summary>Ghi lại lần hoàn thành tour</summary>
    void RecordTourCompleted();
    /// <summary>Ghi lại quãng đường đã đi</summary>
    void AddDistance(double meters);
}
