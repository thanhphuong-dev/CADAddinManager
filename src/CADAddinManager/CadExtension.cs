#if !VINACAD
#if ZWCAD
using ZwSoft.ZwCAD.Runtime;
#elif GSTARCAD
using Gssoft.Gscad.Internal;
using Gssoft.Gscad.Runtime;
#else
using Autodesk.AutoCAD.Runtime;
#endif
using CADAddinManager.Command;

[assembly: ExtensionApplication(typeof(CADAddinManager.App))]
[assembly: CommandClass(typeof(CADAddinManager.App))]
[assembly: CommandClass(typeof(CADAddinManager.Command.CADManagerCommand))]
[assembly: CommandClass(typeof(CADAddinManager.Command.CADManagerRunLast))]
[assembly: CommandClass(typeof(CADAddinManager.Command.DockPanelCommand))]
[assembly: CommandClass(typeof(CADAddinManager.Command.ListLoadedDllsCommand))]
#endif
