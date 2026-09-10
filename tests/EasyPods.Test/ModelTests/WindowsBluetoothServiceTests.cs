using EasyPods.Model.Bluetooth;
using EasyPods.Model.Entities;
using Xunit;

namespace EasyPods.Test.ModelTests;

public class WindowsBluetoothServiceTests
{
    [Fact]
    public void ProcessAdvertisement_ValidAirPodsPayload_AddsDeviceWithRssi()
    {
        using var service = new WindowsBluetoothService();
        AirPodsDevice? updatedDevice = null;
        service.AirPodsDiscoveredOrUpdated += (s, d) => updatedDevice = d;

        var payload = new byte[27];
        payload[0] = 0x07; // AirPods type
        payload[1] = 0x19;
        payload[2] = 0x0E; // Pro Gen 1
        payload[3] = 0x20;
        payload[5] = 0x89; // Left 80%, Right 90%
        payload[6] = 0xA0; // Case 100%
        payload[7] = 0b0000_0101; // Left + Case charging

        service.ProcessAdvertisement(0xAABBCCDDEEFF, "Test AirPods Pro", payload, rssi: -58);

        Assert.NotNull(updatedDevice);
        Assert.Equal(0xAABBCCDDEEFFUL, updatedDevice.BluetoothAddress);
        Assert.Equal("Test AirPods Pro", updatedDevice.Name);
        Assert.Equal(-58, updatedDevice.Rssi);
        Assert.Equal(AirPodsModelType.AirPodsProGen1, updatedDevice.Model);
        Assert.Equal(80, updatedDevice.Battery.LeftPodLevel);
        Assert.Equal(90, updatedDevice.Battery.RightPodLevel);
        Assert.Equal(100, updatedDevice.Battery.CaseLevel);
        Assert.True(updatedDevice.Battery.LeftCharging);
        Assert.False(updatedDevice.Battery.RightCharging);
        Assert.True(updatedDevice.Battery.CaseCharging);

        Assert.Single(service.DiscoveredAirPods);
    }

    [Fact]
    public void ProcessAdvertisement_SameDeviceUpdated_UpdatesWithoutDuplicates()
    {
        using var service = new WindowsBluetoothService();
        ulong address = 0x112233445566;

        var payload1 = new byte[27];
        payload1[0] = 0x07;
        payload1[1] = 0x19;
        payload1[2] = 0x0E;
        payload1[3] = 0x20;
        payload1[5] = 0x55; // 50%
        payload1[6] = 0x50; // 50%

        service.ProcessAdvertisement(address, "AirPods", payload1, rssi: -70);
        Assert.Single(service.DiscoveredAirPods);
        Assert.Equal(-70, service.DiscoveredAirPods[0].Rssi);
        Assert.Equal(50, service.DiscoveredAirPods[0].Battery.LeftPodLevel);

        var payload2 = new byte[27];
        payload2[0] = 0x07;
        payload2[1] = 0x19;
        payload2[2] = 0x0E;
        payload2[3] = 0x20;
        payload2[5] = 0x99; // 90%
        payload2[6] = 0x80; // 80%

        service.ProcessAdvertisement(address, "AirPods", payload2, rssi: -52);
        Assert.Single(service.DiscoveredAirPods);
        Assert.Equal(-52, service.DiscoveredAirPods[0].Rssi);
        Assert.Equal(90, service.DiscoveredAirPods[0].Battery.LeftPodLevel);
    }

    [Fact]
    public void ProcessAdvertisement_MalformedPayload_SafelyIgnored()
    {
        using var service = new WindowsBluetoothService();
        var shortPayload = new byte[5];

        service.ProcessAdvertisement(0x123456789ABC, "Broken Device", shortPayload, rssi: -80);

        Assert.Empty(service.DiscoveredAirPods);
    }

    [Fact]
    public void SetRadioState_UpdatesPropertyAndRaisesEvent()
    {
        using var service = new WindowsBluetoothService();
        bool? receivedState = null;
        service.BluetoothRadioStateChanged += (s, isEnabled) => receivedState = isEnabled;

        service.SetRadioState(false);

        Assert.False(service.IsRadioEnabled);
        Assert.False(receivedState);

        service.SetRadioState(true);
        Assert.True(service.IsRadioEnabled);
        Assert.True(receivedState);
    }

    [Fact]
    public async Task Dispose_CanBeCalledCleanly()
    {
        var service = new WindowsBluetoothService();
        service.Dispose();
        service.Dispose(); // Idempotent

        await Assert.ThrowsAsync<ObjectDisposedException>(() => service.StartMonitoringAsync());
    }
}
