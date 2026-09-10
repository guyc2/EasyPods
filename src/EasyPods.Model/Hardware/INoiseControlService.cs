using EasyPods.Model.Common;
using EasyPods.Model.Entities;

namespace EasyPods.Model.Hardware;

public interface INoiseControlService
{
    event EventHandler<(ulong BluetoothAddress, NoiseControlMode Mode)>? NoiseControlModeChanged;

    bool CanControlModel(AirPodsModelType model);
    NoiseControlMode GetCurrentMode(ulong bluetoothAddress);
    Task<Result> SetModeAsync(ulong bluetoothAddress, AirPodsModelType model, NoiseControlMode mode, CancellationToken cancellationToken = default);
}
