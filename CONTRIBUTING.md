# Contributing

Thanks for helping improve CAD Add-in Manager.

## Development

1. Install Visual Studio 2022 or newer.
2. Install .NET SDK 8.0.
3. Install the .NET Framework 4.8 Developer Pack when building AutoCAD 2020-2024 targets.
4. Restore packages for the configuration you want to build, for example `dotnet restore CADAddinManager.sln --property:Configuration="Release A26"`.
5. Build the AutoCAD configuration you need, for example:

```powershell
dotnet build CADAddinManager.sln --configuration "Release A26"
```

## Pull Requests

- Keep changes focused.
- Include build or runtime validation notes.
- Update README or issue templates when public behavior changes.
- Add screenshots for visible UI changes.

## Issues

When reporting a bug, include the Windows version, AutoCAD version, build configuration, and any logs or screenshots that help reproduce the issue.
