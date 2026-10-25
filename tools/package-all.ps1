param(
  [string]$Version = "dev"
)

$ErrorActionPreference = "Stop"

$toolsDir = $PSScriptRoot

& (Join-Path $toolsDir "package-release.ps1") -Version $Version
& (Join-Path $toolsDir "package-vinacad.ps1") -Version $Version
& (Join-Path $toolsDir "build-installer.ps1") -Version $Version
& (Join-Path $toolsDir "build-vinacad-installer.ps1") -Version $Version

Write-Host "Created all AutoCAD and VinaCAD release packages for $Version."
