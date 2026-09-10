using System.Runtime.InteropServices;
using EasyPods.Model.Common;
using EasyPods.Model.Entities;

namespace EasyPods.Model.Hardware;

/// <summary>
/// Monitors real-time optical/capacitive in-ear placement telemetry and controls
/// Windows media playback (Auto-Pause when removed, Auto-Resume when inserted).
/// </summary>
public sealed class WindowsInEarAutomationService : IInEarAutomationService
{
    private const byte VkMediaPlayPause = 0xB3;
    private const uint KeyEventFKeyUp = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);

    private readonly object _lock = new();
    private readonly Action? _mediaDispatcherOverride;
    private readonly TimeSpan _debounceInterval;

    private bool? _lastAnyPodInEar;
    private DateTimeOffset _lastTransitionTime = DateTimeOffset.MinValue;
    private bool _isDisposed;

    public event EventHandler<bool>? PlaybackStateAutoToggled;

    public bool IsAutoPauseEnabled { get; set; } = true;
    public bool HasAutoPaused { get; private set; }

    public WindowsInEarAutomationService(Action? mediaDispatcherOverride = null, TimeSpan? debounceInterval = null)
    {
        _mediaDispatcherOverride = mediaDispatcherOverride;
        _debounceInterval = debounceInterval ?? TimeSpan.FromMilliseconds(400);
    }

    public void ProcessDeviceTelemetry(AirPodsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (!IsAutoPauseEnabled) return;

        // Ensure sensor data exists
        if (!device.InEar.HasAnySensorData) return;

        bool leftIn = device.InEar.LeftInEar ?? false;
        bool rightIn = device.InEar.RightInEar ?? false;
        bool anyPodInEar = leftIn || rightIn;

        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;

            if (_lastAnyPodInEar.HasValue)
            {
                // Both taken out of ear (or single pod in use removed)
                if (_lastAnyPodInEar.Value && !anyPodInEar)
                {
                    if (now - _lastTransitionTime >= _debounceInterval)
                    {
                        _lastTransitionTime = now;
                        _lastAnyPodInEar = false;

                        if (!HasAutoPaused)
                        {
                            AppLogger.Info($"Ear removal detected on 0x{device.BluetoothAddress:X12}. Triggering Windows Media Auto-Pause.", nameof(WindowsInEarAutomationService));
                            _ = DispatchPlayPauseInternalAsync();
                            HasAutoPaused = true;
                            PlaybackStateAutoToggled?.Invoke(this, false);
                        }
                    }
                }
                // Placed back into ear
                else if (!_lastAnyPodInEar.Value && anyPodInEar)
                {
                    if (now - _lastTransitionTime >= _debounceInterval)
                    {
                        _lastTransitionTime = now;
                        _lastAnyPodInEar = true;

                        if (HasAutoPaused)
                        {
                            AppLogger.Info($"Ear insertion detected on 0x{device.BluetoothAddress:X12}. Triggering Windows Media Auto-Resume.", nameof(WindowsInEarAutomationService));
                            _ = DispatchPlayPauseInternalAsync();
                            HasAutoPaused = false;
                            PlaybackStateAutoToggled?.Invoke(this, true);
                        }
                    }
                }
            }
            else
            {
                _lastAnyPodInEar = anyPodInEar;
                _lastTransitionTime = now;
            }
        }
    }

    public Task<Result> SendPlayPauseMediaKeyAsync()
    {
        return DispatchPlayPauseInternalAsync();
    }

    private Task<Result> DispatchPlayPauseInternalAsync()
    {
        try
        {
            if (_mediaDispatcherOverride is not null)
            {
                _mediaDispatcherOverride.Invoke();
            }
            else
            {
                // Simulate key press and release of VK_MEDIA_PLAY_PAUSE
                keybd_event(VkMediaPlayPause, 0, 0, 0);
                keybd_event(VkMediaPlayPause, 0, KeyEventFKeyUp, 0);
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            AppLogger.Error("Failed to dispatch VK_MEDIA_PLAY_PAUSE key event.", ex, nameof(WindowsInEarAutomationService));
            return Task.FromResult(Result.Fail(new ConnectionFailedFailure("Unable to dispatch media key event.", ex)));
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
    }
}
