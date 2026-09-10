using EasyPods.Model.Common;
using EasyPods.Model.Entities;

namespace EasyPods.Model.Hardware;

public interface IInEarAutomationService : IDisposable
{
    event EventHandler<bool>? PlaybackStateAutoToggled;

    bool IsAutoPauseEnabled { get; set; }
    bool HasAutoPaused { get; }

    void ProcessDeviceTelemetry(AirPodsDevice device);
    Task<Result> SendPlayPauseMediaKeyAsync();
}
