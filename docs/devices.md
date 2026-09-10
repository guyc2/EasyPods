---
title: AirPods Protocol & Beacon Decoding
tags:
  - easypods
  - airpods
  - beacons
  - ble
---

# AirPods Protocol & Beacon Decoding

This document details the reverse-engineered Apple BLE manufacturer advertisement specification used to retrieve AirPods battery levels and telemetry on Windows.

---

## 1. Beacon Format

When AirPods are in proximity or their case is opened, they broadcast BLE advertisements with Apple's Company Identifier:
- **Company ID**: `0x004C` (Apple Inc.)
- **Beacon Type**: `0x07` (Proximity / AirPods status)
- **Length**: `0x19` (25 bytes payload) or `0x1B` (27 bytes with header)

```
Byte Offset | Field                | Notes
---------------------------------------------------------------------------------
0x00 - 0x01 | Company ID           | 0x4C, 0x00 (Little Endian)
0x02        | Beacon Type          | 0x07 (AirPods status packet)
0x03        | Length               | Typically 0x19 (25 bytes)
0x04 - 0x05 | Device Model ID      | Identifies Gen 1/2/3/4, Pro 1/2, Max
0x06        | Status / Battery L/R | Upper nibble: Left Pod, Lower: Right Pod
0x07        | Status / Case & Chg  | Upper nibble: Case, Lower: Charging flags
0x08        | In-Ear Status        | Bit flags for Left and Right in-ear detection
```

---

## 2. Battery Value Translation

Battery nibbles range from `0` to `15`:
- `0x0` to `0xA` (0 - 10): Represents `level * 10%` (or scaled accurately).
- `0xF` (15): Pod / Case disconnected, out of range, or not reporting.

### Charging Flags
- Bit `0`: Left Pod charging
- Bit `1`: Right Pod charging
- Bit `2`: Case charging

---

## 3. Extensibility Design

While currently dedicated exclusively to Apple AirPods, the parser interface `IBeaconParser` is decoupled to allow future headphone types (e.g. Galaxy Buds, Sony WF) to be added without modifying the core Bluetooth watcher or UI layers.
