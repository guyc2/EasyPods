---
name: easypods-guidelines
description: Core architecture conventions, Bluetooth WinRT APIs, AirPods beacon protocol, MVVM state management, and WPF guidelines for EasyPods.
---

# EasyPods Guidelines (`easypods-guidelines`)

This skill provides guidelines and patterns for engineering features in the **EasyPods** Windows desktop application.

---

## 1. Project Separation & Responsibilities

- **`EasyPods.View`**:
  - Pure WPF Presentation layer (.NET 8.0-windows).
  - XAML Windows, UserControls, System Tray integration, Fluent design themes.
  - DataContext bindings to ViewModels. Zero business logic in code-behind (`.xaml.cs`).
- **`EasyPods.ViewModel`**:
  - UI State machines, Commands, and orchestration.
  - Built with `CommunityToolkit.Mvvm` (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`).
  - Decoupled from WPF visual types to enable 100% automated testing.
- **`EasyPods.Model`**:
  - Domain models (`AirPodsDevice`, `BatteryInfo`, `AirPodsModelType`, `ConnectionStatus`).
  - WinRT Bluetooth BLE watcher (`Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementWatcher`).
  - Apple BLE Manufacturer Data (Company ID `0x004C`) beacon parser:
    - Decodes Left pod battery %, Right pod battery %, Case battery %, Charging flags, and In-Ear detection.
  - Audio device endpoint integration (`CoreAudio` / `NAudio` or `Windows.Media.Devices`).
- **`EasyPods.Test`**:
  - Comprehensive unit tests covering beacon parsing, result patterns, and ViewModel states.

---

## 2. AirPods Beacon Specification Summary

Apple AirPods broadcast unencrypted Bluetooth Low Energy advertisement beacons containing manufacturer data:
- **Company ID**: `0x004C` (Apple Inc.)
- **Beacon Type**: `0x07` (Proximity / AirPods status)
- **Length**: Typically 27 bytes
- **Telemetry Encoded**:
  - Model ID (AirPods 1/2/3/4, AirPods Pro 1/2, AirPods Max)
  - Left Pod Battery level (0-10, where 10 = 100%, 15 = disconnected/absent) & Charging flag
  - Right Pod Battery level (0-10) & Charging flag
  - Case Battery level (0-10) & Charging flag
  - In-ear status (Left in-ear, Right in-ear)

---

## 3. Error Handling & Result Pattern

- Every operation that interacts with hardware or external state must return `Result<T>` or `Result`.
- Never throw exceptions across layer boundaries for expected failure scenarios.
- Catch specific hardware exceptions (e.g. `COMException`, `UnauthorizedAccessException`) at the Bluetooth boundary and map them into domain `Failure` types.
