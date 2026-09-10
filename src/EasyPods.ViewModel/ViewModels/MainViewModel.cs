using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyPods.Model.Bluetooth;
using EasyPods.Model.Common;
using EasyPods.Model.Entities;
using EasyPods.ViewModel.Common;

namespace EasyPods.ViewModel.ViewModels;

public sealed partial class MainViewModel : BaseViewModel
{
    private readonly IBluetoothService _bluetoothService;

    [ObservableProperty]
    private bool _isBluetoothRadioOn = true;

    [ObservableProperty]
    private AirPodsStatusViewModel? _selectedAirPods;

    [ObservableProperty]
    private string _statusMessage = "Ready. Searching for AirPods...";

    public ObservableCollection<AirPodsStatusViewModel> AirPodsList { get; } = new();

    public MainViewModel(IBluetoothService bluetoothService)
    {
        _bluetoothService = bluetoothService ?? throw new ArgumentNullException(nameof(bluetoothService));
        _bluetoothService.AirPodsDiscoveredOrUpdated += OnAirPodsDiscoveredOrUpdated;
        _bluetoothService.BluetoothRadioStateChanged += OnBluetoothRadioStateChanged;
    }

    [RelayCommand]
    public async Task StartMonitoringAsync()
    {
        IsBusy = true;
        ClearError();
        StatusMessage = "Starting Bluetooth monitor...";
        AppLogger.Info("Starting Bluetooth monitor...", tag: nameof(MainViewModel));

        var result = await _bluetoothService.StartMonitoringAsync();
        result.Match(
            onSuccess: () =>
            {
                StatusMessage = "Monitoring for AirPods nearby...";
                AppLogger.Info("Bluetooth monitoring active.", tag: nameof(MainViewModel));
                return true;
            },
            onFailure: failure =>
            {
                SetError(failure.Message);
                StatusMessage = "Bluetooth monitoring unavailable.";
                AppLogger.Warn($"Failed to start monitoring: {failure.Message}", tag: nameof(MainViewModel));
                return false;
            });

        IsBusy = false;
    }

    [RelayCommand]
    public async Task ConnectDeviceAsync(AirPodsStatusViewModel? deviceVm)
    {
        if (deviceVm is null) return;

        IsBusy = true;
        ClearError();
        StatusMessage = $"Connecting to {deviceVm.Name}...";
        AppLogger.Info($"Connecting to {deviceVm.Name} (0x{deviceVm.BluetoothAddress:X})...", tag: nameof(MainViewModel));

        var result = await _bluetoothService.ConnectAudioAsync(deviceVm.BluetoothAddress);
        result.Match(
            onSuccess: () =>
            {
                StatusMessage = $"Connected to {deviceVm.Name}.";
                AppLogger.Info($"Successfully connected audio for {deviceVm.Name}.", tag: nameof(MainViewModel));
                return true;
            },
            onFailure: failure =>
            {
                SetError(failure.Message);
                StatusMessage = "Connection attempt failed.";
                AppLogger.Error($"Failed to connect audio for {deviceVm.Name}: {failure.Message}", failure.Exception, tag: nameof(MainViewModel));
                return false;
            });

        IsBusy = false;
    }

    private void OnAirPodsDiscoveredOrUpdated(object? sender, AirPodsDevice device)
    {
        var existing = AirPodsList.FirstOrDefault(vm => vm.BluetoothAddress == device.BluetoothAddress);
        if (existing is null)
        {
            existing = new AirPodsStatusViewModel();
            existing.UpdateFromDevice(device);
            AirPodsList.Add(existing);
        }
        else
        {
            existing.UpdateFromDevice(device);
        }

        SelectedAirPods ??= existing;
    }

    private void OnBluetoothRadioStateChanged(object? sender, bool isEnabled)
    {
        IsBluetoothRadioOn = isEnabled;
        if (!isEnabled)
        {
            SetError("Bluetooth adapter is turned off in Windows.");
        }
        else
        {
            ClearError();
        }
    }
}
