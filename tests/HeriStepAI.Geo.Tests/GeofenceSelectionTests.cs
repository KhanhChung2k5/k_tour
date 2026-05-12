using Xunit;

namespace HeriStepAI.Geo.Tests;

/// <summary>
/// Kiểm tra chọn POI trong geofence (Priority rồi khoảng cách) — cùng logic với GeofenceService / simulator.
/// </summary>
public sealed class GeofenceSelectionTests
{
    private const double MinR = 50;
    private const double HubLat = 10.762622;
    private const double HubLon = 106.660172;

    /// <summary>Độ lệch tọa độ gần đúng cho vài trăm mét quanh vĩ độ Việt Nam.</summary>
    private static (double Lat, double Lon) OffsetMeters(double lat0, double lon0, double northM, double eastM = 0)
    {
        var dLat = northM / 111_320.0;
        var dLon = eastM / (111_320.0 * Math.Cos(lat0 * Math.PI / 180.0));
        return (lat0 + dLat, lon0 + dLon);
    }

    [Fact]
    [Trait("Geofence", GeofenceTestCaseIds.GF001_ThreeSamePriorityOverlap_NearestWins)]
    public void GF001_ThreePois_SamePriority_Overlap_UserInsideAll_NearestWins()
    {
        var user = (HubLat, HubLon);
        const int p = 2;
        const double r = 400;
        var a = new GeofencePoi(1, "A", OffsetMeters(HubLat, HubLon, 80).Lat, OffsetMeters(HubLat, HubLon, 80).Lon, r, p);
        var b = new GeofencePoi(2, "B", OffsetMeters(HubLat, HubLon, 200).Lat, OffsetMeters(HubLat, HubLon, 200).Lon, r, p);
        var c = new GeofencePoi(3, "C", OffsetMeters(HubLat, HubLon, 320).Lat, OffsetMeters(HubLat, HubLon, 320).Lon, r, p);
        var list = new[] { c, b, a };

        var best = GeofenceSelection.FindBestInside(user.Item1, user.Item2, list, MinR);
        Assert.NotNull(best);
        Assert.Equal(1, best.Value.Id);
    }

    [Fact]
    [Trait("Geofence", GeofenceTestCaseIds.GF002_FiveSamePriorityDifferentDistance)]
    public void GF002_FivePois_SamePriority_DifferentDistances_NearestWins()
    {
        var user = (HubLat, HubLon);
        const int p = 1;
        const double r = 500;
        var pois = new[]
        {
            new GeofencePoi(10, "p10", OffsetMeters(HubLat, HubLon, 100).Lat, HubLon, r, p),
            new GeofencePoi(11, "p11", OffsetMeters(HubLat, HubLon, 40).Lat, HubLon, r, p),
            new GeofencePoi(12, "p12", OffsetMeters(HubLat, HubLon, 250).Lat, HubLon, r, p),
            new GeofencePoi(13, "p13", OffsetMeters(HubLat, HubLon, 180).Lat, HubLon, r, p),
            new GeofencePoi(14, "p14", OffsetMeters(HubLat, HubLon, 60).Lat, HubLon, r, p),
        };

        var best = GeofenceSelection.FindBestInside(user.Item1, user.Item2, pois, MinR);
        Assert.NotNull(best);
        Assert.Equal(11, best.Value.Id);
    }

    [Fact]
    [Trait("Geofence", GeofenceTestCaseIds.GF003_PriorityBeatsDistance)]
    public void GF003_TwoOverlapping_HigherPriorityWinsEvenWhenFarther()
    {
        var user = (HubLat, HubLon);
        var nearLow = new GeofencePoi(1, "near", HubLat, HubLon, 300, 1);
        var farHigh = new GeofencePoi(2, "far", OffsetMeters(HubLat, HubLon, 120).Lat, HubLon, 400, 5);
        var list = new[] { nearLow, farHigh };

        var best = GeofenceSelection.FindBestInside(user.Item1, user.Item2, list, MinR);
        Assert.NotNull(best);
        Assert.Equal(2, best.Value.Id);
    }

    [Fact]
    [Trait("Geofence", GeofenceTestCaseIds.GF004_OutsideAll_Null)]
    public void GF004_UserOutsideAllRadii_ReturnsNull()
    {
        var user = (HubLat, HubLon);
        var pois = new[]
        {
            new GeofencePoi(1, "x", OffsetMeters(HubLat, HubLon, 5000).Lat, HubLon, 100, 3),
        };

        Assert.Null(GeofenceSelection.FindBestInside(user.Item1, user.Item2, pois, MinR));
    }

