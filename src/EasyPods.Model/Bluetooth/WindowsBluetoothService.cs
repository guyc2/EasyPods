using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Radios;
using Windows.Storage.Streams;
using EasyPods.Model.Common;
using EasyPods.Model.Entities;
using EasyPods.Model.Protocols.AirPods;

namespace EasyPods.Model.Bluetooth;

/// <summary>
/// Native Windows Bluetooth implementation using WinRT Bluetooth LE APIs.
/// High performance, thread-safe, and non-blocking.
/// </summary>
public sealed class WindowsBluetoothService : IBluetoothService
{
    private readonly List<AirPodsDevice> _discoveredDevices = new();
    private readonly object _lock = new();

    private BluetoothLEAdvertisementWatcher? _watcher;
    private Radio? _bluetoothRadio;
    private bool _isMonitoring;
    private bool _isDisposed;

    public event EventHandler<AirPodsDevice>? AirPodsDiscoveredOrUpdated;
    public event EventHandler<bool>? BluetoothRadioStateChanged;

    public bool IsRadioEnabled { get; private set; } = true;

    public IReadOnlyList<AirPodsDevice> DiscoveredAirPods
    {
        get
        {
            lock (_lock)
            {
                return _discoveredDevices.ToList();
            }
        }
    }

    public WindowsBluetoothService()
    {
        InitializeWatcher();
        _ = InitializeRadioAsync();
    }

    private void InitializeWatcher()
    {
        try
        {
            _watcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };

            // Filter specifically for Apple Inc. manufacturer data (Company ID 0x004C)
            var manufacturerFilter = new BluetoothLEManufacturerData
            {
                CompanyId = AirPodsBeaconParser.AppleCompanyId
            };
            _watcher.AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerFilter);

            _watcher.Received += OnWatcherAdvertisementReceived;
            _watcher.Stopped += OnWatcherStopped;

