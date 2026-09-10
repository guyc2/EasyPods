---
name: logging
description: Guidelines and utility interface for structured logging and telemetry across EasyPods.
---

# Logging Guidelines (`logging`)

This skill defines the structured logging and telemetry standards for **EasyPods**.

---

## 1. Principles
- **Centralized Telemetry**: All components log via `AppLogger` or `ILogger<T>`.
- **Structured Properties**: Include context (e.g. DeviceAddress, DeviceName, BatteryLevel, State) rather than string concatenation.
- **Log Levels**:
  - `Trace`: Raw Bluetooth advertisement hex bytes, frame payloads.
  - `Debug`: Device state transitions, beacon decoded events.
  - `Info`: App startup, connection established, device disconnected.
  - `Warning`: Transient Bluetooth retry, unknown beacon format, low battery alert.
  - `Error`: Connection handshake failure, WinRT hardware exception, unhandled failure.

---

## 2. Rules
- NEVER swallow exceptions without logging.
- NEVER log sensitive user data or personal identifiers.
- Ensure logging calls are fast and do not block Bluetooth processing threads.
