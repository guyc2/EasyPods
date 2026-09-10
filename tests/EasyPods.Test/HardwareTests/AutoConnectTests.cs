using EasyPods.Model.Bluetooth;
using EasyPods.Model.Entities;
using EasyPods.Model.Hardware;
using Xunit;

namespace EasyPods.Test.HardwareTests;

public class AutoConnectTests
{
    [Fact]
    public async Task Evaluate_TriggersConnection_WhenLidOpenNearby()
    {
        using var btService = new WindowsBluetoothService();
        using var autoConnect = new WindowsAutoConnectService(btService);

        ulong triggeredAddress = 0;
        autoConnect.AutoConnectInitiated += (s, addr) => triggeredAddress = addr;

        var device = new AirPodsDevice(
            BluetoothAddress: 0x112233445566,
            Name: "Guy's AirPods 4",
            Model: AirPodsModelType.AirPodsGen4,
            State: ConnectionState.Disconnected,
            Battery: new BatteryInfo(90, false, 90, false, 80, false, DateTimeOffset.UtcNow),
            InEar: new InEarStatus(LeftInEar: false, RightInEar: false, IsCaseLidOpen: true, DateTimeOffset.UtcNow),
            LastSeenUtc: DateTimeOffset.UtcNow,
            Rssi: -60);

        var result = await autoConnect.EvaluateAdvertisementForAutoConnectAsync(device);

        Assert.True(result.IsSuccess || result.IsFailure);
        Assert.Equal(0x112233445566UL, triggeredAddress);
    }

    [Fact]
    public async Task Evaluate_DoesNothing_WhenLidClosed()
    {
        using var btService = new WindowsBluetoothService();
        using var autoConnect = new WindowsAutoConnectService(btService);

        ulong triggeredAddress = 0;
        autoConnect.AutoConnectInitiated += (s, addr) => triggeredAddress = addr;

        var device = new AirPodsDevice(
            BluetoothAddress: 0x112233445566,
            Name: "Guy's AirPods 4",
            Model: AirPodsModelType.AirPodsGen4,
            State: ConnectionState.Disconnected,
            Battery: new BatteryInfo(90, false, 90, false, 80, false, DateTimeOffset.UtcNow),
            InEar: new InEarStatus(LeftInEar: false, RightInEar: false, IsCaseLidOpen: false, DateTimeOffset.UtcNow),
            LastSeenUtc: DateTimeOffset.UtcNow,
            Rssi: -50);

        var result = await autoConnect.EvaluateAdvertisementForAutoConnectAsync(device);

        Assert.True(result.IsSuccess);
        Assert.Equal(0UL, triggeredAddress);
    }

    [Fact]
    public async Task Evaluate_DoesNothing_WhenAutoConnectDisabled()
    {
        using var btService = new WindowsBluetoothService();
        using var autoConnect = new WindowsAutoConnectService(btService)
        {
            IsAutoConnectEnabled = false
        };

        ulong triggeredAddress = 0;
        autoConnect.AutoConnectInitiated += (s, addr) => triggeredAddress = addr;

        var device = new AirPodsDevice(
            BluetoothAddress: 0x112233445566,
            Name: "Guy's AirPods 4",
            Model: AirPodsModelType.AirPodsGen4,
            State: ConnectionState.Disconnected,
            Battery: new BatteryInfo(90, false, 90, false, 80, false, DateTimeOffset.UtcNow),
            InEar: new InEarStatus(LeftInEar: false, RightInEar: false, IsCaseLidOpen: true, DateTimeOffset.UtcNow),
            LastSeenUtc: DateTimeOffset.UtcNow,
            Rssi: -50);

        var result = await autoConnect.EvaluateAdvertisementForAutoConnectAsync(device);

        Assert.True(result.IsSuccess);
        Assert.Equal(0UL, triggeredAddress);
    }
}
