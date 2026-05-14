namespace HeriStepAI.Geo.Tests;

/// <summary>Trait: <c>[Trait("Geofence", Id)]</c> — lọc: <c>dotnet test --filter "Geofence=GF-001"</c></summary>
public static class GeofenceTestCaseIds
{
    public const string GF001_ThreeSamePriorityOverlap_NearestWins = "GF-001";
    public const string GF002_FiveSamePriorityDifferentDistance = "GF-002";
    public const string GF003_PriorityBeatsDistance = "GF-003";
    public const string GF004_OutsideAll_Null = "GF-004";
    public const string GF005_MinRadiusApplies = "GF-005";
    public const string GF006_MixedPriorities_SubsetCompetesByDistance = "GF-006";
    public const string GF007_EmptyPoiList_Null = "GF-007";
    public const string GF008_SamePriority_TwoOverlappingRings_NearestCenterWins = "GF-008";
    public const string GF009_SamePriority_SameDistance_CoincidentCenters_LowestIdWins = "GF-009";
    public const string GF010_SamePriority_SameDistance_SymmetricAroundUser_LowestIdWins = "GF-010";
    public const string GF011_SameDistance_DifferentPriority_HigherPriorityWins = "GF-011";

    public static IReadOnlyList<string> All { get; } =
    [
        GF001_ThreeSamePriorityOverlap_NearestWins,
        GF002_FiveSamePriorityDifferentDistance,
        GF003_PriorityBeatsDistance,
        GF004_OutsideAll_Null,
        GF005_MinRadiusApplies,
        GF006_MixedPriorities_SubsetCompetesByDistance,
        GF007_EmptyPoiList_Null,
        GF008_SamePriority_TwoOverlappingRings_NearestCenterWins,
        GF009_SamePriority_SameDistance_CoincidentCenters_LowestIdWins,
        GF010_SamePriority_SameDistance_SymmetricAroundUser_LowestIdWins,
        GF011_SameDistance_DifferentPriority_HigherPriorityWins
    ];
}
