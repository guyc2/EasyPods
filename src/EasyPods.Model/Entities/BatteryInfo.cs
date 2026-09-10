namespace EasyPods.Model.Entities;

/// <summary>
/// Immutable snapshot of battery telemetry for AirPods.
/// Levels are 0-100%, or null if disconnected/absent.
/// </summary>
public sealed record BatteryInfo(
    int? LeftPodLevel,
    bool LeftCharging,
    int? RightPodLevel,
    bool RightCharging,
    int? CaseLevel,
    bool CaseCharging,
    DateTimeOffset LastUpdatedUtc)
{
    public static BatteryInfo Empty => new(
        LeftPodLevel: null,
        LeftCharging: false,
        RightPodLevel: null,
        RightCharging: false,
        CaseLevel: null,
        CaseCharging: false,
        LastUpdatedUtc: DateTimeOffset.UtcNow);

    public bool HasAnyData => LeftPodLevel.HasValue || RightPodLevel.HasValue || CaseLevel.HasValue;
}
