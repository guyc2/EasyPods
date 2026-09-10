using EasyPods.Model.Common;

namespace EasyPods.Model.Hardware;

public interface IAudioEndpointService : IDisposable
{
    event EventHandler<string>? DefaultPlaybackDeviceChanged;

    string? CurrentDefaultEndpointId { get; }
    string? PreviousDefaultEndpointId { get; }

    Task<IReadOnlyList<AudioEndpointInfo>> GetAvailableEndpointsAsync(CancellationToken cancellationToken = default);
    Task<Result> SetDefaultPlaybackDeviceAsync(string deviceNameOrSubstring, CancellationToken cancellationToken = default);
    Task<Result> RestorePreviousDefaultDeviceAsync(CancellationToken cancellationToken = default);
}

public sealed record AudioEndpointInfo(
    string Id,
    string Name,
    bool IsDefault);
