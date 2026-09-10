using EasyPods.Model.Entities;
using EasyPods.Model.Protocols.AirPods;
using Xunit;

namespace EasyPods.Test.ModelTests;

public class AirPodsBeaconParserTests
{
    [Fact]
    public void Parse_ReturnsFailure_WhenPayloadIsNull()
    {
        var result = AirPodsBeaconParser.Parse(null);

        Assert.True(result.IsFailure);
        Assert.Contains("null or too short", result.Error.Message);
    }

    [Fact]
    public void Parse_ReturnsFailure_WhenBeaconTypeIsWrong()
    {
        var payload = new byte[20];
        payload[0] = 0x05; // Not 0x07

        var result = AirPodsBeaconParser.Parse(payload);

        Assert.True(result.IsFailure);
        Assert.Contains("Unexpected beacon type", result.Error.Message);
    }

    [Fact]
    public void Parse_DecodesAirPodsProAndBatteryLevels()
    {
        var payload = new byte[27];
        payload[0] = 0x07; // AirPods type
        payload[1] = 0x19; // Length
        payload[2] = 0x0E; // Device ID high (AirPods Pro Gen 1)
        payload[3] = 0x20; // Device ID low
        payload[4] = 0x00;
        payload[5] = 0x89; // Left pod: 8 (80%), Right pod: 9 (90%)
        payload[6] = 0xA0; // Case: 10 (100%)
        payload[7] = 0b0000_0101; // Left charging + Case charging

        var result = AirPodsBeaconParser.Parse(payload);

        Assert.True(result.IsSuccess);
        Assert.Equal(AirPodsModelType.AirPodsProGen1, result.Value.Model);

        var battery = result.Value.Battery;
        Assert.Equal(80, battery.LeftPodLevel);
        Assert.True(battery.LeftCharging);
        Assert.Equal(90, battery.RightPodLevel);
        Assert.False(battery.RightCharging);
        Assert.Equal(100, battery.CaseLevel);
        Assert.True(battery.CaseCharging);
    }

    [Theory]
    [InlineData(0x17, 0x20, AirPodsModelType.AirPodsGen4)]
    [InlineData(0x20, 0x17, AirPodsModelType.AirPodsGen4)]
    [InlineData(0x1B, 0x20, AirPodsModelType.AirPodsGen4Anc)]
    [InlineData(0x24, 0x20, AirPodsModelType.AirPodsProGen2UsbC)]
    [InlineData(0x20, 0x24, AirPodsModelType.AirPodsProGen2UsbC)]
    [InlineData(0x27, 0x20, AirPodsModelType.AirPodsMaxUsbC)]
    [InlineData(0x0A, 0x20, AirPodsModelType.AirPodsMaxLightning)]
    [InlineData(0x13, 0x20, AirPodsModelType.AirPodsGen3)]
    [InlineData(0x0F, 0x20, AirPodsModelType.AirPodsGen2)]
    public void Parse_IdentifiesModernAirPodsModelsCorrectly(byte b2, byte b3, AirPodsModelType expectedModel)
    {
        var payload = new byte[27];
        payload[0] = 0x07;
        payload[1] = 0x19;
        payload[2] = b2;
        payload[3] = b3;
        payload[5] = 0x88;
        payload[6] = 0x80;

        var result = AirPodsBeaconParser.Parse(payload);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedModel, result.Value.Model);
    }

    [Fact]
    public void Parse_DecodesInEarAndCaseLidStatus()
    {
        var payload = new byte[27];
        payload[0] = 0x07;
        payload[1] = 0x19;
        payload[2] = 0x17;
        payload[3] = 0x20;
        payload[5] = 0xAA; // 100% Left & Right
        // Byte 6: Upper nibble 0x9 (90% case), Lower nibble: 0b0111 (Left in-ear = 1, Right in-ear = 1, Lid open = 1)
        payload[6] = 0x97; 

        var result = AirPodsBeaconParser.Parse(payload);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.InEar.LeftInEar);
        Assert.True(result.Value.InEar.RightInEar);
        Assert.True(result.Value.InEar.IsCaseLidOpen);
        Assert.True(result.Value.InEar.HasAnySensorData);
    }

    [Theory]
    [InlineData("Guy's AirPods 4", AirPodsModelType.AirPodsGen4)]
    [InlineData("Guy's AirPods 4 with Active Noise Cancellation", AirPodsModelType.AirPodsGen4Anc)]
    [InlineData("AirPods Pro 2 USB-C", AirPodsModelType.AirPodsProGen2UsbC)]
    [InlineData("AirPods Max USB-C", AirPodsModelType.AirPodsMaxUsbC)]
    [InlineData("AirPods Pro (2nd generation)", AirPodsModelType.AirPodsProGen2Lightning)]
    [InlineData("AirPods (3rd generation)", AirPodsModelType.AirPodsGen3)]
    public void Parse_InfersModelFromName_WhenProductIdUnknown(string advertisedName, AirPodsModelType expectedModel)
    {
        var payload = new byte[27];
        payload[0] = 0x07;
        payload[1] = 0x19;
        payload[2] = 0xFF; // Unknown PID
        payload[3] = 0xFF;

        var result = AirPodsBeaconParser.Parse(payload, advertisedName: advertisedName);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedModel, result.Value.Model);
    }
}