            AppLogger.Debug("BluetoothLEAdvertisementWatcher initialized with Apple Company ID filter (0x004C).", nameof(WindowsBluetoothService));
        }
        catch (Exception ex)
        {
            AppLogger.Error("Failed to initialize BluetoothLEAdvertisementWatcher.", ex, nameof(WindowsBluetoothService));
        }
    }

    private async Task InitializeRadioAsync()
    {
        try
        {
            var radios = await Radio.GetRadiosAsync();
            var btRadio = radios.FirstOrDefault(r => r.Kind == RadioKind.Bluetooth);
            if (btRadio is not null)
            {
                _bluetoothRadio = btRadio;
                IsRadioEnabled = btRadio.State == RadioState.On;
                btRadio.StateChanged += OnRadioStateChanged;

                AppLogger.Info($"Bluetooth radio detected: {btRadio.Name}, State: {btRadio.State}", nameof(WindowsBluetoothService));
                BluetoothRadioStateChanged?.Invoke(this, IsRadioEnabled);
            }
            else
            {
                AppLogger.Warn("No Bluetooth radio detected on this system.", nameof(WindowsBluetoothService));
            }
        }
        catch (Exception ex)
        {
            AppLogger.Warn($"Unable to query Windows Bluetooth radio state: {ex.Message}", nameof(WindowsBluetoothService));
        }
    }

    private void OnRadioStateChanged(Radio sender, object args)
    {
        var isEnabled = sender.State == RadioState.On;
        IsRadioEnabled = isEnabled;
        AppLogger.Info($"Bluetooth radio state changed to: {sender.State} (Enabled: {isEnabled})", nameof(WindowsBluetoothService));
        BluetoothRadioStateChanged?.Invoke(this, isEnabled);
    }

    public Task<Result> StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_watcher is null)
        {
            AppLogger.Error("Cannot start monitoring: BluetoothLEAdvertisementWatcher is null.", tag: nameof(WindowsBluetoothService));
            return Task.FromResult(Result.Fail(new BluetoothUnavailableFailure("Bluetooth LE Watcher could not be initialized on this device.")));
        }

        try
        {
            if (!_isMonitoring)
            {
                _watcher.Start();
                _isMonitoring = true;
                AppLogger.Info("BluetoothLEAdvertisementWatcher started successfully.", nameof(WindowsBluetoothService));
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            AppLogger.Error("Exception when starting BluetoothLEAdvertisementWatcher.", ex, nameof(WindowsBluetoothService));
            return Task.FromResult(Result.Fail(new BluetoothUnavailableFailure("Failed to start Bluetooth monitoring.", ex)));
        }
    }

    public Task<Result> StopMonitoringAsync()
    {
        ThrowIfDisposed();

        try
        {
            if (_isMonitoring && _watcher is not null)
            {
                _watcher.Stop();
                _isMonitoring = false;
                AppLogger.Info("BluetoothLEAdvertisementWatcher stopped.", nameof(WindowsBluetoothService));
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            AppLogger.Error("Exception when stopping BluetoothLEAdvertisementWatcher.", ex, nameof(WindowsBluetoothService));
            return Task.FromResult(Result.Fail(new ConnectionFailedFailure("Failed to stop Bluetooth watcher.", ex)));
        }
    }

    public Task<Result> ConnectAudioAsync(ulong bluetoothAddress, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        AppLogger.Info($"ConnectAudio requested for address: 0x{bluetoothAddress:X12}", nameof(WindowsBluetoothService));
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DisconnectAudioAsync(ulong bluetoothAddress, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        AppLogger.Info($"DisconnectAudio requested for address: 0x{bluetoothAddress:X12}", nameof(WindowsBluetoothService));
        return Task.FromResult(Result.Success());
    }

    private void OnWatcherAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
    {
        try
        {
            var manufacturerSections = args.Advertisement.ManufacturerData;
            foreach (var section in manufacturerSections)
            {
                if (section.CompanyId != AirPodsBeaconParser.AppleCompanyId) continue;

                var dataReader = DataReader.FromBuffer(section.Data);
                var payload = new byte[section.Data.Length];
                dataReader.ReadBytes(payload);

                ProcessAdvertisement(args.BluetoothAddress, args.Advertisement.LocalName, payload, args.RawSignalStrengthInDBm);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error("Exception in OnWatcherAdvertisementReceived callback.", ex, nameof(WindowsBluetoothService));
        }
    }

    private void OnWatcherStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
    {
        _isMonitoring = false;
        AppLogger.Warn($"BluetoothLEAdvertisementWatcher stopped unexpectedly. Error: {args.Error}", nameof(WindowsBluetoothService));
    }

    public void ProcessAdvertisement(ulong address, string? name, byte[] payload, short rssi = 0)
    {
        var parseResult = AirPodsBeaconParser.Parse(payload);
        if (parseResult.IsFailure) return;

        var telemetry = parseResult.Value;
        var device = new AirPodsDevice(
            BluetoothAddress: address,
            Name: string.IsNullOrWhiteSpace(name) ? "AirPods" : name,
            Model: telemetry.Model,
            State: ConnectionState.Disconnected,
            Battery: telemetry.Battery,
            InEar: InEarStatus.Unknown,
            LastSeenUtc: DateTimeOffset.UtcNow,
            Rssi: rssi);

        lock (_lock)
        {
            var idx = _discoveredDevices.FindIndex(d => d.BluetoothAddress == address);
            if (idx >= 0)
            {
                _discoveredDevices[idx] = device;
            }
            else
            {
                _discoveredDevices.Add(device);
            }
        }

        AppLogger.Debug($"AirPods telemetry updated: 0x{address:X12} ({device.Model}) RSSI: {rssi} dBm", nameof(WindowsBluetoothService));
        AirPodsDiscoveredOrUpdated?.Invoke(this, device);
    }

    public void SetRadioState(bool isEnabled)
    {
        IsRadioEnabled = isEnabled;
        BluetoothRadioStateChanged?.Invoke(this, isEnabled);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_watcher is not null)
        {
            _watcher.Received -= OnWatcherAdvertisementReceived;
            _watcher.Stopped -= OnWatcherStopped;
            try
            {
                _watcher.Stop();
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"Error stopping watcher during dispose: {ex.Message}", nameof(WindowsBluetoothService));
            }
            _watcher = null;
        }

        if (_bluetoothRadio is not null)
        {
            _bluetoothRadio.StateChanged -= OnRadioStateChanged;
            _bluetoothRadio = null;
        }

        AppLogger.Info("WindowsBluetoothService disposed cleanly.", nameof(WindowsBluetoothService));
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
