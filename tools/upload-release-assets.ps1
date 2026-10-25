param(
  [Parameter(Mandatory = $true)][string]$Version,
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

function Get-GitHubToken {
  $output = "protocol=https`nhost=github.com`n`n" | git credential fill 2>$null
  $line = $output | Where-Object { $_ -like "password=*" } | Select-Object -First 1
  if (-not $line) { throw "No GitHub credential found. Run 'gh auth login' or push once with Git Credential Manager." }
  return ($line.Substring("password=".Length).Trim())
}

function Invoke-GitHubApi {
  param([string]$Method, [string]$Uri, [string]$Token, [string]$Body)
  $headers = @{
    Authorization = "Bearer $Token"
    Accept        = "application/vnd.github+json"
    "User-Agent"  = "CADAddinManager-release"
  }
  if ($Body) {
    Invoke-RestMethod -Method $Method -Uri $Uri -Headers $headers -Body $Body -ContentType "application/json"
  } else {
    Invoke-RestMethod -Method $Method -Uri $Uri -Headers $headers
  }
}

if (-not $SkipBuild) {
  # VinaCAD needs its local SDK; ZWCAD and GstarCAD build from NuGet packages on any machine.
  try {
    & (Join-Path $PSScriptRoot "package-vinacad.ps1") -Version $Version
  } catch {
    Write-Warning "VinaCAD packaging failed: $($_.Exception.Message)"
  }
  try {
    & (Join-Path $PSScriptRoot "package-zwcad.ps1") -Version $Version
  } catch {
    Write-Warning "ZWCAD packaging failed: $($_.Exception.Message)"
  }
  try {
    & (Join-Path $PSScriptRoot "package-gstarcad.ps1") -Version $Version
  } catch {
    Write-Warning "GstarCAD packaging failed: $($_.Exception.Message)"
  }
}

$assetFiles = @(
  (Join-Path $artifactsDir "CADAddinManager-VinaCAD-$Version-bundle.zip"),
  (Join-Path $artifactsDir "CADAddinManager-VinaCAD-$Version-setup.exe"),
  (Join-Path $artifactsDir "CADAddinManager-GstarCAD-$Version.zip")
)
$assetFiles += (Get-ChildItem -Path $artifactsDir -Filter "CADAddinManager-ZWCAD*-$Version.zip" -ErrorAction SilentlyContinue).FullName
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
  $tmp = New-TemporaryFile
  $code = & curl.exe -s -o $tmp -w "%{http_code}" -X POST `
    -H "Authorization: Bearer $token" `
    -H "Accept: application/vnd.github+json" `
    -H "Content-Type: application/octet-stream" `
    --data-binary "@$file" `
    $uploadUri
  Remove-Item $tmp -ErrorAction SilentlyContinue
  if ($code -notmatch '^20\d') { throw "Upload failed for $name (HTTP $code)" }
  Write-Host "Uploaded: $name (HTTP $code)"
}

Write-Host ""
Write-Host "Done. Release $tag now contains the locally-built assets."
