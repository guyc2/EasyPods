# EasyPods — Workspace Rules & Development Guidelines

Welcome to **EasyPods**, a modern Windows desktop application built with **WPF (.NET 8.0)** to seamlessly manage, monitor, and assist with Bluetooth headphone connections, focused on **Apple AirPods** (battery status, case open/close detection, ear placement, quick connection) and architected for modular expansion.

This document outlines the mandatory architecture, coding standards, error handling, testing policies, and agent collaboration instructions for this repository.

---

## 1. Architectural Blueprint (Clean Architecture / Layered MVVM)

The project enforces strict separation of concerns across four core projects:

```
EasyPods/
├── docs/                           # Obsidian-compatible documentation (Docs-Driven Development)
├── .agents/                        # AI agent guidelines, rules, and specialized skills
├── src/
│   ├── EasyPods.Model/             # Domain Entities, Bluetooth Protocols, WinRT Services, Telemetry
│   │   ├── Common/                 # Result<T>, Failure types, AppLogger
│   │   ├── Entities/               # AirPodsDevice, BatteryInfo, ConnectionState, ModelType
│   │   ├── Bluetooth/              # IBluetoothService, WinRT WindowsBluetoothService
│   │   └── Protocols/AirPods/      # AirPodsBeaconParser, BLE Manufacturer Advertisement Decoders
│   │
│   ├── EasyPods.ViewModel/         # MVVM ViewModels, Commands, UI State Machines
│   │   ├── Common/                 # BaseViewModel, AsyncRelayCommand (CommunityToolkit.Mvvm)
│   │   └── ViewModels/             # MainViewModel, AirPodsStatusViewModel, TrayViewModel
│   │
│   └── EasyPods.View/              # WPF UI Application (XAML, Styling, Shell)
│       ├── App.xaml / MainWindow.xaml
│       ├── Controls/               # BatteryGauge, PodIndicator, StatusBadge
│       ├── Converters/             # Value converters (BooleanToVisibility, BatteryToBrush)
│       ├── Resources/              # Fluent Dark/Light Theme brushes, styles, icons
│       └── SystemTray/             # NotifyIcon, quick tray flyout
│
└── tests/
    └── EasyPods.Test/              # Unit & Integration Tests (xUnit / NUnit)
        ├── ModelTests/             # AirPods beacon parser, Result<T>, Bluetooth state
        └── ViewModelTests/         # ViewModel commands, state transitions, mocked services
```

### Layer Rules & Boundaries
1. **`EasyPods.View` (Presentation / XAML)**:
   - Contains ONLY XAML markup, windows, custom controls, resource dictionaries, and UI-specific converters.
   - Views bind strictly to ViewModels via DataContext.
   - **Rule**: Views must NEVER call Bluetooth APIs, disk storage, or direct business logic. Zero business logic in code-behind files.
2. **`EasyPods.ViewModel` (State & Orchestration)**:
   - Contains ViewModels utilizing `CommunityToolkit.Mvvm`.
   - Manages UI states (Connecting, Connected, Disconnected, Error, BatteryUpdate) and executes user actions.
   - Depends only on `EasyPods.Model`.
   - **Rule**: Pure C# logic. ViewModels must not directly import WPF visual types (`System.Windows.Controls`, `FrameworkElement`) to keep ViewModels 100% unit-testable.
3. **`EasyPods.Model` (Domain & Infrastructure)**:
   - Contains domain entities (`AirPodsDevice`, `BatteryInfo`), Bluetooth abstractions (`IBluetoothService`), WinRT Bluetooth BLE watchers, Apple manufacturer beacon decoders, and typed error results (`Result<T>`).
   - Pure domain and hardware integration. Zero dependency on `EasyPods.View` or `EasyPods.ViewModel`.
4. **`EasyPods.Test`**:
   - Houses isolated unit tests covering `EasyPods.Model` and `EasyPods.ViewModel`.

---

## 2. Coding & Quality Standards

### Null Safety & Immutability
- **Strict Null Safety**: Enable `#nullable enable` across all projects. Treat nullable warnings as errors.
- **Record Types & Immutability**: Use `record` or `readonly struct` for state snapshots, DTOs, and battery telemetry data (`BatteryInfo`, `BeaconPayload`).
- **Defensive Defaults**: Default to `readonly` fields and property getters unless mutation is explicitly required.

### Error Handling Policy (Typed Result Pattern)
- **No Swallowed Exceptions**: Never write empty `catch` blocks or ignore errors silently.
- **Typed Result Pattern**: Operations that can fail must return `Result<T>` or `Result`:
  ```csharp
  public async Task<Result<AirPodsDevice>> ConnectAsync(string deviceAddress, CancellationToken ct);
  ```
- **Explicit Failure Hierarchy**:
  - `BluetoothUnavailableFailure` (Bluetooth adapter disabled or missing)
  - `DeviceNotFoundFailure` (AirPods not detected in range)
  - `ConnectionFailedFailure` (Pairing or RFCOMM/GATT handshake failure)
  - `BeaconDecodeFailure` (Malformed Apple BLE payload)
- **User Feedback**: Map domain failures to clear, actionable notifications in the UI.

