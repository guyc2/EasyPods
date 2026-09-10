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
}
