# 🛡️ Net-Vanguard

**Net-Vanguard** is an advanced, high-performance network tracking and Windows Firewall management utility built using modern C# 12 and .NET 8. It leverages the raw speed of Event Tracing for Windows (ETW) and a decoupled, privilege-separated pipeline to deliver a robust, real-time networking dashboard.

## ✨ Key Features
* **Real-time ETW Network Monitoring:** Analyzes network traffic natively at the kernel level without injecting drivers or resorting to external packet sniffers.
* **Granular Process Tracking:** Sort network traffic (Upload, Download, Total Activity) per application natively in the UI.
* **Adapter & Domain Analytics:** View precisely which domains are communicating and how much data passes through individual Wi-Fi or Ethernet interfaces.
* **Native Windows Firewall Integration:** A fully integrated Firewall Rules dashboard lets you view, toggle, delete, and implicitly generate Block/Allow rules seamlessly using secure COM Interop APIs (`HNetCfg.FwPolicy2`).
* **WinUI 3 Fluent Dashboard:** A premium, fully-responsive dashboard engineered specifically for Windows 11 design idioms, featuring dark/light mode, `NavigationView` collapses, and high-performance `LiveCharts2` metrics.

## 🏗️ Architecture Design
Because deep system APIs (ETW tracking, Firewall rules) require strict Administrator privileges on Windows, Net-Vanguard securely segregates capabilities into two isolated processes communicating over a highly optimized Named Pipe (`NetVanguard_TrafficPipe` & `NetVanguard_CommandPipe`):

1. **NetVanguard.Daemon** (Elevated Backend): Runs as an Administrator background service. It hooks into kernel ETW network-send/receive providers to index traffic payloads, safely manipulates the Windows Defender Firewall COM object, and dispatches JSON updates.
2. **NetVanguard.App** (Standard UI Front-end): Runs gracefully as a standard User process. It consumes the Daemon's telemetry, aggregates traffic per-process via `ObservableCollection` models, and renders the stunning WinUI 3 UX components.

## 🚀 Getting Started

### Prerequisites
- Windows 10 (19041) or Windows 11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022 (with Windows App SDK / WinUI 3 workloads)

### Building & Running
You must run both the UI application and the background Daemon to track real-time changes accurately.

1. **Launch the Daemon (Requires Administrator):**
   Open a terminal **as Administrator** and execute:
   ```bash
   dotnet run --project NetVanguard.Daemon
   ```
2. **Launch the UI Dashboard:**
   In a separate secure terminal (standard user privileges are fine), run:
   ```bash
   dotnet run --project NetVanguard.App
   ```

## 🤝 Contributing
Contributions are heavily encouraged! Please review our [Contributing Guidelines](CONTRIBUTING.md) to understand the submission process before creating Pull Requests. Check out our formatted [BUG REPORT](.github/ISSUE_TEMPLATE/bug_report.md) or [FEATURE REQUEST](.github/ISSUE_TEMPLATE/feature_request.md) templates if you find any issues!

## 📜 License & Security
This project is licensed under the **PolyForm Noncommercial 1.0.0** License to prevent strict commercialization. For deep vulnerability reporting instructions, please review our [Security Policy](SECURITY.md).
