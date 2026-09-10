using EasyPods.Model.Entities;
using EasyPods.ViewModel.ViewModels;
using Xunit;

namespace EasyPods.Test.ViewModelTests;

public class AirPodsStatusViewModelTests
{
    [Theory]
    [InlineData(AirPodsModelType.AirPodsGen1, "AirPods (1st Gen)")]
    [InlineData(AirPodsModelType.AirPodsGen2, "AirPods (2nd Gen)")]
    [InlineData(AirPodsModelType.AirPodsGen3, "AirPods (3rd Gen)")]
    [InlineData(AirPodsModelType.AirPodsGen4, "AirPods 4")]
    [InlineData(AirPodsModelType.AirPodsGen4Anc, "AirPods 4 (Active Noise Cancellation)")]
    [InlineData(AirPodsModelType.AirPodsProGen1, "AirPods Pro (1st Gen)")]
    [InlineData(AirPodsModelType.AirPodsProGen2Lightning, "AirPods Pro 2 (Lightning)")]
    [InlineData(AirPodsModelType.AirPodsProGen2UsbC, "AirPods Pro 2 (MagSafe USB-C)")]
    [InlineData(AirPodsModelType.AirPodsMaxLightning, "AirPods Max (Lightning)")]
    [InlineData(AirPodsModelType.AirPodsMaxUsbC, "AirPods Max (USB-C 2024)")]
    [InlineData(AirPodsModelType.Unknown, "Apple AirPods")]
    public void FormatModelName_ReturnsExpectedFriendlyString(AirPodsModelType model, string expectedName)
    {
        var result = AirPodsStatusViewModel.FormatModelName(model);
        Assert.Equal(expectedName, result);
    }

    [Fact]
    public void UpdateFromDevice_SetsSensorAndPlacementStringsCorrectly()
    {
        var vm = new AirPodsStatusViewModel();

        var device = new AirPodsDevice(
            BluetoothAddress: 0x112233445566,
            Name: "Guy's AirPods 4 ANC",
            Model: AirPodsModelType.AirPodsGen4Anc,
            State: ConnectionState.Connected,
            Battery: new BatteryInfo(80, true, 90, false, 100, true, DateTimeOffset.UtcNow),
            InEar: new InEarStatus(LeftInEar: true, RightInEar: false, IsCaseLidOpen: true, DateTimeOffset.UtcNow),
            Rssi: -45,
            LastSeenUtc: DateTimeOffset.UtcNow);

        vm.UpdateFromDevice(device);

        Assert.Equal(AirPodsModelType.AirPodsGen4Anc, vm.ModelType);
        Assert.Equal("AirPods 4 (Active Noise Cancellation)", vm.ModelDisplay);
        Assert.True(vm.LeftInEar);
        Assert.False(vm.RightInEar);
        Assert.True(vm.IsCaseLidOpen);
        Assert.Equal("👂 In Ear", vm.LeftPlacementText);
        Assert.Equal("📦 In Case", vm.RightPlacementText);
        Assert.Equal("📂 Lid Open", vm.CaseLidText);
        Assert.True(vm.LeftPodCharging);
        Assert.False(vm.RightPodCharging);
        Assert.True(vm.CaseCharging);
    }
}
