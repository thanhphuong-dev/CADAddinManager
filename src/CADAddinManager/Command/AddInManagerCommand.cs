using System.Diagnostics;
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
using Exception = System.Exception;

namespace CADAddinManager.Command;


public class CADManagerCommand : ICadCommand
{
    
    [CommandMethod("CADManager",CommandFlags.Session)]
    public override void Execute()
    {
        Trace.Listeners.Clear();
        Trace.Listeners.Clear();
        CodeListener codeListener = new CodeListener();
        Trace.Listeners.Add(codeListener);
        CADManagerBase.Instance.ExecuteCommand(false);
    }
}

public class CADManagerRunLast  : ICadCommand
{
    [CommandMethod("CADManagerRunLast",CommandFlags.Session)]
    public override void Execute()
    {
        try
        {
            CADManagerBase.Instance.ExecuteCommand(true);
        }
        catch (Exception e)
        {
            MessageBox.Show(e.ToString());
            Trace.WriteLine(e.Message);
        }
    }
}

