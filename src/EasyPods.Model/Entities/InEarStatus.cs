namespace EasyPods.Model.Entities;

public sealed record InEarStatus(
    bool? LeftInEar,
    bool? RightInEar,
    DateTimeOffset LastUpdatedUtc)
{
    public static InEarStatus Unknown => new(null, null, DateTimeOffset.UtcNow);
}
