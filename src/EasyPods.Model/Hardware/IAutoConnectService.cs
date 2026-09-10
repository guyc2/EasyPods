using EasyPods.Model.Common;
using EasyPods.Model.Entities;

namespace EasyPods.Model.Hardware;

public interface IAutoConnectService : IDisposable
{
    event EventHandler<ulong>? AutoConnectInitiated;

    bool IsAutoConnectEnabled { get; set; }
    short RssiProximityThreshold { get; set; }

    Task<Result> EvaluateAdvertisementForAutoConnectAsync(AirPodsDevice device, CancellationToken cancellationToken = default);
}
