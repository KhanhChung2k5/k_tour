namespace HeriStepAI.Geo;

/// <summary>Dữ liệu tối thiểu để chọn POI trong geofence (không phụ thuộc MAUI/SQLite).</summary>
public readonly record struct GeofencePoi(
    int Id,
    string Name,
    double Latitude,
    double Longitude,
    double RadiusMeters,
    int Priority);
