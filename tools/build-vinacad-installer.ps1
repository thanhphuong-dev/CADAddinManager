param(
  [string]$Version = "dev"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$artifactsDir = Join-Path $root "artifacts"
$zipPath = Join-Path $artifactsDir "CADAddinManager-VinaCAD-$Version-bundle.zip"
$installerSrc = Join-Path $artifactsDir "vinacad-installer-src"
$installerOut = Join-Path $artifactsDir "vinacad-installer"
$embeddedZip = Join-Path $installerSrc "CADAddinManager.VinaCAD.bundle.zip"
$exePath = Join-Path $artifactsDir "CADAddinManager-VinaCAD-$Version-setup.exe"

if (-not (Test-Path -LiteralPath $zipPath)) {
  & (Join-Path $PSScriptRoot "package-vinacad.ps1") -Version $Version
}

Remove-Item -LiteralPath $installerSrc -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $installerOut -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $exePath -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $installerSrc | Out-Null
Copy-Item -LiteralPath $zipPath -Destination $embeddedZip

@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>CADAddinManagerVinaCADSetup</AssemblyName>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
  </PropertyGroup>
  <ItemGroup>
    <EmbeddedResource Include="CADAddinManager.VinaCAD.bundle.zip" LogicalName="CADAddinManager.VinaCAD.bundle.zip" />
  </ItemGroup>
</Project>
'@ | Set-Content -LiteralPath (Join-Path $installerSrc "CADAddinManagerVinaCADSetup.csproj") -Encoding UTF8

@'
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;

const string bundleName = "CADAddinManager.bundle";
const string resourceName = "CADAddinManager.VinaCAD.bundle.zip";

Console.Title = "CAD Add-in Manager Setup for VinaCAD";
Console.WriteLine("CAD Add-in Manager Setup for VinaCAD 2026");
Console.WriteLine();

if (Process.GetProcessesByName("VinaCAD").Length > 0)
{
    Fail("VinaCAD is running. Close VinaCAD and run this installer again.");
    return;
}

var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
if (string.IsNullOrWhiteSpace(appData))
{
    Fail("Could not find the current user's AppData folder.");
    return;
}

var pluginsDir = Path.Combine(appData, "VinaCAD", "ApplicationPlugins");
var targetBundleDir = Path.Combine(pluginsDir, bundleName);
var tempRoot = Path.Combine(Path.GetTempPath(), "CADVinaCADSetup-" + Guid.NewGuid().ToString("N"));
var zipPath = Path.Combine(tempRoot, "bundle.zip");
var extractDir = Path.Combine(tempRoot, "extract");

try
{
    Directory.CreateDirectory(extractDir);
    Directory.CreateDirectory(pluginsDir);

    using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
    {
        if (resource is null)
        {
            throw new InvalidOperationException("The VinaCAD installer payload was not found.");
        }

        using var file = File.Create(zipPath);
        resource.CopyTo(file);
    }

    ZipFile.ExtractToDirectory(zipPath, extractDir);
    var sourceBundleDir = Path.Combine(extractDir, bundleName);
    if (!Directory.Exists(sourceBundleDir))
    {
        throw new DirectoryNotFoundException("The extracted VinaCAD bundle was not found.");
    }

    if (Directory.Exists(targetBundleDir))
    {
        Directory.Delete(targetBundleDir, recursive: true);
    }

    CopyDirectory(sourceBundleDir, targetBundleDir);
    Console.WriteLine("Installed successfully to:");
    Console.WriteLine(targetBundleDir);
    Console.WriteLine();
    Console.WriteLine("Start VinaCAD and run command: CADManager");
}
catch (Exception ex)
{
    Fail(ex.Message);
    return;
}
finally
{
    try { if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true); } catch { }
}

Console.WriteLine();
Console.WriteLine("Press Enter to close.");
Console.ReadLine();

static void CopyDirectory(string sourceDir, string targetDir)
{
    Directory.CreateDirectory(targetDir);
    foreach (var file in Directory.GetFiles(sourceDir))
    {
        File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), overwrite: true);
    }
    foreach (var directory in Directory.GetDirectories(sourceDir))
    {
        CopyDirectory(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
    }
}

static void Fail(string message)
{
    Console.Error.WriteLine("Install failed: " + message);
    Console.WriteLine();
    Console.WriteLine("Press Enter to close.");
    Console.ReadLine();
}
'@ | Set-Content -LiteralPath (Join-Path $installerSrc "Program.cs") -Encoding UTF8

dotnet publish (Join-Path $installerSrc "CADAddinManagerVinaCADSetup.csproj") `
  --configuration Release `
  --output $installerOut `
  /p:DebugType=None `
  /p:DebugSymbols=false

Copy-Item -LiteralPath (Join-Path $installerOut "CADAddinManagerVinaCADSetup.exe") -Destination $exePath
Write-Host "Created $exePath"
