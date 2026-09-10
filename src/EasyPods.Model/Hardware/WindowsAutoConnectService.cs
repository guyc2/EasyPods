using System.Collections.Concurrent;
using EasyPods.Model.Bluetooth;
using EasyPods.Model.Common;
using EasyPods.Model.Entities;

namespace EasyPods.Model.Hardware;

/// <summary>
/// Hardware automation service that triggers Windows Bluetooth connection
/// automatically when AirPods case lid is flipped open nearby.
/// </summary>
public sealed class WindowsAutoConnectService : IAutoConnectService
{
    private readonly IBluetoothService _bluetoothService;
    private readonly ConcurrentDictionary<ulong, DateTimeOffset> _lastConnectAttempt = new();
    private readonly TimeSpan _reconnectCooldown;
    private bool _isDisposed;

    public event EventHandler<ulong>? AutoConnectInitiated;

    public bool IsAutoConnectEnabled { get; set; } = true;
    public short RssiProximityThreshold { get; set; } = -75;

    public WindowsAutoConnectService(
        IBluetoothService bluetoothService,
        TimeSpan? reconnectCooldown = null)
    {
        _bluetoothService = bluetoothService ?? throw new ArgumentNullException(nameof(bluetoothService));
        _reconnectCooldown = reconnectCooldown ?? TimeSpan.FromSeconds(10);
    }

    public async Task<Result> EvaluateAdvertisementForAutoConnectAsync(
        AirPodsDevice device,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (!IsAutoConnectEnabled)
        {
            return Result.Success();
        }

        // Must have case lid explicitly open
        if (device.InEar.IsCaseLidOpen != true)
        {
            return Result.Success();
        }

        // Already connected
        if (device.State == ConnectionState.Connected)
        {
            return Result.Success();
        }

        // Signal strength proximity check
        if (device.Rssi < RssiProximityThreshold && device.Rssi != 0)
        {
            AppLogger.Debug($"Skipping auto-connect for 0x{device.BluetoothAddress:X12}: RSSI ({device.Rssi} dBm) below threshold ({RssiProximityThreshold} dBm).", nameof(WindowsAutoConnectService));
            return Result.Success();
        }

        // Cooldown enforcement
        var now = DateTimeOffset.UtcNow;
        if (_lastConnectAttempt.TryGetValue(device.BluetoothAddress, out var lastAttempt))
        {
            if (now - lastAttempt < _reconnectCooldown)
            {
                return Result.Success();
            }
        }

        _lastConnectAttempt[device.BluetoothAddress] = now;
        AppLogger.Info($"Case lid open detected in proximity for 0x{device.BluetoothAddress:X12} (RSSI: {device.Rssi} dBm). Initiating Auto-Connect.", nameof(WindowsAutoConnectService));

        AutoConnectInitiated?.Invoke(this, device.BluetoothAddress);
        return await _bluetoothService.ConnectAudioAsync(device.BluetoothAddress, cancellationToken);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _lastConnectAttempt.Clear();
    }
}