### Asynchronous Operations & Dispatching
- Always use `async` / `await` with `CancellationToken` for asynchronous I/O and Bluetooth scanning.
- Bluetooth events received on background threads must be safely marshaled to the UI thread in ViewModels or Views.

---

## 3. UI/UX & Design Guidelines

- **Fluent Design Aesthetics**: Modern Windows 11 design language with subtle acrylic/mica-like effects, smooth rounded corners, clean padding, and consistent iconography.
- **Dark & Light Theme**: Native support for Windows system theme switching (Dark & Light mode) with semantic resource brushes.
- **System Tray Integration**: Unobtrusive background operation with quick-glance battery levels, left/right pod indicators, and a single-click quick connect toggle.
- **Responsive Layout**: Resizable dashboard and compact mini-widget modes.

---

## 4. Agent Collaboration & Workflow Instructions

When generating or modifying code in this codebase, AI agents MUST follow these instructions:

1. **Static Analysis Zero-Tolerance**: Build with `TreatWarningsAsErrors=true`. There must be **zero warnings and zero errors**.
2. **Modular Edits**: Keep files small, focused, and single-purpose. Limit XAML and C# files to under 300 lines by extracting child controls and helper classes.
3. **Testing**: Write unit tests for all domain models, beacon parsers, and ViewModels under `tests/EasyPods.Test/`.
4. **Git Commit Standards**: Use conventional commits:
   - `feat:` New feature
   - `fix:` Bug fix
   - `docs:` Documentation updates
   - `refactor:` Code refactoring without behavioral change
   - `test:` Adding or updating tests
5. **Branching & GitFlow Policy**: NEVER develop or commit directly to the `main` branch. All development, refactoring, and sprint execution MUST occur on dedicated feature branches (e.g., `feature/<name>` or `feature/<name>-sprint-<number>`). Upon passing tests and PR code review, feature branches MUST be rebased onto the `develop` branch (using git rebase) and NOT merged. Merging `develop` into `main` is strictly reserved for the user or explicit user instruction. Immediately after successfully rebasing onto `develop`, the completed feature branch MUST be deleted (`git branch -d <branch_name>`) to maintain a clean repository.

---

## 5. Mandatory Documentation Policy (Docs-Driven Development)

All developers and AI agents working on this project MUST strictly follow the documentation synchronization policy:

1. **Documentation Synchronization**: Any addition, modification, refactoring, or deletion of code, modules, files, or architecture MUST be immediately updated in `/docs/` (`docs/index.md` and module notes).
2. **Docs-First Reference**: Before starting work on any feature or change, agents MUST consult `/docs/` to ground their implementation in existing specifications, system dependencies, and module flows.
3. **Obsidian-Style Markdown**: Documentation in `/docs/` MUST be written using Obsidian-style markdown conventions, including YAML frontmatter (`tags: [...]`, `aliases: [...]`), wikilinks (`[[note|Display Text]]`), Obsidian callouts (`> [!info]`, `> [!warning]`), and fenced Mermaid diagrams (` ```mermaid `).

---

## 6. Sprint-Based Feature Planning & Multi-Agent Execution Gates

For every new feature or major enhancement, the following workflow is mandatory:

1. **Sprint Breakdown & Architecture Plan Governance**:
   - Every feature plan MUST be broken into sequential, testable Sprints (steps).
   - Before finalizing the implementation plan, ask the user to confirm or specify the desired number of Sprints.
   - **Plan Governance**: Before starting implementation of any development plan or sprint, the plan MUST be reviewed and approved by the **Architecture Manager Subagent** (`manage-architecture` skill) to ensure Clean Architecture compliance, layer boundaries, MVVM separation, and domain purity.
2. **Dedicated Architecture, Test, Review & Maintenance Subagents**:
   - **Architecture Manager**: Governs plan creation, audits layer boundaries (`EasyPods.View`, `EasyPods.ViewModel`, `EasyPods.Model`), and enforces `/docs/` synchronization.
   - **Maintenance & Debugging**: Governed by the `manage-maintenance` skill. Responsible for proactive .NET SDK / NuGet package maintenance, static analysis zero-tolerance, and reactive bug triage with regression test coverage.
   - **Code Review**: Every Sprint implementation MUST be peer-reviewed by an independent Reviewer subagent using the `review-pr` skill. The Reviewer MUST always explicitly verify that centralized logging (`AppLogger`) and typed error handling (`Result`/`Failure`) are properly implemented without swallowed exceptions.
   - **Test Generation**: Tests MUST be written and verified by a dedicated Testing subagent to ensure unbiased coverage.
3. **Strict Execution Gates**:
   - Upon finishing a Sprint (code implementation + test coverage + review verification), execution MUST STOP.
   - The agent MUST present the Sprint summary to the user and wait for explicit permission before resuming to the next Sprint.

---

## 7. Configuration & Secrets Policy

- **Zero Hardcoded Secrets Policy**: Any credentials, telemetry keys, or configuration parameters must be loaded via appsettings or environment variables.
- **Template Synchronization**: Maintain a clean `appsettings.json` and `appsettings.example.json` in the view project.
