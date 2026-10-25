$ErrorActionPreference = "Continue"
$bundle = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins\CADAddinManager.bundle"
$base = "HKCU:\Software\Autodesk\AutoCAD"
$script:added = 0
$script:already = 0
$script:failed = 0

Get-ChildItem $base -ErrorAction SilentlyContinue | ForEach-Object {
  $versionKey = $_
  Get-ChildItem $versionKey.PSPath -ErrorAction SilentlyContinue | Where-Object { $_.PSChildName -like "ACAD-*" } | ForEach-Object {
    $acadKey = $_
    $profilesPath = Join-Path $acadKey.PSPath "Profiles"
    if (-not (Test-Path $profilesPath)) { return }
    Get-ChildItem $profilesPath -ErrorAction SilentlyContinue | ForEach-Object {
      $generalPath = Join-Path $_.PSPath "General"
      if (-not (Test-Path $generalPath)) { return }
      $subPath = ($generalPath -replace '^Microsoft\.PowerShell\.Core\\Registry::HKEY_CURRENT_USER\\', '')
      $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($subPath, $true)
      if (-not $key) {
        Write-Host "Khong ghi duoc (quyen): $subPath"
        $script:failed++
        return
      }
      $label = "$($versionKey.PSChildName)\$($acadKey.PSChildName)\$($_.PSChildName)"

      try {
        if ($key.GetValueNames() -notcontains "TrustedLocs") {
          $key.SetValue("TrustedLocs", [string[]]@($bundle), [Microsoft.Win32.RegistryValueKind]::MultiString)
          Write-Host "Created TrustedLocs -> $label"
          $script:added++
        }
        else {
          $kind = $key.GetValueKind("TrustedLocs")
          $existing = @($key.GetValue("TrustedLocs") | ForEach-Object { [string]$_ })
          if ($existing -contains $bundle) {
            Write-Host "Already trusted: $label"
            $script:already++
          }
          else {
            $key.SetValue("TrustedLocs", [string[]]($existing + $bundle), $kind)
            Write-Host "Appended -> $label"
            $script:added++
          }
        }
      }
      catch {
        Write-Host "Loi tai ${label}: $($_.Exception.Message)"
        $script:failed++
      }
      finally {
        $key.Close()
      }
    }
  }
}

Write-Host ""
Write-Host "Added: $script:added, already trusted: $script:already, failed: $script:failed"
Write-Host "Path: $bundle"
