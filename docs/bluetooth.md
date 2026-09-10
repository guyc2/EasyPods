---
title: Windows Bluetooth Architecture & WinRT Integration
tags:
  - easypods
  - bluetooth
  - winrt
  - ble
---

# Windows Bluetooth Architecture & WinRT Integration

This document outlines how EasyPods interacts with the Windows Bluetooth subsystem via Windows Runtime (WinRT) APIs.

---

## 1. WinRT Bluetooth APIs

EasyPods utilizes native Windows 10/11 WinRT APIs without external COM dependencies:
- **`Windows.Devices.Bluetooth.BluetoothDevice`**: Classic Bluetooth connection, pairing status, audio device profiles.
- **`Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementWatcher`**: Low-latency background BLE advertisement packet receiver.
- **`Windows.Devices.Radios.Radio`**: Monitoring adapter power state (On, Off, Disabled).
- **`Windows.Media.Devices.MediaDevice`**: Monitoring and switching default multimedia audio playback endpoints.

---

## 2. Connection & Telemetry Flow

```mermaid
sequenceDiagram
    participant UI as EasyPods.View / ViewModel
    participant BT as WindowsBluetoothService
    participant WinRT as WinRT Watcher & Device
    participant Pods as Apple AirPods

    UI->>BT: StartMonitoringAsync()
    BT->>WinRT: Start BLE Advertisement Watcher
    Pods-->>WinRT: Broadcast Apple BLE Manufacturer Beacon
    WinRT-->>BT: Received Advertisement (CompanyId 0x004C)
    BT->>BT: Parse Battery & Ear Placement
    BT-->>UI: OnAirPodsTelemetryUpdated(BatteryInfo)
    UI->>BT: ConnectAudioAsync(address)
    BT->>WinRT: BluetoothDevice.FromBluetoothAddressAsync()
    BT->>WinRT: Set Default Audio Endpoint
    BT-->>UI: Result.Success()
```
