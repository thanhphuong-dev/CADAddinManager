param(
  [string]$Version = "dev",
  [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$normalizedVersion = $Version.TrimStart("v")

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$solution = Join-Path $root "CADAddinManager.sln"
$projectDir = Join-Path $root "src\CADAddinManager"
$configuration = "Release V26"
$outputDir = Join-Path $projectDir "bin\$configuration"
$manifest = Join-Path $projectDir "PackageContents.VinaCAD.xml"
$artifactsDir = Join-Path $root "artifacts"
$releaseDir = Join-Path $artifactsDir "vinacad-release"
$bundleRoot = Join-Path $releaseDir "CADAddinManager.bundle"
$contentsDir = Join-Path $bundleRoot "Contents\V26"
$zipPath = Join-Path $artifactsDir "CADAddinManager-VinaCAD-$Version-bundle.zip"

if (-not $SkipBuild) {
  dotnet restore $solution --property:Configuration="$configuration"
  dotnet build $solution --configuration "$configuration" --no-restore -p:Version="$normalizedVersion"
}

if (-not (Test-Path -LiteralPath (Join-Path $outputDir "CADAddinManager.dll"))) {
  throw "VinaCAD build output was not found in '$outputDir'. Run without -SkipBuild after closing VinaCAD."
}

Remove-Item -LiteralPath $releaseDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $contentsDir -Force | Out-Null
Copy-Item -LiteralPath $manifest -Destination (Join-Path $bundleRoot "PackageContents.xml")
Copy-Item -Path (Join-Path $outputDir "*") -Destination $contentsDir -Recurse -Force

Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
Compress-Archive -Path $bundleRoot -DestinationPath $zipPath -Force

Write-Host "Created $zipPath"
