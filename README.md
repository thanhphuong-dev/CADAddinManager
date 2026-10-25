# CAD Add-in Manager

[![Build](https://github.com/thanhphuong-dev/CADAddinManager/actions/workflows/Workflow.yml/badge.svg)](https://github.com/thanhphuong-dev/CADAddinManager/actions/workflows/Workflow.yml)
[![Release](https://github.com/thanhphuong-dev/CADAddinManager/actions/workflows/release.yml/badge.svg)](https://github.com/thanhphuong-dev/CADAddinManager/actions/workflows/release.yml)
![Platform](https://img.shields.io/badge/platform-Windows%20x64-lightgrey)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](License.md)

[English](README.md) | [Tiếng Việt](README.vi.md)

CAD Add-in Manager is a developer utility for CAD platforms. It lets you load, inspect, and execute external .NET commands inside a running CAD session — without recompiling, reloading, or restarting the host application on every change.

## Features

- **Load external .NET assemblies** on demand and browse the CAD commands they expose.
- **Execute any command instantly** and re-run the most recent one with a single command.
- **Isolated loading** — assemblies are loaded from a shadow copy so source DLLs stay unlocked and rebuildable.
- **Diagnostics** — a dockable trace/log palette and an auditor that lists every non-default DLL the host has loaded.
- **Autoload bundles** — deploy once and the tool registers itself on the ribbon at startup; no `NETLOAD` required.

## Supported hosts

| Host | Versions | .NET runtime | Notes |
|---|---|---|---|
| AutoCAD | 2020–2026 | .NET Framework 4.8 (2020–2024), .NET 8 (2025–2026) | Builds against the AutoCAD .NET API NuGet packages |
| ZWCAD | 2021, 2026 | .NET Framework 4.8 | Builds against the ZRX.NET API installed with ZWCAD |
| VinaCAD | 2026.07.x | .NET 8 | Builds against the VinaCAD/Teigha .NET 8 SDK |

## Commands

| Command | Description | Availability |
|---|---|---|
| `CADManager` | Open the add-in manager window | All hosts |
| `CADManagerRunLast` | Re-run the most recently selected command | All hosts |
| `CADManagerDockPanel` | Toggle the dockable trace/debug palette | All hosts |
| `CADListLoadedDlls` | List non-default DLLs loaded by the host | All hosts |
| `CADManagerRibbon` | Recreate the *Add-in Manager* ribbon tab | All hosts (runs automatically at startup via the bundle) |
| `CADVinaCadTest` | Verify command registration in VinaCAD | VinaCAD builds only |

## Installation

Close the target CAD application before installing or updating.

### AutoCAD

1. Download `CADAddinManager-<version>-setup.exe` from the [Releases](https://github.com/thanhphuong-dev/CADAddinManager/releases) page and run it.
2. The installer places the bundle in:

   ```text
   %APPDATA%\Autodesk\ApplicationPlugins\CADAddinManager.bundle
   ```

### VinaCAD

1. Download and run `CADAddinManager-VinaCAD-<version>-setup.exe`.
2. The installer places the bundle in:

   ```text
   %APPDATA%\VinaCAD\ApplicationPlugins\CADAddinManager.bundle
   ```

### ZWCAD

1. Download and extract the ZWCAD package.
2. Run `install-zwcad.bat` from the extracted folder.
3. Restart ZWCAD and run `CADManager`. If autoload is unavailable, use `NETLOAD` to load the DLL manually.

### Manual installation

Extract the matching `*-bundle.zip` from the release assets and copy the contained `CADAddinManager.bundle` directory into the corresponding `ApplicationPlugins` folder shown above.

### First run

Restart the CAD host. The **Add-in Manager** tab appears on the ribbon — or simply type `CADManager` on the command line. With the autoload bundle installed, `NETLOAD` is never required.

## Build from source

### Prerequisites

- Windows x64
- Visual Studio 2022 or newer
- .NET Framework 4.8 Developer Pack (AutoCAD 2020–2024, ZWCAD builds)
- .NET 8 SDK (AutoCAD 2025–2026, VinaCAD builds)
- VinaCAD 2026 installed locally, for `V26` builds
- ZWCAD 2021 / 2026 installed locally, for `Z21` / `Z26` builds

### Configurations

Each supported host has a dedicated build configuration:

| Configuration | Host | Configuration | Host |
|---|---|---|---|
| `Debug/Release A20` | AutoCAD 2020 | `Debug/Release A25` | AutoCAD 2025 |
| `Debug/Release A21` | AutoCAD 2021 | `Debug/Release A26` | AutoCAD 2026 |
| `Debug/Release A22` | AutoCAD 2022 | `Debug/Release V26` | VinaCAD 2026 |
| `Debug/Release A23` | AutoCAD 2023 | `Debug/Release Z21` | ZWCAD 2021 |
| `Debug/Release A24` | AutoCAD 2024 | `Debug/Release Z26` | ZWCAD 2026 |

### Build

```powershell
dotnet restore CADAddinManager.sln --property:Configuration="Release A26"
dotnet build CADAddinManager.sln --configuration "Release A26" --no-restore
```

If an SDK is installed in a non-default location, point the build at it:

```powershell
# VinaCAD (default: C:\Program Files\VinaCAD\VinaCAD 2026)
dotnet build CADAddinManager.sln --configuration "Release V26" -p:VinaCadInstallDir="D:\VinaCAD 2026"

# ZWCAD (default: C:\Program Files\ZWSOFT\ZWCAD <version>)
dotnet build CADAddinManager.sln --configuration "Release Z26" -p:ZwcadInstallDir="D:\ZWSOFT\ZWCAD 2026"
```

## Local bundle deployment

For day-to-day development, deploy the bundle straight into the host's autoload folder:

```powershell
# AutoCAD debug bundle
dotnet build CADAddinManager.sln --configuration "Debug A26" -p:DeployToAutoCAD=true

# VinaCAD release bundle
dotnet build CADAddinManager.sln --configuration "Release V26" -p:DeployToVinaCAD=true
```

## Packaging and releases

Build every AutoCAD and VinaCAD ZIP/EXE package in one step:

```powershell
.\tools\package-all.ps1 -Version v1.1.0
```

Or run individual packaging steps:

```powershell
.\tools\package-release.ps1 -Version v1.1.0
.\tools\package-vinacad.ps1 -Version v1.1.0
.\tools\package-zwcad.ps1 -Version v1.1.0
.\tools\build-installer.ps1 -Version v1.1.0
.\tools\build-vinacad-installer.ps1 -Version v1.1.0
```

All artifacts are written to `artifacts/`.

Pushing a `v*` tag triggers the release workflow, which builds the AutoCAD packages on GitHub-hosted runners and attaches them to a GitHub Release automatically. VinaCAD and ZWCAD packages must be built locally on a machine with the corresponding SDK installed.

## Project layout

```text
src/CADAddinManager/
  Command/                       CAD command entry points
  Model/                         Add-in loading engine and utilities
  View/                          WPF views and controls
  ViewModel/                     UI state and commands
  PackageContents.xml            AutoCAD bundle manifest
  PackageContents.VinaCAD.xml    VinaCAD bundle manifest
tools/                           Packaging and installer scripts
.github/workflows/               CI build and release automation
```

## Contributing

Contributions are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md) (or [CONTRIBUTING.vi.md](CONTRIBUTING.vi.md)) before opening a pull request.

## License

This project is licensed under the [MIT License](License.md).
