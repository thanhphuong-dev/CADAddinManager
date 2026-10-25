using System.IO;
using System.Reflection;
using System.Windows;
#if VINACAD
using Prima.VinaCAD.ApplicationServices;
using Teigha.Runtime;
#elif ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.Runtime;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
#endif
using CADAddinManager.Model;
using CADAddinManager.View;
using CADAddinManager.ViewModel;
#if VINACAD
using Application = Prima.VinaCAD.ApplicationServices.Application;
#elif ZWCAD
using Application = ZwSoft.ZwCAD.ApplicationServices.Application;
#else
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;
#endif
using Exception = System.Exception;
using MessageBox = System.Windows.MessageBox;

namespace CADAddinManager.Command;

public sealed class CADManagerBase
{
    public void ExecuteCommand(bool faceless)
    {
        var vm = new AddInManagerViewModel();
        if (_activeCmd != null && faceless)
        {
            RunActiveCommand();
            return;
        }

        var FrmAddInManager = new FrmAddInManager(vm);
        FrmAddInManager.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        // Application.ShowModelessWindow(FrmAddInManager);
        FrmAddInManager.SetCadAsWindowOwner();
        FrmAddInManager.Show();
    }

    public string ActiveTempFolder
    {
        get => _activeTempFolder;
        set => _activeTempFolder = value;
    }

//
// #if A25
//     public void RunActiveCommand()
//     {
//         var filePath = _activeCmd.FilePath;
//         if (!File.Exists(filePath))
//         {
//             MessageBox.Show("File not found: " + filePath, DefaultSetting.AppName, MessageBoxButton.OK,
//                 MessageBoxImage.Error);
//             return;
//         }
//         var alc = new AssemblyLoadContext(filePath);
//         Stream stream = null;
//         try
//         {
//             
//             
//             stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
//             Assembly assembly = alc.LoadFromStream(stream);
//             //var assembly = assemLoader.LoadAddinsToTempFolder(filePath, false);
//             WeakReference alcWeakRef = new WeakReference(alc, trackResurrection: true);
//             Type[] types = assembly.GetTypes();
//             foreach (Type type in types)
//             {
//                 List<MethodInfo> methodInfos = type.GetMethods().ToList();
//                 foreach (MethodInfo methodInfo in methodInfos)
//                 {
//                     CommandMethodAttribute commandAtt = (CommandMethodAttribute)methodInfo
//                         .GetCustomAttributes(typeof(CommandMethodAttribute), false)
//                         .FirstOrDefault();
//                     string fullName = string.Join(".", methodInfo.DeclaringType.Name, methodInfo.Name);
//                     if (commandAtt != null && fullName == Instance.ActiveCmdItem.FullClassName)
//                     {
//                         // clone the assembly to 
//                         // DO HERE
//                         Invoke(methodInfo);
//                         alc.Unload();
//                     }
//                 }
//             }
//             int counter = 0;
//             for (counter = 0; alcWeakRef.IsAlive && (counter < 10); counter++)
//             {
//                 alc = null;
//                 GC.Collect();
//                 GC.WaitForPendingFinalizers();
//             }
//             stream.Close();
//         }
//         catch (Exception e)
//         {
//             //Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(e.ToString());
//             alc?.Unload();
//             WeakReference alcWeakRef = new WeakReference(alc, trackResurrection: true);
//             for (int counter = 0; alcWeakRef.IsAlive && (counter < 10); counter++)
//             {
//                 alc = null;
//                 GC.Collect();
//                 GC.WaitForPendingFinalizers();
//             }
//             stream?.Close();
//         }
//         // finally
//         // {
//         //     assemLoader.UnhookAssemblyResolve();
//         //     assemLoader.CopyGeneratedFilesBack();
//         // }
//     }
// #else
     public void RunActiveCommand()
    {
        AssemLoader assemLoader = new AssemLoader();
        var filePath = _activeCmd.FilePath;
        if (!File.Exists(filePath))
        {
            MessageBox.Show("File not found: " + filePath, DefaultSetting.AppName, MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        try
        {
            assemLoader.HookAssemblyResolve();
            var assembly = assemLoader.LoadAddinsToTempFolder(filePath, false);
            if (assembly == null) return;
            else
            {
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    List<MethodInfo> methodInfos = type.GetMethods().ToList();
                    foreach (MethodInfo methodInfo in methodInfos)
                    {
                        CommandMethodAttribute commandAtt = (CommandMethodAttribute) methodInfo
                            .GetCustomAttributes(typeof(CommandMethodAttribute), false)
                            .FirstOrDefault();
                        string fullName = string.Join(".", methodInfo.DeclaringType.Name, methodInfo.Name);
                        if (commandAtt != null && fullName == Instance.ActiveCmdItem.FullClassName)
                        {
                            Invoke(methodInfo);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(e.ToString());
        }
        finally
        {
            assemLoader.UnhookAssemblyResolve();
            assemLoader.CopyGeneratedFilesBack();
        }
    }


    void Invoke(MethodInfo methodInfo)
    {
        Document doc = Application.DocumentManager.MdiActiveDocument;
#if VINACAD
        InvokeInDocumentContext(methodInfo, doc);
#else
        using (doc.LockDocument())
        {
            InvokeInDocumentContext(methodInfo, doc);
        }
#endif
    }

    private static void InvokeInDocumentContext(MethodInfo methodInfo, Document doc)
    {
            if (methodInfo.IsStatic)
            {
                methodInfo.Invoke(null, null);
                return;
            }

            try
            {
                if (methodInfo.DeclaringType != null)
                {
                    var instance = Activator.CreateInstance(methodInfo.DeclaringType);
                    methodInfo.Invoke(instance, null);
                }
            }
            catch (System.Reflection.TargetInvocationException e)
            {
                doc.Editor.WriteMessage(e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
    }

    public static CADManagerBase Instance
    {
        get
        {
            if (_instance == null)
            {
#pragma warning disable RCS1059 // Avoid locking on publicly accessible instance.
                lock (typeof(CADManagerBase))
                {
                    if (_instance == null)
                    {
                        _instance = new CADManagerBase();
                    }
                }
#pragma warning restore RCS1059 // Avoid locking on publicly accessible instance.
            }

            return _instance;
        }
    }

    private CADManagerBase()
    {
        _addinManager = new CADAddinManager.ViewModel.CADManager();
        _activeCmd = null;
        _activeCmdItem = null;
        _activeLisp = null;
        _activeLispItem = null;
    }

    public Addin ActiveCmd
    {
        get => _activeCmd;
        set => _activeCmd = value;
    }

    public AddinItem ActiveCmdItem
    {
        get => _activeCmdItem;
        set => _activeCmdItem = value;
    }

    public Addin ActiveLisp
    {
        get => _activeLisp;
        set => _activeLisp = value;
    }

    public AddinItem ActiveLispItem
    {
        get => _activeLispItem;
        set => _activeLispItem = value;
    }

    public CADAddinManager.ViewModel.CADManager CADManager
    {
        get => _addinManager;
        set => _addinManager = value;
    }

    private string _activeTempFolder = string.Empty;

    private static volatile CADManagerBase _instance;


    private Addin _activeCmd;

    private AddinItem _activeCmdItem;

    private Addin _activeLisp;

    private AddinItem _activeLispItem;

    private CADAddinManager.ViewModel.CADManager _addinManager;
}
