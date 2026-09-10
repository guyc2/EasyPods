using CommunityToolkit.Mvvm.ComponentModel;
using EasyPods.Model.Entities;
using EasyPods.ViewModel.Common;

namespace EasyPods.ViewModel.ViewModels;

public sealed partial class AirPodsStatusViewModel : BaseViewModel
{
    [ObservableProperty]
    private ulong _bluetoothAddress;

    [ObservableProperty]
    private string _formattedAddress = string.Empty;

    [ObservableProperty]
    private string _name = "AirPods";

    [ObservableProperty]
    private string _modelDisplay = "Apple AirPods";

    [ObservableProperty]
    private ConnectionState _connectionState = ConnectionState.Disconnected;

    [ObservableProperty]
    private int? _leftPodBattery;

    [ObservableProperty]
    private bool _leftPodCharging;

    [ObservableProperty]
    private int? _rightPodBattery;

    [ObservableProperty]
    private bool _rightPodCharging;

    [ObservableProperty]
    private int? _caseBattery;

    [ObservableProperty]
    private bool _caseCharging;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private short _rssi;

    [ObservableProperty]
    private string _signalStrength = "Good";

    [ObservableProperty]
    private string _lastSeenText = "Just now";

    public void UpdateFromDevice(AirPodsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        BluetoothAddress = device.BluetoothAddress;
        FormattedAddress = device.FormattedMacAddress;
        Name = string.IsNullOrWhiteSpace(device.Name) ? "AirPods" : device.Name;
        ModelDisplay = FormatModelName(device.Model);
        ConnectionState = device.State;
        IsConnected = device.State == ConnectionState.Connected;

        LeftPodBattery = device.Battery.LeftPodLevel;
        LeftPodCharging = device.Battery.LeftCharging;

        RightPodBattery = device.Battery.RightPodLevel;
        RightPodCharging = device.Battery.RightCharging;

        CaseBattery = device.Battery.CaseLevel;
        CaseCharging = device.Battery.CaseCharging;

        Rssi = device.Rssi;
        SignalStrength = EvaluateSignalStrength(device.Rssi);
        LastSeenText = FormatLastSeen(device.LastSeenUtc);
    }

    private static string EvaluateSignalStrength(short rssi) => rssi switch
    {
        >= -60 and <= 0 => "Excellent (Nearby)",
        >= -75 and < -60 => "Good",
        >= -88 and < -75 => "Fair",
        _ when rssi != 0 => "Weak",
        _ => "Nearby"
    };

    private static string FormatLastSeen(DateTimeOffset lastSeen)
    {
        var elapsed = DateTimeOffset.UtcNow - lastSeen;
        if (elapsed.TotalSeconds < 10) return "Just now";
        if (elapsed.TotalSeconds < 60) return $"{Math.Max(1, (int)elapsed.TotalSeconds)}s ago";
        if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
        return lastSeen.ToLocalTime().ToString("t");
    }

    private static string FormatModelName(AirPodsModelType model) => model switch
    {
        AirPodsModelType.AirPodsGen1 => "AirPods (1st Gen)",
        AirPodsModelType.AirPodsGen2 => "AirPods (2nd Gen)",
        AirPodsModelType.AirPodsGen3 => "AirPods (3rd Gen)",
        AirPodsModelType.AirPodsGen4 => "AirPods 4",
        AirPodsModelType.AirPodsProGen1 => "AirPods Pro (1st Gen)",
        AirPodsModelType.AirPodsProGen2 => "AirPods Pro (2nd Gen)",
        AirPodsModelType.AirPodsMax => "AirPods Max",
        _ => "Apple AirPods"
    };
}
