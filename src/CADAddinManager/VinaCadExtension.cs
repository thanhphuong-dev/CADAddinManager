#if VINACAD
using Teigha.Runtime;
using CADAddinManager.Command;
using VinaCadApplication = Prima.VinaCAD.ApplicationServices.Application;

[assembly: ExtensionApplication(typeof(CADAddinManager.VinaCadExtension))]
[assembly: CommandClass(typeof(CADAddinManager.VinaCadExtension))]
[assembly: CommandClass(typeof(CADAddinManager.App))]
[assembly: CommandClass(typeof(CADAddinManager.Command.CADManagerCommand))]
[assembly: CommandClass(typeof(CADAddinManager.Command.CADManagerRunLast))]
[assembly: CommandClass(typeof(CADAddinManager.Command.DockPanelCommand))]
[assembly: CommandClass(typeof(CADAddinManager.Command.ListLoadedDllsCommand))]

namespace CADAddinManager;

public sealed class VinaCadExtension : IExtensionApplication
{
    public void Initialize()
    {
        try
        {
            new App().Execute();
        }
        catch (System.Exception exception)
        {
            System.Diagnostics.Trace.WriteLine(
                $"CAD ribbon could not be initialized during VinaCAD startup: {exception}");
        }
    }

    public void Terminate()
    {
    }

    [CommandMethod("CADVinaCadTest")]
    public void Test()
    {
        WriteMessage("\nCAD VinaCAD command registration is working.\n");
    }

    private static void WriteMessage(string message)
    {
        VinaCadApplication.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(message);
    }
}
#endif
