---
name: logging
description: Guidelines and utility interface for structured logging and telemetry across EasyPods using NLog.
---

# Logging Guidelines (`logging`)

This skill defines the structured logging and telemetry standards for **EasyPods**, powered by **NLog**.

---

## 1. Architecture & Outputs

EasyPods utilizes **NLog** with non-blocking asynchronous targets:
1. **Visual Studio Debugger Output**: Real-time log streaming in IDE Output.
2. **Rolling File Target**:
   - Location: `%LOCALAPPDATA%\EasyPods\Logs\easypods-${shortdate}.log`
   - Archive: `%LOCALAPPDATA%\EasyPods\Logs\archives\` (Daily rolling, up to 7 days preserved).
   - Threading: Asynchronous target wrapper (`AsyncTargetWrapper`) ensuring Bluetooth packet handling and UI threads are never blocked.
3. **Application Crash Locations**:
   - `AppLogger.Fatal` is hooked into `DispatcherUnhandledException`, `AppDomain.CurrentDomain.UnhandledException`, and `TaskScheduler.UnobservedTaskException` in `App.xaml.cs`.

---

## 2. API & Usage

All layers log via the static `AppLogger` utility in `EasyPods.Model.Common`:

```csharp
using EasyPods.Model.Common;

// 1. Debug: fine-grained BLE packets, state transitions, cache lookups
AppLogger.Debug("Decoded beacon byte payload: 0x4C...", tag: nameof(AirPodsBeaconParser));

// 2. Info: key lifecycle milestones
AppLogger.Info("Connected to AirPods Pro.", tag: nameof(MainViewModel));

// 3. Warn / Warning: non-fatal issues, retries, low battery
AppLogger.Warn("Bluetooth adapter disabled.", tag: nameof(WindowsBluetoothService));

// 4. Error: failures with optional exception
AppLogger.Error("Audio routing failed.", ex, tag: nameof(WindowsBluetoothService));

// 5. Fatal: unhandled crashes and critical failures
AppLogger.Fatal("Critical application crash.", ex, tag: "CrashReporter");
```

---

## 3. Rules

- **Zero Swallowed Exceptions**: Any caught exception that causes an operation to fail must be logged via `AppLogger.Error` (or `AppLogger.Fatal`) and returned as a typed `Failure`.
- **Tag Conventions**: Use `nameof(CurrentClass)` for the `tag` parameter to make log filtering seamless.
- **Data Privacy**: Never log personal identifiable information or raw user audio data.
