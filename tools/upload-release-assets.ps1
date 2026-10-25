param(
  [Parameter(Mandatory = $true)][string]$Version,
  [switch]$IncludeZWCAD,
  [switch]$SkipBuild,
  [string]$RepoOwner = "thanhphuong-dev",
  [string]$RepoName = "CADAddinManager"
)

# Builds the locally-buildable host packages (VinaCAD, optionally ZWCAD) and uploads
# them as assets to the GitHub Release matching the tag, replacing existing assets.
# Requires a GitHub credential stored for git (Git Credential Manager) or gh auth.

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$artifactsDir = Join-Path $root "artifacts"

$tag = if ($Version.StartsWith("v")) { $Version } else { "v$Version" }
$normalizedVersion = $tag.TrimStart("v")

function Get-GitHubToken {
  $output = "protocol=https`nhost=github.com`n`n" | git credential fill 2>$null
  $line = $output | Where-Object { $_ -like "password=*" } | Select-Object -First 1
  if (-not $line) { throw "No GitHub credential found. Run 'gh auth login' or push once with Git Credential Manager." }
  return ($line.Substring("password=".Length).Trim())
}

function Invoke-GitHubApi {
  param([string]$Method, [string]$Uri, [string]$Token, [string]$Body)
  Invoke-RestMethod -Method $Method -Uri $Uri -Headers @{
    Authorization = "Bearer $Token"
    Accept        = "application/vnd.github+json"
    "User-Agent"  = "CADAddinManager-release"
  } -Body $Body -ContentType "application/json"
}

if (-not $SkipBuild) {
  & (Join-Path $PSScriptRoot "package-vinacad.ps1") -Version $Version

  $zwcadSdkFound = (Test-Path "C:\Program Files\ZWSOFT\ZWCAD 2021\ZwManaged.dll") -or
                   (Test-Path "C:\Program Files\ZWSOFT\ZWCAD 2026\ZwManaged.dll")
  if ($IncludeZWCAD -and -not $zwcadSdkFound) {
    Write-Warning "ZWCAD SDK was not found; skipping ZWCAD packaging."
    $IncludeZWCAD = $false
  }
  if ($IncludeZWCAD) {
    & (Join-Path $PSScriptRoot "package-zwcad.ps1") -Version $Version
  }
}

$assetFiles = @(
  (Join-Path $artifactsDir "CADAddinManager-VinaCAD-$Version-bundle.zip"),
  (Join-Path $artifactsDir "CADAddinManager-VinaCAD-$Version-setup.exe")
)
if ($IncludeZWCAD) {
  $assetFiles += (Get-ChildItem -Path $artifactsDir -Filter "CADAddinManager-ZWCAD$normalizedVersion-*.zip" -ErrorAction SilentlyContinue).FullName
}
$assetFiles = @($assetFiles | Where-Object { $_ -and (Test-Path $_) })

if ($assetFiles.Count -eq 0) {
  throw "No asset files found in '$artifactsDir'. Run the packaging scripts first."
}

$token = Get-GitHubToken
$release = Invoke-GitHubApi -Method Get -Uri "https://api.github.com/repos/$RepoOwner/$RepoName/releases/tags/$tag" -Token $token
Write-Host "Target release: $($release.name) (id $($release.id))"

$existingNames = @{}
foreach ($asset in $release.assets) { $existingNames[$asset.name] = $asset.id }

foreach ($file in $assetFiles) {
  $name = [IO.Path]::GetFileName($file)
  if ($existingNames.ContainsKey($name)) {
    Write-Host "Replacing existing asset: $name"
    Invoke-GitHubApi -Method Delete -Uri "https://api.github.com/repos/$RepoOwner/$RepoName/releases/assets/$($existingNames[$name])" -Token $token | Out-Null
  }

  $uploadUri = "https://uploads.github.com/repos/$RepoOwner/$RepoName/releases/$($release.id)/assets?name=$([uri]::EscapeDataString($name))"
  Invoke-RestMethod -Method Post -Uri $uploadUri -Headers @{
    Authorization = "Bearer $token"
    Accept        = "application/vnd.github+json"
    "User-Agent"  = "CADAddinManager-release"
  } -InFile $file -ContentType "application/octet-stream" | Out-Null
  Write-Host "Uploaded: $name"
}

Write-Host ""
Write-Host "Done. Release $tag now contains the locally-built assets."
