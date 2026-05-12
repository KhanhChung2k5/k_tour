namespace HeriStepAI.Geo;

/// <summary>
/// Chọn POI "thắng" khi user nằm trong nhiều vòng tròn: Priority cao trước, cùng Priority thì gần hơn trước,
/// cùng luôn khoảng cách thì Id nhỏ hơn (tie-break ổn định).
/// (đồng bộ với GeofenceService trên mobile và logic sort trong GeofenceSimulator).
/// </summary>
public static class GeofenceSelection
{
    /// <summary>
    /// Trả về POI được chọn nếu user nằm trong ít nhất một vùng; null nếu ngoài tất cả.
    /// </summary>
    /// <param name="minRadiusMeters">Bán kính tối thiểu áp dụng (ví dụ 50m).</param>
    public static GeofencePoi? FindBestInside(
        double userLatitude,
        double userLongitude,
        IReadOnlyList<GeofencePoi> pois,
        double minRadiusMeters)
    {
        if (pois == null || pois.Count == 0)
            return null;

        var candidates = new List<(GeofencePoi Poi, double DistanceMeters)>();
        foreach (var poi in pois)
        {
            var effectiveRadius = Math.Max(poi.RadiusMeters, minRadiusMeters);
            var distance = HaversineMeters(userLatitude, userLongitude, poi.Latitude, poi.Longitude);
            if (distance <= effectiveRadius)
                candidates.Add((poi, distance));
        }

        if (candidates.Count == 0)
            return null;

        var best = candidates
            .OrderByDescending(x => x.Poi.Priority)
            .ThenBy(x => x.DistanceMeters)
            .ThenBy(x => x.Poi.Id)
            .First();

        return best.Poi;
    }

    public static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
