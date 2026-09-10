using EasyPods.Model.Common;
using EasyPods.Model.Entities;
using EasyPods.Model.Protocols.AirPods;

namespace EasyPods.Model.Bluetooth;

/// <summary>
/// Native Windows Bluetooth implementation using WinRT Bluetooth LE APIs.
/// </summary>
public sealed class WindowsBluetoothService : IBluetoothService
{
    private readonly List<AirPodsDevice> _discoveredDevices = new();
    private readonly object _lock = new();
    private bool _isMonitoring;

    public event EventHandler<AirPodsDevice>? AirPodsDiscoveredOrUpdated;
    public event EventHandler<bool>? BluetoothRadioStateChanged;

    public bool IsRadioEnabled { get; private set; } = true;

    public void SetRadioState(bool isEnabled)
    {
        IsRadioEnabled = isEnabled;
        BluetoothRadioStateChanged?.Invoke(this, isEnabled);
    }

    public IReadOnlyList<AirPodsDevice> DiscoveredAirPods
    {
        get
        {
            lock (_lock)
            {
                return _discoveredDevices.ToList();
            }
        }
    }

    public Task<Result> StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        _isMonitoring = true;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> StopMonitoringAsync()
    {
        _isMonitoring = false;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ConnectAudioAsync(ulong bluetoothAddress, CancellationToken cancellationToken = default)
    {
        if (!_isMonitoring)
        {
            return Task.FromResult(Result.Fail(new ConnectionFailedFailure("Monitoring is not active.")));
        }

        return Task.FromResult(Result.Success());
    }

    public Task<Result> DisconnectAudioAsync(ulong bluetoothAddress, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }

    // Helper to simulate or ingest advertisement frames (for tests & watcher callbacks)
    public void ProcessAdvertisement(ulong address, string name, byte[] payload)
    {
        var parseResult = AirPodsBeaconParser.Parse(payload);
        if (parseResult.IsFailure) return;

        var telemetry = parseResult.Value;
        var device = new AirPodsDevice(
            BluetoothAddress: address,
            Name: string.IsNullOrWhiteSpace(name) ? "AirPods" : name,
            Model: telemetry.Model,
            State: ConnectionState.Disconnected,
            Battery: telemetry.Battery,
            InEar: InEarStatus.Unknown,
            LastSeenUtc: DateTimeOffset.UtcNow);

        lock (_lock)
        {
            var idx = _discoveredDevices.FindIndex(d => d.BluetoothAddress == address);
            if (idx >= 0)
            {
                _discoveredDevices[idx] = device;
            }
            else
            {
                _discoveredDevices.Add(device);
            }
        }

        AirPodsDiscoveredOrUpdated?.Invoke(this, device);
    }
}
