using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
#if VINACAD
using Prima.VinaCAD.ApplicationServices;
using Teigha.Runtime;
#elif ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.Runtime;
#elif GSTARCAD
using Gssoft.Gscad.ApplicationServices;
using Gssoft.Gscad.Runtime;
#else
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Runtime;
#endif
using CADAddinManager.Model;
#if VINACAD
using Application = Prima.VinaCAD.ApplicationServices.Application;
#elif ZWCAD
using Application = ZwSoft.ZwCAD.ApplicationServices.Application;
#elif GSTARCAD
using Application = Gssoft.Gscad.ApplicationServices.Application;
#else
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;
#endif
using Clipboard = System.Windows.Clipboard;
using WpfButton = System.Windows.Controls.Button;
using WpfColor = System.Windows.Media.Color;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfOrientation = System.Windows.Controls.Orientation;
using Window = System.Windows.Window;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace CADAddinManager.Command;

public sealed class ListLoadedDllsCommand : ICadCommand
{
    private static LoadedDllsWindow window;

    [CommandMethod("CADListLoadedDlls")]
    public override void Execute()
    {
        var paths = LoadedDllCollector.GetThirdPartyDllPaths();

        Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
            $"\nNon-default loaded DLLs ({paths.Count}).\n");

        if (window == null || !window.IsVisible)
        {
            window = new LoadedDllsWindow(paths);
            window.Closed += (_, _) => window = null;
            window.SetCadAsWindowOwner();
            window.Show();
        }
        else
        {
            window.Refresh(paths);
            window.Activate();
        }
    }
}

internal static class LoadedDllCollector
{
    public static IReadOnlyList<string> GetThirdPartyDllPaths()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddManagedAssemblyPaths(paths);
        AddNativeModulePaths(paths);

        return paths
            .Where(path => !IsDefaultDll(path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddManagedAssemblyPaths(ISet<string> paths)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                AddDllPath(paths, assembly.Location);
            }
            catch (NotSupportedException)
            {
                // Dynamic/in-memory assemblies do not have a file location.
            }
            catch (FileNotFoundException)
            {
                // The assembly may have been loaded without its original file.
            }
        }
    }

    private static void AddNativeModulePaths(ISet<string> paths)
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            foreach (ProcessModule module in process.Modules)
            {
                AddDllPath(paths, module.FileName);
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Module enumeration can be denied by Windows process restrictions.
        }
        catch (InvalidOperationException)
        {
            // AutoCAD may exit while the module list is being enumerated.
        }
    }

    private static void AddDllPath(ISet<string> paths, string path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            !string.Equals(Path.GetExtension(path), ".dll", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        paths.Add(Path.GetFullPath(path));
    }

    private static bool IsDefaultDll(string path)
    {
        var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (IsUnderDirectory(path, windowsDirectory))
        {
            return true;
        }

        try
        {
            using var process = Process.GetCurrentProcess();
            var autoCadDirectory = Path.GetDirectoryName(process.MainModule?.FileName);
            if (IsUnderDirectory(path, autoCadDirectory))
            {
                return true;
            }
        }
        catch (InvalidOperationException)
        {
            // Fall through to the other default locations.
        }

        var normalizedPath = path.Replace('/', '\\');
        return normalizedPath.IndexOf("\\Microsoft\\EdgeWebView\\", StringComparison.OrdinalIgnoreCase) >= 0 ||
               normalizedPath.IndexOf("Autodesk FeaturedApps.bundle", StringComparison.OrdinalIgnoreCase) >= 0 ||
               normalizedPath.IndexOf("Autodesk Save to Web and Mobile.bundle", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsUnderDirectory(string path, string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return false;
        }

        var normalizedDirectory = Path.GetFullPath(directory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        var normalizedPath = Path.GetFullPath(path);
        return normalizedPath.StartsWith(normalizedDirectory, StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class LoadedDllsWindow : Window
{
    private readonly WpfTextBox logTextBox;
    private readonly TextBlock statusText;

    public LoadedDllsWindow(IReadOnlyList<string> paths)
    {
        Title = "Loaded DLLs - Non-default";
        Width = 980;
        Height = 620;
        MinWidth = 600;
        MinHeight = 350;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var grid = new Grid { Margin = new Thickness(12) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new TextBlock
        {
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(31, 41, 55)),
            Margin = new Thickness(0, 0, 0, 10)
        };
        grid.Children.Add(header);

        logTextBox = new WpfTextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            FontFamily = new WpfFontFamily("Consolas"),
            FontSize = 12,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            TextWrapping = TextWrapping.NoWrap,
            Padding = new Thickness(8)
        };
        Grid.SetRow(logTextBox, 1);
        grid.Children.Add(logTextBox);

        var footer = new DockPanel { Margin = new Thickness(0, 10, 0, 0) };
        statusText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(107, 114, 128))
        };
        footer.Children.Add(statusText);

        var buttonPanel = new StackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            HorizontalAlignment = WpfHorizontalAlignment.Right
        };
        var refreshButton = new WpfButton { Content = "Refresh", MinWidth = 85, Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(10, 5, 10, 5) };
        refreshButton.Click += (_, _) => Refresh(LoadedDllCollector.GetThirdPartyDllPaths());
        var copyButton = new WpfButton { Content = "Copy log", MinWidth = 95, Padding = new Thickness(10, 5, 10, 5) };
        copyButton.Click += (_, _) => CopyLog();
        buttonPanel.Children.Add(refreshButton);
        buttonPanel.Children.Add(copyButton);
        DockPanel.SetDock(buttonPanel, Dock.Right);
        footer.Children.Add(buttonPanel);

        Grid.SetRow(footer, 2);
        grid.Children.Add(footer);
        Content = grid;

        Refresh(paths);
    }

    public void Refresh(IReadOnlyList<string> paths)
    {
        logTextBox.Text = string.Join(Environment.NewLine, paths);
        statusText.Text = $"{paths.Count} non-default DLL(s)";
    }

    private void CopyLog()
    {
        Clipboard.SetText(logTextBox.Text ?? string.Empty);
        statusText.Text = "Log copied to clipboard";
    }
}
