namespace EasyPods.Model.Common;

/// <summary>
/// Base class representing a typed domain or infrastructure failure.
/// Swallowing exceptions or returning null on error is strictly prohibited.
/// </summary>
public abstract record Failure(string Message, Exception? Exception = null)
{
    public override string ToString() => Exception is null 
        ? $"{GetType().Name}: {Message}" 
        : $"{GetType().Name}: {Message} (Inner: {Exception.Message})";
}

public sealed record BluetoothUnavailableFailure(string Message = "Bluetooth adapter is turned off or not present.", Exception? Exception = null)
    : Failure(Message, Exception);

public sealed record DeviceNotFoundFailure(string Message = "AirPods device could not be found or is out of range.", Exception? Exception = null)
    : Failure(Message, Exception);

public sealed record ConnectionFailedFailure(string Message = "Failed to establish Bluetooth connection with AirPods.", Exception? Exception = null)
    : Failure(Message, Exception);

public sealed record BeaconDecodeFailure(string Message = "Failed to parse Apple BLE advertisement beacon payload.", Exception? Exception = null)
    : Failure(Message, Exception);

public sealed record AudioRoutingFailure(string Message = "Failed to switch Windows default audio playback device.", Exception? Exception = null)
    : Failure(Message, Exception);
