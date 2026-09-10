---
title: AirPods Protocol & Beacon Decoding
tags:
  - easypods
  - airpods
  - beacons
  - ble
  - hardware
---

# AirPods Protocol & Beacon Decoding

This document details the reverse-engineered Apple BLE manufacturer advertisement specification used by EasyPods to retrieve real-time AirPods telemetry on Windows 10/11 without requiring jailbreaking or non-standard drivers.

---

## 1. Supported AirPods Hardware Models

EasyPods natively identifies all Apple AirPods generations, including the newest releases:

| Model Enum | Friendly Name | Product ID (BE) | Product ID (LE) | Case / Port Type |
| :--- | :--- | :--- | :--- | :--- |
| `AirPodsGen1` | AirPods (1st Gen) | `0x0220` | `0x2002` | Lightning |
| `AirPodsGen2` | AirPods (2nd Gen) | `0x0F20` | `0x200F` | Lightning / Qi Wireless |
| `AirPodsGen3` | AirPods (3rd Gen) | `0x1320` | `0x2013` | MagSafe Lightning |
| `AirPodsGen4` | AirPods 4 | `0x1720` | `0x2017` | USB-C |
| `AirPodsGen4Anc` | AirPods 4 (ANC) | `0x1B20` / `0x1C20` | `0x201B` / `0x201C` | USB-C / Speaker Case |
| `AirPodsProGen1` | AirPods Pro (1st Gen) | `0x0E20` | `0x200E` | MagSafe Lightning |
| `AirPodsProGen2Lightning` | AirPods Pro 2 (Lightning) | `0x1420` | `0x2014` | MagSafe Lightning |
| `AirPodsProGen2UsbC` | AirPods Pro 2 (USB-C) | `0x2420` | `0x2024` | MagSafe USB-C (IP54) |
| `AirPodsMaxLightning` | AirPods Max (Lightning) | `0x0A20` | `0x200A` | Lightning |
| `AirPodsMaxUsbC` | AirPods Max (USB-C 2024) | `0x2720` / `0x2820` | `0x2027` / `0x2028` | USB-C (Lossless/Audio) |

> [!NOTE]
> In addition to BLE Product ID matching, EasyPods features a secondary **Advertised Name Heuristic Engine** that infers the specific revision if Windows caches generic or custom Bluetooth peripheral names.

---

## 2. BLE Proximity Advertisement Structure

When AirPods are in range or their charging case is flipped open, they broadcast BLE advertising frames under Apple's Manufacturer ID:
- **Company ID**: `0x004C` (Apple Inc., Little-Endian `[0x4C, 0x00]`)
- **Proximity Beacon Type**: `0x07`
- **Data Payload Length**: `0x19` (25 bytes payload) or up to 27 bytes

### Byte Offset Matrix (within Apple Manufacturer Data slice)

```
Byte Offset | Field                  | Description
-----------------------------------------------------------------------------------------
0x00        | Beacon Type            | 0x07 (AirPods status/proximity packet)
0x01        | Length                 | Typically 0x19 (25 bytes)
0x02 - 0x03 | Device Model ID        | 16-bit Product Identifier (BE or LE)
0x04        | Status Flags           | Reserved / Connection status
0x05        | Pod Battery Levels     | Upper nibble: Left Pod (0-10), Lower nibble: Right Pod (0-10)
0x06        | Case & Sensor Telemetry| Upper nibble: Case Level (0-10)
            |                        | Lower nibble: In-Ear & Lid detection bits
0x07        | Charging Indicators    | Bit 0: Left charging, Bit 1: Right, Bit 2: Case charging
0x08 - 0x1A | Encrypted Telemetry    | Additional Apple FindMy / UWB payload bytes
```

---

## 3. Battery & Charging Bitmask Translation

Battery nibbles range from `0` to `15` (`0x0` to `0xF`):
- `0` to `10` (`0x0` - `0xA`): Scaled to percentage `level * 10%` (`0%` to `100%`).
- `15` (`0xF`): Pod or Case is disconnected, in sleep mode, or out of range.

### Charging Flags (Byte 7)
- `bit 0` (`0b0000_0001`): Left Pod is charging (displayed with `⚡`).
- `bit 1` (`0b0000_0010`): Right Pod is charging (displayed with `⚡`).
- `bit 2` (`0b0000_0100`): Case is charging (displayed with `⚡`).

---

## 4. In-Ear Detection & Case Lid Sensor (Byte 6 Lower Nibble)

EasyPods extracts real-time placement sensors directly from the lower 4 bits of byte `0x06`:
- `bit 0` (`0b0000_0001`): **Left In-Ear** (`1` = placed in ear, `0` = in case or removed).
- `bit 1` (`0b0000_0010`): **Right In-Ear** (`1` = placed in ear, `0` = in case or removed).
- `bit 2` (`0b0000_0100`): **Case Lid Status** (`1` = lid flipped open, `0` = lid closed).

UI Representation in [[ui|Dashboard View]]:
- `👂 In Ear`: Active optical/capacitive skin detection sensor triggered.
- `📦 In Case`: Detected in charging cradle.
- `📂 Lid Open`: Immediate visual prompt when case is opened nearby.

---

## 5. Architectural Separation & Decoupling

The decoding pipeline follows [[core|Clean Architecture]]:
1. [[bluetooth|WindowsBluetoothService]] captures raw WinRT BLE manufacturer data sections.
2. `AirPodsBeaconParser` decodes payload bytes in pure C# without any UI or OS platform dependencies.
3. Returns a typed `Result<AirPodsTelemetry>` containing immutable `BatteryInfo` and `InEarStatus` record snapshots.
4. `AirPodsStatusViewModel` maps domain values to thread-safe ObservableProperties for WPF binding.