    [Fact]
    [Trait("Geofence", GeofenceTestCaseIds.GF005_MinRadiusApplies)]
    public void GF005_SmallDeclaredRadius_UsesMinRadius_UserStillInside()
    {
        var user = (HubLat, HubLon);
        var poi = new GeofencePoi(1, "tiny", OffsetMeters(HubLat, HubLon, 40).Lat, HubLon, 10, 2);
        var best = GeofenceSelection.FindBestInside(user.Item1, user.Item2, new[] { poi }, MinR);
        Assert.NotNull(best);
        Assert.Equal(1, best.Value.Id);
    }

    [Fact]
    [Trait("Geofence", GeofenceTestCaseIds.GF006_MixedPriorities_SubsetCompetesByDistance)]
    public void GF006_ThreeHighPriorityAndTwoLow_OnlyHighBand_CompetesByDistance()
    {
        var user = (HubLat, HubLon);
        var lowA = new GeofencePoi(1, "L1", HubLat, HubLon, 400, 1);
        var lowB = new GeofencePoi(2, "L2", OffsetMeters(HubLat, HubLon, 50).Lat, HubLon, 400, 1);
        var hiA = new GeofencePoi(3, "H1", OffsetMeters(HubLat, HubLon, 90).Lat, HubLon, 500, 4);
        var hiB = new GeofencePoi(4, "H2", OffsetMeters(HubLat, HubLon, 30).Lat, HubLon, 500, 4);
        var hiC = new GeofencePoi(5, "H3", OffsetMeters(HubLat, HubLon, 150).Lat, HubLon, 500, 4);
        var list = new[] { lowA, hiC, lowB, hiA, hiB };

        var best = GeofenceSelection.FindBestInside(user.Item1, user.Item2, list, MinR);
        Assert.NotNull(best);
        Assert.Equal(4, best.Value.Id);
    }

    [Fact]
    [Trait("Geofence", GeofenceTestCaseIds.GF007_EmptyPoiList_Null)]
    public void GF007_EmptyList_ReturnsNull()
    {
        Assert.Null(GeofenceSelection.FindBestInside(HubLat, HubLon, Array.Empty<GeofencePoi>(), MinR));
    }

    [Fact]
    [Trait("Geofence", GeofenceTestCaseIds.GF008_SamePriority_TwoOverlappingRings_NearestCenterWins)]
    public void GF008_SamePriority_TwoOverlappingRings_NearestCenterToUserWins()
    {
        var user = (HubLat, HubLon);
        const int p = 3;
        var outer = new GeofencePoi(1, "outer", OffsetMeters(HubLat, HubLon, 200).Lat, HubLon, 450, p);
        var inner = new GeofencePoi(2, "inner", OffsetMeters(HubLat, HubLon, 45).Lat, HubLon, 200, p);
        var list = new[] { outer, inner };

        var best = GeofenceSelection.FindBestInside(user.Item1, user.Item2, list, MinR);
        Assert.NotNull(best);
        Assert.Equal(2, best.Value.Id);
    }

    [Fact]
    [Trait("Geofence", GeofenceTestCaseIds.GF009_SamePriority_SameDistance_CoincidentCenters_LowestIdWins)]
    public void GF009_SamePriority_SameDistance_UserAtSharedCenter_LowestIdWins()
    {
        const int p = 2;
        const double r = 200;
        var highId = new GeofencePoi(99, "late", HubLat, HubLon, r, p);
        var lowId = new GeofencePoi(7, "early", HubLat, HubLon, r, p);
        var list = new[] { highId, lowId };

        var best = GeofenceSelection.FindBestInside(HubLat, HubLon, list, MinR);
        Assert.NotNull(best);
        Assert.Equal(7, best.Value.Id);
    }

    [Fact]
    [Trait("Geofence", GeofenceTestCaseIds.GF010_SamePriority_SameDistance_SymmetricAroundUser_LowestIdWins)]
    public void GF010_SamePriority_EqualHaversineDistanceNorthSouth_LowestIdWins()
    {
        var user = (HubLat, HubLon);
        const int p = 1;
        const double r = 300;
        var north = new GeofencePoi(30, "N", OffsetMeters(HubLat, HubLon, 120).Lat, HubLon, r, p);
        var south = new GeofencePoi(20, "S", OffsetMeters(HubLat, HubLon, -120).Lat, HubLon, r, p);
        var list = new[] { north, south };

        var dN = GeofenceSelection.HaversineMeters(user.Item1, user.Item2, north.Latitude, north.Longitude);
        var dS = GeofenceSelection.HaversineMeters(user.Item1, user.Item2, south.Latitude, south.Longitude);
        Assert.Equal(dN, dS, 5); // cùng độ lệch ±120m đối xứng qua user → cùng Haversine (làm tròn 5 chữ số thập phân)

        var best = GeofenceSelection.FindBestInside(user.Item1, user.Item2, list, MinR);
        Assert.NotNull(best);
        Assert.Equal(20, best.Value.Id);
    }
}
