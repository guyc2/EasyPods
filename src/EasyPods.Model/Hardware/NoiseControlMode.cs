using EasyPods.Model.Entities;

namespace EasyPods.Model.Hardware;

/// <summary>
/// Hardware listening modes for supported Apple AirPods models (AirPods Pro, Max, and 4 ANC).
/// </summary>
public enum NoiseControlMode
{
    Off = 0,
    ActiveNoiseCancellation = 1,
    Transparency = 2,
    Adaptive = 3
}

public static class NoiseControlExtensions
{
    /// <summary>
    /// Checks whether the hardware model physically supports active noise control modes.
    /// </summary>
    public static bool SupportsNoiseControl(this AirPodsModelType model) => model switch
    {
        AirPodsModelType.AirPodsGen4Anc => true,
        AirPodsModelType.AirPodsProGen1 => true,
        AirPodsModelType.AirPodsProGen2Lightning => true,
        AirPodsModelType.AirPodsProGen2UsbC => true,
        AirPodsModelType.AirPodsMaxLightning => true,
        AirPodsModelType.AirPodsMaxUsbC => true,
        _ => false
    };

    /// <summary>
    /// Checks whether the model supports Adaptive Audio (AirPods Pro 2 and AirPods 4 ANC).
    /// </summary>
    public static bool SupportsAdaptiveAudio(this AirPodsModelType model) => model switch
    {
        AirPodsModelType.AirPodsGen4Anc => true,
        AirPodsModelType.AirPodsProGen2Lightning => true,
        AirPodsModelType.AirPodsProGen2UsbC => true,
        _ => false
    };

    public static string ToFriendlyName(this NoiseControlMode mode) => mode switch
    {
        NoiseControlMode.Off => "Off",
        NoiseControlMode.ActiveNoiseCancellation => "Noise Cancellation",
        NoiseControlMode.Transparency => "Transparency",
        NoiseControlMode.Adaptive => "Adaptive Audio",
        _ => "Unknown"
    };
}
