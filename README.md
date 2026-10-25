# CAD Add-in Manager

[English](README.md) | [Tiếng Việt](README.vi.md)

CAD Add-in Manager is a WPF development utility for loading, inspecting, and running external .NET CAD commands without repeatedly restarting the host application.

## Supported hosts

- AutoCAD 2020–2026.
- VinaCAD 2026.07.x using the VinaCAD/Teigha .NET 8 API.
- ZWCAD 2021 and ZWCAD 2026 using the bundled ZRX.NET API.
- Separate autoload bundles are provided for AutoCAD and VinaCAD.

## Commands

- `CADManager` — open the add-in manager.
- `CADManagerRunLast` — run the last selected command.
- `CADManagerDockPanel` — toggle the trace/debug palette.
- `CADListLoadedDlls` — display non-default loaded DLLs.
- `InitCADManager` — recreate the ribbon tab.
- `CADVinaCadTest` — verify VinaCAD command registration.

## Requirements

- Windows x64 and Visual Studio 2022 or newer.
- .NET Framework 4.8 Developer Pack for AutoCAD 2020–2024.
- .NET SDK 8.0 for AutoCAD 2025–2026 and VinaCAD 2026.
- VinaCAD 2026 with `TD_Mgd_Net8_26.12_17.dll` and `VinaCADCoreMgd_Net8.dll` for VinaCAD builds.

## Install from a release

Close the target CAD application before installing or updating.

### AutoCAD

Run `CADAddinManager-<version>-setup.exe`. The installer places the bundle in:

```text
%APPDATA%\Autodesk\ApplicationPlugins\CADAddinManager.bundle
```

### VinaCAD

Run `CADAddinManager-VinaCAD-<version>-setup.exe`. The installer places the bundle in:

```text
%APPDATA%\VinaCAD\ApplicationPlugins\CADAddinManager.bundle
```

Restart the CAD host and run `CADManager`. Autoload bundles do not require `NETLOAD`.

For manual installation, extract the matching `*-bundle.zip` and copy the contained `CADAddinManager.bundle` directory into the corresponding `ApplicationPlugins` directory above.

## Build configurations

| Configuration | Host |
|---|---|
| `Debug/Release A20` | AutoCAD 2020 |
| `Debug/Release A21` | AutoCAD 2021 |
| `Debug/Release A22` | AutoCAD 2022 |
| `Debug/Release A23` | AutoCAD 2023 |
| `Debug/Release A24` | AutoCAD 2024 |
| `Debug/Release A25` | AutoCAD 2025 |
| `Debug/Release A26` | AutoCAD 2026 |
| `Debug/Release V26` | VinaCAD 2026.07.x |
| `Debug/Release Z21` | ZWCAD 2021 |
| `Debug/Release Z26` | ZWCAD 2026 |

Example:

```powershell
dotnet restore CADAddinManager.sln --property:Configuration="Release A26"
dotnet build CADAddinManager.sln --configuration "Release A26" --no-restore
```

VinaCAD uses `C:\Program Files\VinaCAD\VinaCAD 2026` by default. Override it when necessary:

```powershell
dotnet build CADAddinManager.sln --configuration "Release V26" -p:VinaCadInstallDir="D:\VinaCAD 2026"
```

## Local bundle deployment

```powershell
# AutoCAD debug bundle
dotnet build CADAddinManager.sln --configuration "Debug A26" -p:DeployToAutoCAD=true

# VinaCAD release bundle
dotnet build CADAddinManager.sln --configuration "Release V26" -p:DeployToVinaCAD=true
```

## Release packaging

Create all AutoCAD and VinaCAD ZIP/EXE packages:

```powershell
.\tools\package-all.ps1 -Version v1.1.0
```

Individual packaging commands:

```powershell
.\tools\package-release.ps1 -Version v1.1.0
.\tools\package-vinacad.ps1 -Version v1.1.0
.\tools\build-installer.ps1 -Version v1.1.0
.\tools\build-vinacad-installer.ps1 -Version v1.1.0
```

Outputs are written to `artifacts/`. The VinaCAD package must be built on a machine with the VinaCAD SDK installed; the public GitHub-hosted workflow continues to build the AutoCAD packages.

## Project layout

```text
src/CADAddinManager/
  Command/                       CAD command entry points
  Model/                         Add-in loading and utilities
  View/                          WPF views and controls
  ViewModel/                     UI state and commands
  PackageContents.xml            AutoCAD bundle manifest
  PackageContents.VinaCAD.xml    VinaCAD bundle manifest
```

## Contributing and license

See [CONTRIBUTING.md](CONTRIBUTING.md), [CONTRIBUTING.vi.md](CONTRIBUTING.vi.md), and [License.md](License.md).
