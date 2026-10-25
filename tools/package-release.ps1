param(
  [string]$Version = "dev"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$solution = Join-Path $root "CADAddinManager.sln"
$projectDir = Join-Path $root "src\CADAddinManager"
$packageXml = Join-Path $projectDir "PackageContents.xml"
$artifactsDir = Join-Path $root "artifacts"
$releaseDir = Join-Path $artifactsDir "release"
$bundleRoot = Join-Path $releaseDir "CADAddinManager.bundle"

$versions = @(
  @{ Configuration = "Release A20"; Folder = "20" },
  @{ Configuration = "Release A21"; Folder = "21" },
  @{ Configuration = "Release A22"; Folder = "22" },
  @{ Configuration = "Release A23"; Folder = "23" },
  @{ Configuration = "Release A24"; Folder = "24" },
  @{ Configuration = "Release A25"; Folder = "25" },
  @{ Configuration = "Release A26"; Folder = "26" }
)

Remove-Item -LiteralPath $releaseDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $bundleRoot | Out-Null
Copy-Item -LiteralPath $packageXml -Destination (Join-Path $bundleRoot "PackageContents.xml")

foreach ($versionInfo in $versions) {
  $configuration = $versionInfo.Configuration
  $folder = $versionInfo.Folder
  $outputDir = Join-Path $projectDir "bin\$configuration"
  $bundleVersionDir = Join-Path $bundleRoot $folder

  dotnet restore $solution --property:Configuration="$configuration"
  dotnet build $solution --configuration "$configuration" --no-restore

  New-Item -ItemType Directory -Path $bundleVersionDir | Out-Null
  Copy-Item -Path (Join-Path $outputDir "*") -Destination $bundleVersionDir -Recurse -Force
}

$zipPath = Join-Path $artifactsDir "CADAddinManager-$Version-bundle.zip"
Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
Compress-Archive -Path $bundleRoot -DestinationPath $zipPath -Force

Write-Host "Created $zipPath"
