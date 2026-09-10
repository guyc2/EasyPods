---
title: WPF UI & System Tray Architecture
tags:
  - easypods
  - wpf
  - xaml
  - mvvm
  - system-tray
---

# WPF UI & System Tray Architecture

This document describes the presentation architecture in `EasyPods.View`, styling tokens, and the system tray shell.

---

## 1. Project Organization (`EasyPods.View`)

- **`Controls/`**: Custom user controls:
  - `BatteryGauge.xaml`: Circular or bar indicator displaying 0-100% battery with charging animation.
  - `PodStatusCard.xaml`: Displays Left Pod, Right Pod, or Case status with distinct glyphs.
- **`Converters/`**:
  - `BatteryLevelToColorConverter.cs`: Maps high (>20%), medium, and low (<20%) levels to semantic theme brushes.
  - `BooleanToVisibilityConverter.cs`: Standard WPF visibility converter.
- **`Resources/`**:
  - `Theme.Dark.xaml` / `Theme.Light.xaml`: Fluent palette brushes.
  - `Typography.xaml`: Clean Segoe UI Variable typography scale.
- **`SystemTray/`**:
  - `TrayIconManager.cs`: Manages the Windows taskbar notify icon, context menu, and click-to-flyout interaction.

---

## 2. MVVM Separation

- Views contain **zero business logic**.
- All interactive controls bind to `ICommand` implementations (`AsyncRelayCommand`) provided by `EasyPods.ViewModel`.
- Dynamic UI updates are driven by property change notifications (`[ObservableProperty]`).
