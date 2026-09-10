using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyPods.Model.Bluetooth;
using EasyPods.Model.Common;
using EasyPods.Model.Entities;
using EasyPods.Model.Hardware;
using EasyPods.ViewModel.Common;

namespace EasyPods.ViewModel.ViewModels;

public sealed partial class MainViewModel : BaseViewModel, IDisposable, IAsyncDisposable
{
    private readonly IBluetoothService _bluetoothService;
    private readonly IInEarAutomationService _inEarAutomation;
    private readonly IAutoConnectService _autoConnectService;
    private readonly INoiseControlService _noiseControlService;
    private readonly IAudioEndpointService _audioEndpointService;
    private readonly SynchronizationContext? _syncContext;
    private bool _isDisposed;

    [ObservableProperty]
    private bool _isBluetoothRadioOn = true;

    [ObservableProperty]
    private AirPodsStatusViewModel? _selectedAirPods;

    [ObservableProperty]
    private string _statusMessage = "Ready. Searching for AirPods...";

    [ObservableProperty]
    private bool _isAutoPauseEnabled = true;

    [ObservableProperty]
    private bool _isAutoConnectEnabled = true;

    [ObservableProperty]
    private string _hardwareStatus = "Auto-Pause & Auto-Connect Active";

    public ObservableCollection<AirPodsStatusViewModel> AirPodsList { get; } = new();

    public MainViewModel(
        IBluetoothService bluetoothService,
        IInEarAutomationService? inEarAutomation = null,
        IAutoConnectService? autoConnectService = null,
        INoiseControlService? noiseControlService = null,
        IAudioEndpointService? audioEndpointService = null)
    {
        _bluetoothService = bluetoothService ?? throw new ArgumentNullException(nameof(bluetoothService));
        _inEarAutomation = inEarAutomation ?? new WindowsInEarAutomationService();
        _autoConnectService = autoConnectService ?? new WindowsAutoConnectService(_bluetoothService);
        _noiseControlService = noiseControlService ?? new WindowsNoiseControlService();
        _audioEndpointService = audioEndpointService ?? new WindowsAudioEndpointService();
        _syncContext = SynchronizationContext.Current;

        _bluetoothService.AirPodsDiscoveredOrUpdated += OnAirPodsDiscoveredOrUpdated;
        _bluetoothService.BluetoothRadioStateChanged += OnBluetoothRadioStateChanged;
        _inEarAutomation.PlaybackStateAutoToggled += OnPlaybackStateAutoToggled;
        _autoConnectService.AutoConnectInitiated += OnAutoConnectInitiated;

        IsBluetoothRadioOn = _bluetoothService.IsRadioEnabled;
        _inEarAutomation.IsAutoPauseEnabled = IsAutoPauseEnabled;
        _autoConnectService.IsAutoConnectEnabled = IsAutoConnectEnabled;
    }

    partial void OnIsAutoPauseEnabledChanged(bool value)
    {
        _inEarAutomation.IsAutoPauseEnabled = value;
        AppLogger.Info($"In-Ear Auto-Pause toggled to: {value}", nameof(MainViewModel));
    }

    partial void OnIsAutoConnectEnabledChanged(bool value)
    {
        _autoConnectService.IsAutoConnectEnabled = value;
        AppLogger.Info($"Case Lid Auto-Connect toggled to: {value}", nameof(MainViewModel));
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
                _ = _audioEndpointService.SetDefaultPlaybackDeviceAsync(deviceVm.Name);
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

    [RelayCommand]
    public async Task DisconnectDeviceAsync(AirPodsStatusViewModel? deviceVm)
    {
        if (deviceVm is null) return;

        IsBusy = true;
        StatusMessage = $"Disconnecting {deviceVm.Name}...";
        var result = await _bluetoothService.DisconnectAudioAsync(deviceVm.BluetoothAddress);

        result.Match(
            onSuccess: () =>
            {
                StatusMessage = $"Disconnected {deviceVm.Name}.";
                _ = _audioEndpointService.RestorePreviousDefaultDeviceAsync();
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
    public async Task SetNoiseControlModeAsync(NoiseControlMode mode)
    {
        if (SelectedAirPods is null) return;

        var result = await _noiseControlService.SetModeAsync(
            SelectedAirPods.BluetoothAddress,
            SelectedAirPods.ModelType,
            mode);

        result.Match(
            onSuccess: () =>
            {
                SelectedAirPods.NoiseControlMode = mode;
                SelectedAirPods.NoiseControlDisplay = mode.ToFriendlyName();
                StatusMessage = $"Noise Control: {mode.ToFriendlyName()}";
                return true;
            },
            onFailure: failure =>
            {
                SetError(failure.Message);
                return false;
            });
    }

    private void OnPlaybackStateAutoToggled(object? sender, bool isPlaying)
    {
        HardwareStatus = isPlaying ? "Media Resumed (In-Ear)" : "Media Paused (Pod Removed)";
        AppLogger.Info($"Hardware In-Ear action: {HardwareStatus}", nameof(MainViewModel));
    }

    private void OnAutoConnectInitiated(object? sender, ulong address)
    {
        HardwareStatus = $"Auto-Connecting to 0x{address:X12} (Lid Open)";
        AppLogger.Info(HardwareStatus, nameof(MainViewModel));
    }

    private void OnAirPodsDiscoveredOrUpdated(object? sender, AirPodsDevice device)
    {
        // 1. Process hardware in-ear placement for auto-pause/resume
        _inEarAutomation.ProcessDeviceTelemetry(device);

        // 2. Evaluate case lid open for auto-connect
        _ = _autoConnectService.EvaluateAdvertisementForAutoConnectAsync(device);

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
        _inEarAutomation.PlaybackStateAutoToggled -= OnPlaybackStateAutoToggled;
        _autoConnectService.AutoConnectInitiated -= OnAutoConnectInitiated;

        _bluetoothService.Dispose();
        _inEarAutomation.Dispose();
        _autoConnectService.Dispose();
        _audioEndpointService.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _bluetoothService.AirPodsDiscoveredOrUpdated -= OnAirPodsDiscoveredOrUpdated;
        _bluetoothService.BluetoothRadioStateChanged -= OnBluetoothRadioStateChanged;
        _inEarAutomation.PlaybackStateAutoToggled -= OnPlaybackStateAutoToggled;
        _autoConnectService.AutoConnectInitiated -= OnAutoConnectInitiated;

        await _bluetoothService.DisposeAsync();
        _inEarAutomation.Dispose();
        _autoConnectService.Dispose();
        _audioEndpointService.Dispose();
    }
}
