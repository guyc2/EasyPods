using EasyPods.Model.Common;
using EasyPods.Model.Entities;

namespace EasyPods.Model.Bluetooth;

public interface IBluetoothService : IDisposable, IAsyncDisposable
{
    event EventHandler<AirPodsDevice>? AirPodsDiscoveredOrUpdated;
    event EventHandler<bool>? BluetoothRadioStateChanged;

    bool IsRadioEnabled { get; }
    IReadOnlyList<AirPodsDevice> DiscoveredAirPods { get; }

    Task<Result> StartMonitoringAsync(CancellationToken cancellationToken = default);
    Task<Result> StopMonitoringAsync();
    Task<Result> ConnectAudioAsync(ulong bluetoothAddress, CancellationToken cancellationToken = default);
    Task<Result> DisconnectAudioAsync(ulong bluetoothAddress, CancellationToken cancellationToken = default);
}
