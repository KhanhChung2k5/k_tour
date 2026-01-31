namespace HeriStepAI.API.Models;

public class VisitLog
{
    public int Id { get; set; }
    public int POId { get; set; }
    public POI POI { get; set; } = null!;
    public string? UserId { get; set; } // Anonymous or user ID
    public DateTime VisitTime { get; set; } = DateTime.UtcNow;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public VisitType VisitType { get; set; } = VisitType.Geofence;
    public int? DurationSeconds { get; set; } // How long they stayed/listened
}

public enum VisitType
{
    Geofence = 1,
    MapClick = 2,
    QRCode = 3
}
