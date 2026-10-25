param(
  [string]$Version = "dev",
  [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$normalizedVersion = $Version.TrimStart("v")
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$solution = Join-Path $root "CADAddinManager.sln"
$projectDir = Join-Path $root "src\CADAddinManager"
$artifactsDir = Join-Path $root "artifacts"

$versions = @(
  @{ Configuration = "Release Z21"; Product = "2021" },
  @{ Configuration = "Release Z26"; Product = "2026" }
)

New-Item -ItemType Directory -Path $artifactsDir -Force | Out-Null
foreach ($versionInfo in $versions) {
  $configuration = $versionInfo.Configuration
  $product = $versionInfo.Product
  $outputDir = Join-Path $projectDir "bin\$configuration"
  $releaseDir = Join-Path $artifactsDir "zwcad-$product-release"
  $zipPath = Join-Path $artifactsDir "CADAddinManager-ZWCAD$product-$Version.zip"

  if (-not $SkipBuild) {
    dotnet restore $solution --property:Configuration="$configuration"
    dotnet build $solution --configuration "$configuration" --no-restore -p:Version="$normalizedVersion"
  }
  if (-not (Test-Path -LiteralPath (Join-Path $outputDir "CADAddinManager.dll"))) {
    throw "ZWCAD $product build output was not found in '$outputDir'."
  }

  Remove-Item -LiteralPath $releaseDir -Recurse -Force -ErrorAction SilentlyContinue
  New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
  Copy-Item -Path (Join-Path $outputDir "*") -Destination $releaseDir -Recurse -Force
  Copy-Item -LiteralPath (Join-Path $PSScriptRoot "install-zwcad.bat") -Destination $releaseDir -Force
  Set-Content -LiteralPath (Join-Path $releaseDir "INSTALL.txt") -Encoding UTF8 -Value @"
CAD Add-in Manager for ZWCAD $product

Run install-zwcad.bat, restart ZWCAD, then run CADManager.
If needed, use NETLOAD to select CADAddinManager.dll.
"@
  Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
  Compress-Archive -Path (Join-Path $releaseDir "*") -DestinationPath $zipPath -Force
  Write-Host "Created $zipPath"
}
