using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyPods.Model.Bluetooth;
using EasyPods.Model.Common;
using EasyPods.Model.Entities;
using EasyPods.ViewModel.Common;

namespace EasyPods.ViewModel.ViewModels;

public sealed partial class MainViewModel : BaseViewModel, IDisposable, IAsyncDisposable
{
    private readonly IBluetoothService _bluetoothService;
    private readonly SynchronizationContext? _syncContext;
    private bool _isDisposed;

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
        _syncContext = SynchronizationContext.Current;

        _bluetoothService.AirPodsDiscoveredOrUpdated += OnAirPodsDiscoveredOrUpdated;
        _bluetoothService.BluetoothRadioStateChanged += OnBluetoothRadioStateChanged;
        IsBluetoothRadioOn = _bluetoothService.IsRadioEnabled;
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
    public async Task StopMonitoringAsync()
    {
        IsBusy = true;
        StatusMessage = "Stopping Bluetooth monitor...";
        AppLogger.Info("Stopping Bluetooth monitor...", tag: nameof(MainViewModel));

        var result = await _bluetoothService.StopMonitoringAsync();
        result.Match(
            onSuccess: () =>
            {
                StatusMessage = "Monitoring paused.";
                return true;
            },
            onFailure: failure =>
            {
                SetError(failure.Message);
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
        void UpdateAction()
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

        if (_syncContext is not null && SynchronizationContext.Current != _syncContext)
        {
            _syncContext.Post(_ => UpdateAction(), null);
        }
        else
        {
            UpdateAction();
        }
    }

    private void OnBluetoothRadioStateChanged(object? sender, bool isEnabled)
    {
        void StateAction()
        {
            IsBluetoothRadioOn = isEnabled;
            if (!isEnabled)
            {
                SetError("Bluetooth adapter is turned off in Windows.");
                StatusMessage = "Bluetooth is disabled.";
            }
            else
            {
                ClearError();
                StatusMessage = "Bluetooth enabled. Ready.";
            }
        }

        if (_syncContext is not null && SynchronizationContext.Current != _syncContext)
        {
            _syncContext.Post(_ => StateAction(), null);
        }
        else
        {
            StateAction();
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _bluetoothService.AirPodsDiscoveredOrUpdated -= OnAirPodsDiscoveredOrUpdated;
        _bluetoothService.BluetoothRadioStateChanged -= OnBluetoothRadioStateChanged;
        _bluetoothService.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _bluetoothService.AirPodsDiscoveredOrUpdated -= OnAirPodsDiscoveredOrUpdated;
        _bluetoothService.BluetoothRadioStateChanged -= OnBluetoothRadioStateChanged;
        await _bluetoothService.DisposeAsync();
    }
}
