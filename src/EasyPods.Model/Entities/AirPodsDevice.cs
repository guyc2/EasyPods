namespace EasyPods.Model.Entities;

/// <summary>
/// Domain entity representing an Apple AirPods device.
/// </summary>
public sealed record AirPodsDevice(
    ulong BluetoothAddress,
    string Name,
    AirPodsModelType Model,
    ConnectionState State,
    BatteryInfo Battery,
    InEarStatus InEar,
    DateTimeOffset LastSeenUtc)
{
    public string FormattedMacAddress => string.Format(
        "{0:X2}:{1:X2}:{2:X2}:{3:X2}:{4:X2}:{5:X2}",
        (BluetoothAddress >> 40) & 0xFF,
        (BluetoothAddress >> 32) & 0xFF,
        (BluetoothAddress >> 24) & 0xFF,
        (BluetoothAddress >> 16) & 0xFF,
        (BluetoothAddress >> 8) & 0xFF,
        BluetoothAddress & 0xFF);
}
