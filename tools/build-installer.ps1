param(
  [string]$Version = "dev"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$artifactsDir = Join-Path $root "artifacts"
$zipPath = Join-Path $artifactsDir "CADAddinManager-$Version-bundle.zip"
$installerSrc = Join-Path $artifactsDir "installer-src"
$installerOut = Join-Path $artifactsDir "installer"
$embeddedZip = Join-Path $installerSrc "CADAddinManager.bundle.zip"
$exePath = Join-Path $artifactsDir "CADAddinManager-$Version-setup.exe"

if (-not (Test-Path -LiteralPath $zipPath)) {
  & (Join-Path $PSScriptRoot "package-release.ps1") -Version $Version
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
    <AssemblyName>CADAddinManagerSetup</AssemblyName>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
  </PropertyGroup>
  <ItemGroup>
    <EmbeddedResource Include="CADAddinManager.bundle.zip" LogicalName="CADAddinManager.bundle.zip" />
  </ItemGroup>
</Project>
'@ | Set-Content -LiteralPath (Join-Path $installerSrc "CADAddinManagerSetup.csproj") -Encoding UTF8

@'
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;

const string bundleName = "CADAddinManager.bundle";

Console.Title = "CAD Add-in Manager Setup for AutoCAD";
Console.WriteLine("CAD Add-in Manager Setup for AutoCAD 2020-2026");
Console.WriteLine();

if (Process.GetProcessesByName("acad").Length > 0)
{
    Fail("AutoCAD is running. Close AutoCAD and run this installer again.");
    return;
}

var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
if (string.IsNullOrWhiteSpace(appData))
{
    Fail("Could not find the current user's AppData folder.");
    return;
}

var pluginsDir = Path.Combine(appData, "Autodesk", "ApplicationPlugins");
var targetBundleDir = Path.Combine(pluginsDir, bundleName);
var tempRoot = Path.Combine(Path.GetTempPath(), "CADAddinManagerSetup-" + Guid.NewGuid().ToString("N"));
var zipPath = Path.Combine(tempRoot, "bundle.zip");
var extractDir = Path.Combine(tempRoot, "extract");

try
{
    Directory.CreateDirectory(tempRoot);
    Directory.CreateDirectory(extractDir);
    Directory.CreateDirectory(pluginsDir);

    using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("CADAddinManager.bundle.zip"))
    {
        if (resource is null)
        {
            Fail("Installer payload was not found.");
            return;
        }

        using var file = File.Create(zipPath);
        resource.CopyTo(file);
    }

    ZipFile.ExtractToDirectory(zipPath, extractDir);
    var sourceBundleDir = Path.Combine(extractDir, bundleName);
    if (!Directory.Exists(sourceBundleDir))
    {
        Fail("The extracted bundle folder was not found.");
        return;
    }

    if (Directory.Exists(targetBundleDir))
    {
        Directory.Delete(targetBundleDir, recursive: true);
    }

    CopyDirectory(sourceBundleDir, targetBundleDir);

    Console.WriteLine("Installed successfully.");
    Console.WriteLine();
    Console.WriteLine("Installed to:");
    Console.WriteLine(targetBundleDir);
    Console.WriteLine();
    Console.WriteLine("Restart AutoCAD, then run command: CADManager");
    Console.WriteLine();
}
catch (Exception ex)
{
    Fail(ex.Message);
}
finally
{
    try
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
    catch
    {
    }
}

Console.WriteLine("Press Enter to close.");
Console.ReadLine();

static void CopyDirectory(string sourceDir, string targetDir)
{
    Directory.CreateDirectory(targetDir);

    foreach (var file in Directory.GetFiles(sourceDir))
    {
        var targetFile = Path.Combine(targetDir, Path.GetFileName(file));
        File.Copy(file, targetFile, overwrite: true);
    }

    foreach (var directory in Directory.GetDirectories(sourceDir))
    {
        var targetSubDir = Path.Combine(targetDir, Path.GetFileName(directory));
        CopyDirectory(directory, targetSubDir);
    }
}

static void Fail(string message)
{
    Console.Error.WriteLine("Install failed.");
    Console.Error.WriteLine(message);
    Console.WriteLine();
    Console.WriteLine("Press Enter to close.");
    Console.ReadLine();
}
'@ | Set-Content -LiteralPath (Join-Path $installerSrc "Program.cs") -Encoding UTF8

dotnet publish (Join-Path $installerSrc "CADAddinManagerSetup.csproj") `
  --configuration Release `
  --output $installerOut `
  /p:DebugType=None `
  /p:DebugSymbols=false

Copy-Item -LiteralPath (Join-Path $installerOut "CADAddinManagerSetup.exe") -Destination $exePath

Write-Host "Created $exePath"
