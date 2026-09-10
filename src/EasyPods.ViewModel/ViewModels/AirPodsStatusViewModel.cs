using CommunityToolkit.Mvvm.ComponentModel;
using EasyPods.Model.Entities;
using EasyPods.ViewModel.Common;

namespace EasyPods.ViewModel.ViewModels;

public sealed partial class AirPodsStatusViewModel : BaseViewModel
{
    [ObservableProperty]
    private ulong _bluetoothAddress;

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

    public void UpdateFromDevice(AirPodsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        BluetoothAddress = device.BluetoothAddress;
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
