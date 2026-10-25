param(
  [string]$Version = "dev",
  [switch]$SkipBuild
)
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$solution = Join-Path $root "CADAddinManager.sln"
$projectDir = Join-Path $root "src\CADAddinManager"
$artifactsDir = Join-Path $root "artifacts"
$configuration = "Release G26"
$outputDir = Join-Path $projectDir "bin\$configuration"
$zipPath = Join-Path $artifactsDir "CADAddinManager-GstarCAD-$Version.zip"

if (-not $SkipBuild) {
  dotnet restore $solution --property:Configuration="$configuration"
  dotnet build $solution --configuration "$configuration" --no-restore -p:Version="$($Version.TrimStart('v'))"
}
if (-not (Test-Path -LiteralPath (Join-Path $outputDir "CADAddinManager.dll"))) {
  throw "GstarCAD build output was not found in '$outputDir'."
}

$releaseDir = Join-Path $artifactsDir "gstarcad-release"
Remove-Item -LiteralPath $releaseDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
Copy-Item -Path (Join-Path $outputDir "*") -Destination $releaseDir -Recurse -Force
Set-Content -LiteralPath (Join-Path $releaseDir "INSTALL.txt") -Encoding UTF8 -Value @"
CAD Add-in Manager for GstarCAD 2026

Start GstarCAD, run NETLOAD and select CADAddinManager.dll,
then run the CADManager command. (Autoload bundle for GstarCAD is not provided yet.)
"@
Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $releaseDir "*") -DestinationPath $zipPath -Force
Write-Host "Created $zipPath"
