namespace EasyPods.Model.Entities;

/// <summary>
/// Identifies the specific model generation and hardware variant of Apple AirPods.
/// </summary>
public enum AirPodsModelType
{
    Unknown = 0,
    AirPodsGen1 = 1,
    AirPodsGen2 = 2,
    AirPodsGen3 = 3,
    AirPodsGen4 = 4,
    AirPodsGen4Anc = 5,
    AirPodsProGen1 = 6,
    AirPodsProGen2Lightning = 7,
    AirPodsProGen2UsbC = 8,
    AirPodsMaxLightning = 9,
    AirPodsMaxUsbC = 10
}
