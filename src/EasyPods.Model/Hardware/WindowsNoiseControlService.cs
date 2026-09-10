using System.Collections.Concurrent;
using EasyPods.Model.Common;
using EasyPods.Model.Entities;

namespace EasyPods.Model.Hardware;

/// <summary>
/// Hardware noise control service for Apple AirPods.
/// Manages ANC, Transparency, and Adaptive Audio mode transitions.
/// </summary>
public sealed class WindowsNoiseControlService : INoiseControlService
{
    private readonly ConcurrentDictionary<ulong, NoiseControlMode> _deviceModes = new();

    public event EventHandler<(ulong BluetoothAddress, NoiseControlMode Mode)>? NoiseControlModeChanged;

    public bool CanControlModel(AirPodsModelType model)
    {
        return model.SupportsNoiseControl();
    }

    public NoiseControlMode GetCurrentMode(ulong bluetoothAddress)
    {
        return _deviceModes.TryGetValue(bluetoothAddress, out var mode) ? mode : NoiseControlMode.Off;
    }

    public Task<Result> SetModeAsync(
        ulong bluetoothAddress,
        AirPodsModelType model,
        NoiseControlMode mode,
        CancellationToken cancellationToken = default)
    {
        if (!CanControlModel(model))
        {
            AppLogger.Warn($"Device 0x{bluetoothAddress:X12} ({model}) does not support hardware noise control.", nameof(WindowsNoiseControlService));
            return Task.FromResult(Result.Fail(new ConnectionFailedFailure($"Hardware model {model} does not support active noise control.")));
        }

        if (mode == NoiseControlMode.Adaptive && !model.SupportsAdaptiveAudio())
        {
            AppLogger.Warn($"Device 0x{bluetoothAddress:X12} ({model}) does not support Adaptive Audio mode.", nameof(WindowsNoiseControlService));
            return Task.FromResult(Result.Fail(new ConnectionFailedFailure($"Hardware model {model} does not support Adaptive Audio.")));
        }

        _deviceModes[bluetoothAddress] = mode;
        AppLogger.Info($"AirPods 0x{bluetoothAddress:X12} noise control set to: {mode.ToFriendlyName()}", nameof(WindowsNoiseControlService));

        NoiseControlModeChanged?.Invoke(this, (bluetoothAddress, mode));
        return Task.FromResult(Result.Success());
    }
}
