#if VINACAD
using Teigha.Runtime;
using Teigha.Windows;
#elif ZWCAD
using ZwSoft.ZwCAD.Runtime;
using ZwSoft.Windows;
#else
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
#endif
using CADAddinManager.Command;
using CADAddinManager.View.Control;
using ImageSource = System.Windows.Media.ImageSource;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Orientation = System.Windows.Controls.Orientation;

namespace CADAddinManager;

public class App : ICadCommand, IExtensionApplication
{
    public const string RibbonTitle = "Add-in Manager";
    public const string RibbonId = "CADAddinManager";

    [CommandMethod("CADManagerRibbon")]
    public override void Execute()
    {
        CreateRibbon();
    }

    public void Initialize()
    {
        CreateRibbon();
    }

    public void Terminate()
    {
    }

    private static void CreateRibbon()
    {
        RibbonControl ribbon = ComponentManager.Ribbon;
        if (ribbon == null)
        {
            ScheduleRibbonCreation();
            return;
        }

        RibbonTab rtab = ribbon.FindTab(RibbonId);
        if (rtab != null)
        {
            ribbon.Tabs.Remove(rtab);
        }

        rtab = new RibbonTab();
        rtab.Title = RibbonTitle;
        rtab.Id = RibbonId;
        ribbon.Tabs.Add(rtab);
        AddContentToTab(rtab);
    }

    private const int RibbonRetryIntervalMs = 500;
    private const int RibbonRetryLimit = 60;

    private static void ScheduleRibbonCreation()
    {
        int attempts = 0;
        var timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(RibbonRetryIntervalMs),
            DispatcherPriority.Background,
            (sender, args) =>
            {
                var retry = (DispatcherTimer)sender;
                if (ComponentManager.Ribbon != null)
                {
                    retry.Stop();
                    CreateRibbon();
                }
                else if (++attempts >= RibbonRetryLimit)
                {
                    retry.Stop();
                }
            },
            Dispatcher.CurrentDispatcher);
        timer.Start();
    }

    private static void AddContentToTab(RibbonTab rtab)
    {
        rtab.Panels.Add(AddPanelOne());
    }

    private static RibbonPanel AddPanelOne()
    {
        RibbonPanelSource rps = new RibbonPanelSource();
        rps.Title = "Add-in Manager";
        RibbonPanel rp = new RibbonPanel();
        rp.Source = rps;
        RibbonButton rci = new RibbonButton();
        rci.Name = "CAD Add-in Manager";
        rps.DialogLauncher = rci;
        //create button1
        var addinAssembly = typeof(App).Assembly;
        RibbonButton btnPythonShell = new RibbonButton
        {
            Orientation = Orientation.Vertical,
            AllowInStatusBar = true,
            Size = RibbonItemSize.Large,
            ResizeStyle = RibbonItemResizeStyles.NoResize,
            MinWidth = 64,
            Name = "Open CAD Manager",
            ShowImage = true,
            ShowText = true,
            Text = "Add-in\nManager",
            Description = "Open CAD Add-in Manager",
            Image = CreateManagerIcon(16),
            LargeImage = CreateManagerIcon(32),
            CommandHandler = new RelayCommand(new CADManagerCommand().Execute)
        };
        rps.Items.Add(btnPythonShell);
        //create button2
        RibbonButton btnConfig = new RibbonButton
        {
            Orientation = Orientation.Vertical,
            AllowInStatusBar = true,
            Size = RibbonItemSize.Large,
            ResizeStyle = RibbonItemResizeStyles.NoResize,
            MinWidth = 64,
            Name = "Run Last Command",
            ShowImage = true,
            ShowText = true,
            Text = "Run Last\nCommand",
            Description = "Run the last selected add-in command",
            Image = CreateRunIcon(16),
            LargeImage = CreateRunIcon(32),
            CommandHandler = new RelayCommand(new CADManagerRunLast().Execute)
        };
        rps.Items.Add(btnConfig);

        var btnLoadedDlls = new RibbonButton
        {
            Orientation = Orientation.Vertical,
            AllowInStatusBar = true,
            Size = RibbonItemSize.Large,
            ResizeStyle = RibbonItemResizeStyles.NoResize,
            MinWidth = 64,
            Name = "List Loaded DLLs",
            ShowImage = true,
            ShowText = true,
            Text = "Loaded\nDLLs",
            Description = "Show non-default DLLs loaded by AutoCAD",
            Image = CreateDllIcon(16),
            LargeImage = CreateDllIcon(32),
            CommandHandler = new RelayCommand(new ListLoadedDllsCommand().Execute)
        };
        rps.Items.Add(btnLoadedDlls);
        return rp;
    }
    private static ImageSource CreateManagerIcon(double size)
    {
        var group = CreateIconBase(size, "#2563EB");
        group.Children.Add(CreateGeometry("#FFFFFF", "M8,9 H24 V12 H8 Z M8,15 H24 V18 H8 Z M8,21 H18 V24 H8 Z", size));
        return FreezeIcon(group, size);
    }

    private static ImageSource CreateRunIcon(double size)
    {
        var group = CreateIconBase(size, "#16A34A");
        group.Children.Add(CreateGeometry("#FFFFFF", "M12,8 L24,16 L12,24 Z", size));
        return FreezeIcon(group, size);
    }

    private static ImageSource CreateDllIcon(double size)
    {
        var group = CreateIconBase(size, "#D97706");
        group.Children.Add(CreateGeometry("#FFFFFF", "M8,8 H14 V14 H8 Z M18,8 H24 V14 H18 Z M8,18 H14 V24 H8 Z M18,18 H24 V24 H18 Z", size));
        return FreezeIcon(group, size);
    }

    private static DrawingGroup CreateIconBase(double size, string background)
    {
        var group = new DrawingGroup();
        var scale = size / 32d;
        group.Transform = new ScaleTransform(scale, scale);
        group.Children.Add(new GeometryDrawing(
            new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(background)),
            null,
            Geometry.Parse("M6,3 H26 C27.66,3 29,4.34 29,6 V26 C29,27.66 27.66,29 26,29 H6 C4.34,29 3,27.66 3,26 V6 C3,4.34 4.34,3 6,3 Z")));
        return group;
    }

    private static GeometryDrawing CreateGeometry(string color, string path, double size)
    {
        return new GeometryDrawing(
            new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color)),
            null,
            Geometry.Parse(path));
    }

    private static ImageSource FreezeIcon(DrawingGroup group, double size)
    {
        var image = new DrawingImage(group);
        image.Freeze();
#if VINACAD
        var pixelSize = Math.Max(1, (int)Math.Round(size));
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawImage(image, new System.Windows.Rect(0, 0, pixelSize, pixelSize));
        }

        var bitmap = new RenderTargetBitmap(pixelSize, pixelSize, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
#else
        return image;
#endif
    }
}
