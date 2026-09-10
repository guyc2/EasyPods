using EasyPods.Model.Common;
using EasyPods.Model.Entities;

namespace EasyPods.Model.Protocols.AirPods;

/// <summary>
/// Decodes Apple BLE Manufacturer Advertisements (Company ID 0x004C, Type 0x07) into AirPods telemetry.
/// </summary>
public static class AirPodsBeaconParser
{
    public const ushort AppleCompanyId = 0x004C;
    public const byte AirPodsBeaconType = 0x07;

    public static Result<AirPodsTelemetry> Parse(byte[]? payload, DateTimeOffset? timestamp = null)
    {
        if (payload is null || payload.Length < 16)
        {
            return Result<AirPodsTelemetry>.Fail(
                new BeaconDecodeFailure($"Payload is null or too short ({payload?.Length ?? 0} bytes, expected at least 16)."));
        }

        // Check beacon type if present at index 0
        if (payload[0] != AirPodsBeaconType)
        {
            return Result<AirPodsTelemetry>.Fail(
                new BeaconDecodeFailure($"Unexpected beacon type 0x{payload[0]:X2}, expected 0x07."));
        }

        try
        {
            var now = timestamp ?? DateTimeOffset.UtcNow;

            // Model identification
            // Typically at offset 1 or 2 depending on header framing
            var model = DecodeModel(payload);

            // In Apple AirPods beacons, battery levels are represented in nibbles:
            // 0-10: 0% - 100% (each step is 10%)
            // 15 (0x0F): disconnected or unavailable
            // Charging flags are encoded in bits

            int? leftLevel = null;
            int? rightLevel = null;
            int? caseLevel = null;
            bool leftCharging = false;
            bool rightCharging = false;
            bool caseCharging = false;

            // Standard 27-byte beacon format
            if (payload.Length >= 20)
            {
                // Offsets based on standard AirPods BLE advertisement structure:
                // byte 5 or 6 contains Pod battery nibbles
                // byte 6 or 7 contains Case battery and charging flags
                byte podByte = payload[5];
                byte caseByte = payload[6];
                byte chargingByte = payload[7];

                int rawLeft = (podByte >> 4) & 0x0F;
                int rawRight = podByte & 0x0F;
                int rawCase = (caseByte >> 4) & 0x0F;

                if (rawLeft <= 10)
                {
                    leftLevel = rawLeft * 10;
                }
                if (rawRight <= 10)
                {
                    rightLevel = rawRight * 10;
                }
                if (rawCase <= 10)
                {
                    caseLevel = rawCase * 10;
                }

                leftCharging = (chargingByte & 0b0000_0001) != 0;
                rightCharging = (chargingByte & 0b0000_0010) != 0;
                caseCharging = (chargingByte & 0b0000_0100) != 0;
            }

            var battery = new BatteryInfo(
                LeftPodLevel: leftLevel,
                LeftCharging: leftCharging,
                RightPodLevel: rightLevel,
                RightCharging: rightCharging,
                CaseLevel: caseLevel,
                CaseCharging: caseCharging,
                LastUpdatedUtc: now);

            return Result<AirPodsTelemetry>.Success(new AirPodsTelemetry(model, battery));
        }
        catch (Exception ex)
        {
            return Result<AirPodsTelemetry>.Fail(
                new BeaconDecodeFailure("Unexpected exception while parsing beacon payload.", ex));
        }
    }

    private static AirPodsModelType DecodeModel(byte[] payload)
    {
        if (payload.Length < 4) return AirPodsModelType.Unknown;

        ushort deviceId = (ushort)((payload[2] << 8) | payload[3]);

        return deviceId switch
        {
            0x0220 => AirPodsModelType.AirPodsGen1,
            0x0F20 => AirPodsModelType.AirPodsGen2,
            0x1320 => AirPodsModelType.AirPodsGen3,
            0x1720 => AirPodsModelType.AirPodsGen4,
            0x0E20 => AirPodsModelType.AirPodsProGen1,
            0x1420 => AirPodsModelType.AirPodsProGen2,
            0x0A20 => AirPodsModelType.AirPodsMax,
            _ => AirPodsModelType.Unknown
        };
    }
}

public sealed record AirPodsTelemetry(
    AirPodsModelType Model,
    BatteryInfo Battery);
