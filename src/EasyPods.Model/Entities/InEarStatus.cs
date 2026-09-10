namespace EasyPods.Model.Entities;

/// <summary>
/// Real-time placement and case lid telemetry broadcast by Apple AirPods beacons.
/// </summary>
public sealed record InEarStatus(
    bool? LeftInEar,
    bool? RightInEar,
    bool? IsCaseLidOpen,
    DateTimeOffset LastUpdatedUtc)
{
    public static InEarStatus Unknown => new(null, null, null, DateTimeOffset.UtcNow);

    public bool HasAnySensorData => LeftInEar.HasValue || RightInEar.HasValue || IsCaseLidOpen.HasValue;
}
