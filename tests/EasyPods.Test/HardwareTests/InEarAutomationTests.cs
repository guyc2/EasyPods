using EasyPods.Model.Entities;
using EasyPods.Model.Hardware;
using Xunit;

namespace EasyPods.Test.HardwareTests;

public class InEarAutomationTests
{
    [Fact]
    public void ProcessDeviceTelemetry_TriggersAutoPause_WhenPodRemovedFromEar()
    {
        int dispatchCount = 0;
        using var service = new WindowsInEarAutomationService(
            mediaDispatcherOverride: () => dispatchCount++,
            debounceInterval: TimeSpan.Zero);

        bool? lastToggledState = null;
        service.PlaybackStateAutoToggled += (s, isPlaying) => lastToggledState = isPlaying;

        var inEarDevice = new AirPodsDevice(
            BluetoothAddress: 0x112233445566,
            Name: "Guy's AirPods Pro",
            Model: AirPodsModelType.AirPodsProGen2UsbC,
            State: ConnectionState.Connected,
            Battery: new BatteryInfo(80, false, 80, false, 100, false, DateTimeOffset.UtcNow),
            InEar: new InEarStatus(LeftInEar: true, RightInEar: true, IsCaseLidOpen: false, DateTimeOffset.UtcNow),
            LastSeenUtc: DateTimeOffset.UtcNow);

        // First event: AirPods in ear
        service.ProcessDeviceTelemetry(inEarDevice);
        Assert.Equal(0, dispatchCount);
        Assert.False(service.HasAutoPaused);

        // Second event: Pods removed from ear
        var outOfEarDevice = inEarDevice with
        {
            InEar = new InEarStatus(LeftInEar: false, RightInEar: false, IsCaseLidOpen: false, DateTimeOffset.UtcNow)
        };

        service.ProcessDeviceTelemetry(outOfEarDevice);

        Assert.Equal(1, dispatchCount);
        Assert.True(service.HasAutoPaused);
        Assert.Equal(false, lastToggledState);
    }

    [Fact]
    public void ProcessDeviceTelemetry_TriggersAutoResume_WhenPodPlacedBackInEar()
    {
        int dispatchCount = 0;
        using var service = new WindowsInEarAutomationService(
            mediaDispatcherOverride: () => dispatchCount++,
            debounceInterval: TimeSpan.Zero);

        bool? lastToggledState = null;
        service.PlaybackStateAutoToggled += (s, isPlaying) => lastToggledState = isPlaying;

        var baseDevice = new AirPodsDevice(
            BluetoothAddress: 0x112233445566,
            Name: "Guy's AirPods Pro",
            Model: AirPodsModelType.AirPodsProGen2UsbC,
            State: ConnectionState.Connected,
            Battery: new BatteryInfo(80, false, 80, false, 100, false, DateTimeOffset.UtcNow),
            InEar: new InEarStatus(LeftInEar: true, RightInEar: true, IsCaseLidOpen: false, DateTimeOffset.UtcNow),
            LastSeenUtc: DateTimeOffset.UtcNow);

        // 1. In ear
        service.ProcessDeviceTelemetry(baseDevice);
        // 2. Remove from ear -> auto pause
        service.ProcessDeviceTelemetry(baseDevice with
        {
            InEar = new InEarStatus(LeftInEar: false, RightInEar: false, IsCaseLidOpen: false, DateTimeOffset.UtcNow)
        });
        Assert.Equal(1, dispatchCount);
        Assert.True(service.HasAutoPaused);

        // 3. Put back in ear -> auto resume
        service.ProcessDeviceTelemetry(baseDevice with
        {
            InEar = new InEarStatus(LeftInEar: true, RightInEar: false, IsCaseLidOpen: false, DateTimeOffset.UtcNow)
        });

        Assert.Equal(2, dispatchCount);
        Assert.False(service.HasAutoPaused);
        Assert.Equal(true, lastToggledState);
    }

    [Fact]
    public void ProcessDeviceTelemetry_DoesNothing_WhenAutoPauseDisabled()
    {
        int dispatchCount = 0;
        using var service = new WindowsInEarAutomationService(
            mediaDispatcherOverride: () => dispatchCount++,
            debounceInterval: TimeSpan.Zero)
        {
            IsAutoPauseEnabled = false
        };

        var inEarDevice = new AirPodsDevice(
            BluetoothAddress: 0x112233445566,
            Name: "Guy's AirPods Pro",
            Model: AirPodsModelType.AirPodsProGen2UsbC,
            State: ConnectionState.Connected,
            Battery: new BatteryInfo(80, false, 80, false, 100, false, DateTimeOffset.UtcNow),
            InEar: new InEarStatus(LeftInEar: true, RightInEar: true, IsCaseLidOpen: false, DateTimeOffset.UtcNow),
            LastSeenUtc: DateTimeOffset.UtcNow);

        service.ProcessDeviceTelemetry(inEarDevice);
        service.ProcessDeviceTelemetry(inEarDevice with
        {
            InEar = new InEarStatus(LeftInEar: false, RightInEar: false, IsCaseLidOpen: false, DateTimeOffset.UtcNow)
        });

        Assert.Equal(0, dispatchCount);
        Assert.False(service.HasAutoPaused);
    }

    [Fact]
    public async Task SendPlayPauseMediaKeyAsync_ExecutesSuccessfully()
    {
        int dispatchCount = 0;
        using var service = new WindowsInEarAutomationService(mediaDispatcherOverride: () => dispatchCount++);

        var result = await service.SendPlayPauseMediaKeyAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, dispatchCount);
    }
}
