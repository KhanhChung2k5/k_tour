using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStepAI.API.Models;

public class Analytics
{
    public int Id { get; set; }
    public int POId { get; set; }
    public POI POI { get; set; } = null!;
    public DateTime Date { get; set; }
    public int VisitCount { get; set; }
    public int UniqueVisitors { get; set; }
    public double AverageDuration { get; set; }
    [NotMapped]
    public Dictionary<string, int>? VisitByType { get; set; } // JSON
}
