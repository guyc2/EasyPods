using EasyPods.Model.Entities;
using EasyPods.Model.Hardware;
using Xunit;

namespace EasyPods.Test.HardwareTests;

public class NoiseControlTests
{
    [Theory]
    [InlineData(AirPodsModelType.AirPodsGen4Anc, true)]
    [InlineData(AirPodsModelType.AirPodsProGen1, true)]
    [InlineData(AirPodsModelType.AirPodsProGen2Lightning, true)]
    [InlineData(AirPodsModelType.AirPodsProGen2UsbC, true)]
    [InlineData(AirPodsModelType.AirPodsMaxLightning, true)]
    [InlineData(AirPodsModelType.AirPodsMaxUsbC, true)]
    [InlineData(AirPodsModelType.AirPodsGen1, false)]
    [InlineData(AirPodsModelType.AirPodsGen2, false)]
    [InlineData(AirPodsModelType.AirPodsGen3, false)]
    [InlineData(AirPodsModelType.AirPodsGen4, false)]
    public void SupportsNoiseControl_IdentifiesHardwareCapabilities(AirPodsModelType model, bool expectedSupport)
    {
        Assert.Equal(expectedSupport, model.SupportsNoiseControl());
    }

    [Theory]
    [InlineData(AirPodsModelType.AirPodsGen4Anc, true)]
    [InlineData(AirPodsModelType.AirPodsProGen2UsbC, true)]
    [InlineData(AirPodsModelType.AirPodsProGen2Lightning, true)]
    [InlineData(AirPodsModelType.AirPodsProGen1, false)]
    [InlineData(AirPodsModelType.AirPodsMaxLightning, false)]
    [InlineData(AirPodsModelType.AirPodsGen3, false)]
    public void SupportsAdaptiveAudio_IdentifiesAdaptiveModels(AirPodsModelType model, bool expectedSupport)
    {
        Assert.Equal(expectedSupport, model.SupportsAdaptiveAudio());
    }

    [Fact]
    public async Task SetModeAsync_ReturnsSuccess_ForSupportedModel()
    {
        var service = new WindowsNoiseControlService();
        var result = await service.SetModeAsync(
            0x112233445566,
            AirPodsModelType.AirPodsProGen2UsbC,
            NoiseControlMode.ActiveNoiseCancellation);

        Assert.True(result.IsSuccess);
        Assert.Equal(NoiseControlMode.ActiveNoiseCancellation, service.GetCurrentMode(0x112233445566));
    }

    [Fact]
    public async Task SetModeAsync_ReturnsFailure_ForUnsupportedModel()
    {
        var service = new WindowsNoiseControlService();
        var result = await service.SetModeAsync(
            0x112233445566,
            AirPodsModelType.AirPodsGen2,
            NoiseControlMode.ActiveNoiseCancellation);

        Assert.True(result.IsFailure);
        Assert.Contains("does not support", result.Error.Message);
    }
}
