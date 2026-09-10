using EasyPods.Model.Common;
using EasyPods.Model.Entities;

namespace EasyPods.Model.Protocols.AirPods;

/// <summary>
/// Decodes Apple BLE Manufacturer Advertisements (Company ID 0x004C, Type 0x07) into AirPods telemetry.
/// Supports all historical and modern AirPods models (including AirPods 4, Pro 2 USB-C, and Max USB-C).
/// </summary>
public static class AirPodsBeaconParser
{
    public const ushort AppleCompanyId = 0x004C;
    public const byte AirPodsBeaconType = 0x07;

    public static Result<AirPodsTelemetry> Parse(byte[]? payload, DateTimeOffset? timestamp = null, string? advertisedName = null)
    {
        if (payload is null || payload.Length < 16)
        {
            return Result<AirPodsTelemetry>.Fail(
                new BeaconDecodeFailure($"Payload is null or too short ({payload?.Length ?? 0} bytes, expected at least 16)."));
        }

        // Check beacon type at index 0 (0x07 represents AirPods proximity packet)
        if (payload[0] != AirPodsBeaconType)
        {
            return Result<AirPodsTelemetry>.Fail(
                new BeaconDecodeFailure($"Unexpected beacon type 0x{payload[0]:X2}, expected 0x07."));
        }

        try
        {
            var now = timestamp ?? DateTimeOffset.UtcNow;

            // 1. Model identification (PID lookup + name fallback)
            var model = DecodeModel(payload, advertisedName);

            // 2. Battery & Charging telemetry extraction
            int? leftLevel = null;
            int? rightLevel = null;
            int? caseLevel = null;
            bool leftCharging = false;
            bool rightCharging = false;
            bool caseCharging = false;

            bool? leftInEar = null;
            bool? rightInEar = null;
            bool? isCaseOpen = null;

            if (payload.Length >= 8)
            {
                byte podByte = payload[5];
                byte caseAndSensorByte = payload[6];
                byte chargingByte = payload[7];

                int rawLeft = (podByte >> 4) & 0x0F;
                int rawRight = podByte & 0x0F;
                int rawCase = (caseAndSensorByte >> 4) & 0x0F;

                // Battery nibble: 0-10 represents 0%-100%. 15 (0x0F) means disconnected / absent
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

                // In-Ear & Case Flip flags (lower nibble of byte 6)
                byte sensorBits = (byte)(caseAndSensorByte & 0x0F);
                leftInEar = (sensorBits & 0b0000_0001) != 0;
                rightInEar = (sensorBits & 0b0000_0010) != 0;
                isCaseOpen = (sensorBits & 0b0000_0100) != 0;

                // Charging flags (byte 7)
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

            var inEar = new InEarStatus(
                LeftInEar: leftInEar,
                RightInEar: rightInEar,
                IsCaseLidOpen: isCaseOpen,
                LastUpdatedUtc: now);

            return Result<AirPodsTelemetry>.Success(new AirPodsTelemetry(model, battery, inEar));
        }
        catch (Exception ex)
        {
            return Result<AirPodsTelemetry>.Fail(
                new BeaconDecodeFailure("Unexpected exception while parsing beacon payload.", ex));
        }
    }

    private static AirPodsModelType DecodeModel(byte[] payload, string? advertisedName)
    {
        if (payload.Length >= 4)
        {
            ushort idBe = (ushort)((payload[2] << 8) | payload[3]);
            ushort idLe = (ushort)((payload[3] << 8) | payload[2]);

            var detected = MatchProductId(idBe);
            if (detected == AirPodsModelType.Unknown)
            {
                detected = MatchProductId(idLe);
            }

            if (detected != AirPodsModelType.Unknown)
            {
                return detected;
            }
        }

        // Secondary fallback heuristic: Inspect Bluetooth advertised local name
        return InferModelFromName(advertisedName);
    }

    private static AirPodsModelType MatchProductId(ushort id) => id switch
    {
        0x0220 or 0x2002 => AirPodsModelType.AirPodsGen1,
        0x0F20 or 0x200F => AirPodsModelType.AirPodsGen2,
        0x1320 or 0x2013 => AirPodsModelType.AirPodsGen3,
        0x1720 or 0x2017 => AirPodsModelType.AirPodsGen4,
        0x1B20 or 0x201B or 0x1C20 or 0x201C => AirPodsModelType.AirPodsGen4Anc,
        0x0E20 or 0x200E => AirPodsModelType.AirPodsProGen1,
        0x1420 or 0x2014 => AirPodsModelType.AirPodsProGen2Lightning,
        0x2420 or 0x2024 => AirPodsModelType.AirPodsProGen2UsbC,
        0x0A20 or 0x200A => AirPodsModelType.AirPodsMaxLightning,
        0x2720 or 0x2027 or 0x2820 or 0x2028 => AirPodsModelType.AirPodsMaxUsbC,
        _ => AirPodsModelType.Unknown
    };

    private static AirPodsModelType InferModelFromName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return AirPodsModelType.Unknown;

        var lower = name.ToLowerInvariant();

        if (lower.Contains("pro 2") || lower.Contains("pro (2nd") || lower.Contains("pro 2nd"))
        {
            return lower.Contains("usb") || lower.Contains("type-c")
                ? AirPodsModelType.AirPodsProGen2UsbC
                : AirPodsModelType.AirPodsProGen2Lightning;
        }

        if (lower.Contains("pro"))
        {
            return AirPodsModelType.AirPodsProGen1;
        }

        if (lower.Contains("max"))
        {
            return lower.Contains("usb") || lower.Contains("type-c")
                ? AirPodsModelType.AirPodsMaxUsbC
                : AirPodsModelType.AirPodsMaxLightning;
        }

        if (lower.Contains("airpods 4") || lower.Contains("airpods (4th"))
        {
            return lower.Contains("anc") || lower.Contains("noise")
                ? AirPodsModelType.AirPodsGen4Anc
                : AirPodsModelType.AirPodsGen4;
        }

        if (lower.Contains("airpods 3") || lower.Contains("airpods (3rd"))
        {
            return AirPodsModelType.AirPodsGen3;
        }

        if (lower.Contains("airpods 2") || lower.Contains("airpods (2nd"))
        {
            return AirPodsModelType.AirPodsGen2;
        }

        if (lower.Contains("airpods"))
        {
            return AirPodsModelType.AirPodsGen2;
        }

        return AirPodsModelType.Unknown;
    }
}

public sealed record AirPodsTelemetry(
    AirPodsModelType Model,
    BatteryInfo Battery,
    InEarStatus InEar);
