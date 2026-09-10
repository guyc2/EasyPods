using Windows.Devices.Enumeration;
using Windows.Media.Devices;
using EasyPods.Model.Common;

namespace EasyPods.Model.Hardware;

/// <summary>
/// Monitors and manages Windows multimedia audio playback endpoints.
/// </summary>
public sealed class WindowsAudioEndpointService : IAudioEndpointService
{
    private readonly object _lock = new();
    private bool _isDisposed;

    public event EventHandler<string>? DefaultPlaybackDeviceChanged;

    public string? CurrentDefaultEndpointId { get; private set; }
    public string? PreviousDefaultEndpointId { get; private set; }

    public WindowsAudioEndpointService()
    {
        try
        {
            CurrentDefaultEndpointId = MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default);
            MediaDevice.DefaultAudioRenderDeviceChanged += OnDefaultRenderDeviceChanged;
        }
        catch (Exception ex)
        {
            AppLogger.Warn($"Failed to initialize MediaDevice default render query: {ex.Message}", nameof(WindowsAudioEndpointService));
        }
    }

    private void OnDefaultRenderDeviceChanged(object? sender, DefaultAudioRenderDeviceChangedEventArgs args)
    {
        if (args.Role == AudioDeviceRole.Default)
        {
            lock (_lock)
            {
                if (CurrentDefaultEndpointId != args.Id)
                {
                    PreviousDefaultEndpointId = CurrentDefaultEndpointId;
                    CurrentDefaultEndpointId = args.Id;
                }
            }

            AppLogger.Info($"Default Windows audio render device changed to: {args.Id}", nameof(WindowsAudioEndpointService));
            DefaultPlaybackDeviceChanged?.Invoke(this, args.Id);
        }
    }

    public async Task<IReadOnlyList<AudioEndpointInfo>> GetAvailableEndpointsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var list = new List<AudioEndpointInfo>();
        try
        {
            var selector = MediaDevice.GetAudioRenderSelector();
            var devices = await DeviceInformation.FindAllAsync(selector).AsTask(cancellationToken);
            var defaultId = MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default);

            foreach (var dev in devices)
            {
                list.Add(new AudioEndpointInfo(
                    Id: dev.Id,
                    Name: dev.Name,
                    IsDefault: dev.Id == defaultId));
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error("Error enumerating audio render endpoints.", ex, nameof(WindowsAudioEndpointService));
        }

        return list;
    }

    public async Task<Result> SetDefaultPlaybackDeviceAsync(string deviceNameOrSubstring, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceNameOrSubstring);

        try
        {
            var endpoints = await GetAvailableEndpointsAsync(cancellationToken);
            var target = endpoints.FirstOrDefault(e => e.Name.Contains(deviceNameOrSubstring, StringComparison.OrdinalIgnoreCase));

            if (target is null)
            {
                AppLogger.Warn($"No audio endpoint found matching '{deviceNameOrSubstring}'.", nameof(WindowsAudioEndpointService));
                return Result.Fail(new DeviceNotFoundFailure($"No audio playback endpoint found matching '{deviceNameOrSubstring}'."));
            }

            lock (_lock)
            {
                if (CurrentDefaultEndpointId != target.Id)
                {
                    PreviousDefaultEndpointId = CurrentDefaultEndpointId;
                    CurrentDefaultEndpointId = target.Id;
                }
            }

            AppLogger.Info($"Target audio endpoint identified: {target.Name} ({target.Id})", nameof(WindowsAudioEndpointService));
            return Result.Success();
        }
        catch (Exception ex)
        {
            AppLogger.Error("Exception while setting default audio endpoint.", ex, nameof(WindowsAudioEndpointService));
            return Result.Fail(new ConnectionFailedFailure("Failed to route audio to endpoint.", ex));
        }
    }

    public Task<Result> RestorePreviousDefaultDeviceAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            if (string.IsNullOrEmpty(PreviousDefaultEndpointId))
            {
                return Task.FromResult(Result.Success());
            }

            AppLogger.Info($"Restoring previous default audio endpoint: {PreviousDefaultEndpointId}", nameof(WindowsAudioEndpointService));
            CurrentDefaultEndpointId = PreviousDefaultEndpointId;
            PreviousDefaultEndpointId = null;
        }

        return Task.FromResult(Result.Success());
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        try
        {
            MediaDevice.DefaultAudioRenderDeviceChanged -= OnDefaultRenderDeviceChanged;
        }
        catch (Exception ex)
        {
            AppLogger.Warn($"Error unsubscribing MediaDevice listener: {ex.Message}", nameof(WindowsAudioEndpointService));
        }
    }
}
