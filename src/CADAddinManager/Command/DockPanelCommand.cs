using System.Windows.Forms;
using System.Windows.Forms.Integration;
#if VINACAD
using Prima.VinaCAD.ApplicationServices;
using Teigha.Runtime;
#elif ZWCAD
using ZwSoft.ZwCAD.Runtime;
using ZwSoft.ZwCAD.Windows;
#elif GSTARCAD
using Gssoft.Gscad.Runtime;
using Gssoft.Gscad.Windows;
#else
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
#endif
using CADAddinManager.View.Control;

namespace CADAddinManager.Command;

public class DockPanelCommand : ICadCommand
{
    static PaletteSet pLogControl = null;
    [CommandMethod("CADManagerDockPanel")]
    public override void Execute()
    {
        if(pLogControl == null)
        {
#if VINACAD
            pLogControl = new PaletteSet("Trace/Debug Output", "CADManagerDockPanel",
                new Guid("42AF3E8F-E292-4651-A2A9-F6B9D7AB225D"));
#else
            pLogControl = new PaletteSet("Trace/Debug Output");
#endif
            pLogControl.MinimumSize = new System.Drawing.Size(300, 300);
            pLogControl.DockEnabled = (DockSides)((int)DockSides.Left + (int)DockSides.Right);
#if VINACAD
            pLogControl.Style = PaletteSetStyles.ShowCloseButton;
#else
            pLogControl.Style = PaletteSetStyles.ShowAutoHideButton
                                | PaletteSetStyles.ShowCloseButton
                                | PaletteSetStyles.ShowPropertiesMenu
                                | PaletteSetStyles.Snappable;
#endif
            LogControl Lib = new LogControl();
            ElementHost host = new ElementHost();
            host.AutoSize = true;
            host.Dock = DockStyle.Right;
            host.Child = Lib;
#if VINACAD
            pLogControl.AddVisual("LogControl", Lib);
#else
            pLogControl.Add("LogControl", host);
#endif
        }

        pLogControl.KeepFocus = true;
        pLogControl.Visible = !pLogControl.Visible;
    }
}
