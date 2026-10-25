using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using CADAddinManager.Command;
using CADAddinManager.Model;
using CADAddinManager.View;
using CADAddinManager.View.Control;
using Microsoft.Win32;
#if VINACAD
using Application = Prima.VinaCAD.ApplicationServices.Application;
#elif ZWCAD
using Application = ZwSoft.ZwCAD.ApplicationServices.Application;
#elif GSTARCAD
using Application = Gssoft.Gscad.ApplicationServices.Application;
#else
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;
#endif
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Localization = CADAddinManager.Model.Localization;

namespace CADAddinManager.ViewModel;

public class AddInManagerViewModel : ViewModelBase
{
    //public AddInPlugin ExternalCommandData { get; set; }
    public FrmAddInManager FrmAddInManager { get; set; }
    public AssemLoader AssemLoader { get; set; }

    public CADManagerBase MCADManagerBase { get; set; }

    private ObservableCollection<AddinModel> commandItems;
    
    public ObservableCollection<AddinModel> CommandItems
    {
        get => commandItems;
        set
        {
            if (value == commandItems) return;
            commandItems = value;
            OnPropertyChanged();
        }
    }
    private ObservableCollection<AddinModel> lispFunctionItems;

    public ObservableCollection<AddinModel> LispFunctionItems
    {
        get => lispFunctionItems;
        set
        {
            if (value == lispFunctionItems) return;
            lispFunctionItems = value;
            OnPropertyChanged();
        }
    }

    private AddinModel selectedCommandItem;

    public AddinModel SelectedCommandItem
    {
        get
        {
            if (selectedCommandItem != null && selectedCommandItem.IsParentTree == true && IsTabCmdSelected)
            {
                IsCanRun = false;
                MCADManagerBase.ActiveCmd = selectedCommandItem.Addin;
            }
            else if (selectedCommandItem != null && selectedCommandItem.IsParentTree == false && IsTabCmdSelected)
            {
                IsCanRun = true;
                MCADManagerBase.ActiveCmdItem = selectedCommandItem.AddinItem;
                MCADManagerBase.ActiveCmd = selectedCommandItem.Addin;
                VendorDescription = MCADManagerBase.ActiveCmdItem.Description;
            }
            else IsCanRun = false;
            return selectedCommandItem;
        }
        set => OnPropertyChanged(ref selectedCommandItem, value);
    }
    private AddinModel selectedLispItem;

    public AddinModel SelectedLispItem
    {
        get
        {
            if (selectedLispItem != null && selectedLispItem.IsParentTree == true && IsTabLispSelected)
            {
                MCADManagerBase.ActiveLisp = selectedLispItem.Addin;
            }
            else if (selectedLispItem != null && selectedLispItem.IsParentTree == false && IsTabLispSelected)
            {
                MCADManagerBase.ActiveLispItem = selectedLispItem.AddinItem;
                MCADManagerBase.ActiveLisp = selectedLispItem.Addin;
                VendorDescription = MCADManagerBase.ActiveLispItem.Description;
            }
            return selectedLispItem;
        }
        set => OnPropertyChanged(ref selectedLispItem, value);
    }

    public ICommand LoadCommand => new RelayCommand(LoadCommandClick);
    public ICommand ClearCommand => new RelayCommand(ClearCommandClick);

    public ICommand RemoveCommand => new RelayCommand(RemoveAddinClick);
    private ICommand _executeAddinCommand;
    public ICommand ExecuteAddinCommand => _executeAddinCommand ??= new RelayCommand(ExecuteAddinCommandClick);
    public ICommand OpenLcAssemblyCommand => new RelayCommand(OpenLcAssemblyCommandClick);
    public ICommand ReloadCommand => new RelayCommand(ReloadCommandClick);
    public ICommand FreshSearch => new RelayCommand(FreshSearchClick);

    private string searchText;

    public string SearchText
    {
        get => searchText;
        set
        {
            if (value == searchText) return;
            searchText = value;
            OnPropertyChanged();
            FreshSearchClick();
        }
    }

    private bool isCurrentVersion = true;

    public bool IsCurrentVersion
    {
        get => isCurrentVersion;
        set => OnPropertyChanged(ref isCurrentVersion, value);
    }

    private ObservableCollection<CadAddin> addinStartup;

    public ObservableCollection<CadAddin> AddInStartUps
    {
        get { return addinStartup ??= new ObservableCollection<CadAddin>(); }
        set => OnPropertyChanged(ref addinStartup, value);
    }

    public ObservableCollection<string> RecentDlls { get; } = new ObservableCollection<string>(UserPreferences.RecentDlls);

    public ICommand HelpCommand => new RelayCommand(HelpCommandClick);
    public ICommand ToggleLanguageCommand => new RelayCommand(ToggleLanguageClick);
    public ICommand ToggleThemeCommand => new RelayCommand(ToggleThemeClick);

    public string AppTitle => $"{Resource.AppName} {VersionText}";

    public string VersionText => "v" + typeof(AddInManagerViewModel).Assembly.GetName().Version.ToString(3);
#if Z21
    public string VersionRangeText => "ZWCAD 2021";
#elif Z26
    public string VersionRangeText => "ZWCAD 2026";
#elif GSTARCAD
    public string VersionRangeText => "GstarCAD 2026";
#elif VINACAD
    public string VersionRangeText => "VinaCAD 2026";
#else
    public string VersionRangeText => "AutoCAD 2020-2026";
#endif
    public string HelpText => Resource.Help;
    public string HelpTooltip => Resource.HelpTooltip;
    public string TempFilesText => Resource.TempFiles;
    public string TempFilesTooltip => Resource.TempFilesTooltip;
    public string SearchTooltip => Resource.SearchTooltip;
    public string RecentTooltip => Resource.RecentTooltip;
    public string LoadDllText => Resource.LoadDll;
    public string ReloadText => Resource.Reload;
    public string RemoveText => Resource.Remove;
    public string CommandsText => Resource.Commands;
    public string LispText => Resource.Autolisp;
    public string OutputText => "Output";
    public string OpenFolderText => "Open Folder";
    public string RunSelectedText => Resource.RunSelected;
    public string VendorDescriptionTooltip => "Vendor description";
    public string ContextLoadDllText => Resource.LoadDll;
    public string ContextReloadText => Resource.Reload;
    public string ContextRunText => Resource.ContextRun;
    public string ContextOpenLocationText => Resource.ContextOpenLocation;
    public string ContextRemoveText => Resource.Remove;

    public string LanguageBadge => UserPreferences.Language == "vi" ? "VI" : "EN";

    public Visibility ThemeLightVisibility => UserPreferences.Theme == "light" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ThemeDarkVisibility => UserPreferences.Theme == "dark" ? Visibility.Visible : Visibility.Collapsed;

    private void ToggleLanguageClick()
    {
        UserPreferences.Language = UserPreferences.Language == "vi" ? "en" : "vi";
        Localization.Apply(UserPreferences.Language);
        OnPropertyChanged(string.Empty);
    }

    private void ToggleThemeClick()
    {
        UserPreferences.Theme = UserPreferences.Theme == "dark" ? "light" : "dark";
        ThemeManager.Apply(FrmAddInManager);
        OnPropertyChanged(string.Empty);
    }

    private string vendorDescription = string.Empty;

    public string VendorDescription
    {
        get => vendorDescription;
        set => OnPropertyChanged(ref vendorDescription, value);
    }

    private bool isTabCmdSelected = true;

    public bool IsTabCmdSelected
    {
        get => isTabCmdSelected;
        set => OnPropertyChanged(ref isTabCmdSelected, value);
    }
    private bool isTabAppSelected;

    public bool IsTabLispSelected
    {
        get
        {
            if (isTabAppSelected) IsCanRun = false;
            return isTabAppSelected;
        }
        set => OnPropertyChanged(ref isTabAppSelected, value);
    }

    private bool isCanRun;

    public bool IsCanRun
    {
        get => isCanRun;
        set => OnPropertyChanged(ref isCanRun, value);
    }

    private void HelpCommandClick()
    {
        OpenWithShell(DefaultSetting.HelpUrl);
    }

    public AddInManagerViewModel()
    {
        Localization.Apply(UserPreferences.Language);
        AssemLoader = new AssemLoader();
        MCADManagerBase = CADManagerBase.Instance;
        CommandItems = FreshTreeItems(false, MCADManagerBase.CADManager.Commands);
        LispFunctionItems = FreshTreeItems(false, MCADManagerBase.CADManager.LispFunctions);
    }

    private ObservableCollection<AddinModel> FreshTreeItems(bool isSearchText, Addins addins)
    {
        var MainTrees = new ObservableCollection<AddinModel>();
        foreach (var keyValuePair in addins.AddinDict)
        {
            var addin = keyValuePair.Value;
            var title = keyValuePair.Key;
            var addinItemList = addin.ItemList;
            var addinModels = new List<AddinModel>();
            foreach (var addinItem in addinItemList)
            {
                if (isSearchText)
                {
                    if (addinItem.FullClassName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        addinModels.Add(new AddinModel(addinItem.FullClassName)
                        {
                            IsChecked = true,
                            Addin = addin,
                            AddinItem = addinItem,
                        });
                    }
                }
                else
                {
                    addinModels.Add(new AddinModel(addinItem.FullClassName)
                    {
                        IsChecked = true,
                        Addin = addin,
                        AddinItem = addinItem,
                    });
                }
            }
            var root = new AddinModel(title)
            {
                IsChecked = true,
                Children = addinModels,
                IsParentTree = true,
                Addin = addin,
            };
            root.Initialize();
            MainTrees.Add(root);
        }

        return MainTrees;
    }

    public void ExecuteAddinCommandClick()
    {
        try
        {
            if (SelectedCommandItem?.IsParentTree == false && FrmAddInManager != null)
            {
                MCADManagerBase.ActiveCmd = SelectedCommandItem.Addin;
                MCADManagerBase.ActiveCmdItem = SelectedCommandItem.AddinItem;
                CheckCountSelected(CommandItems, out var result);
                if (result > 0)
                {
                    FrmAddInManager.Close();
                    Execute();
                }
            }
            if (SelectedLispItem?.IsParentTree == false && FrmAddInManager != null)
            {
                MCADManagerBase.ActiveLisp = SelectedLispItem.Addin;
                MCADManagerBase.ActiveLispItem = SelectedLispItem.AddinItem;
                CheckCountSelected(LispFunctionItems, out var result);
                if (result > 0)
                {
                    //TODO
                    // FrmAddInManager.Close();
                    // Execute();

                }
            }
        }
        catch (Exception e)
        {
            MessageBox.Show(e.ToString());
        }
    }
    private void Execute()
    {
        MCADManagerBase.RunActiveCommand();
    }

    private void OpenLcAssemblyCommandClick()
    {
        bool flag = MCADManagerBase.ActiveCmd == null;
        if (flag) return;
        string path = MCADManagerBase.ActiveCmd.FilePath;
        if (!File.Exists(path))
        {
            ShowFileNotExit(path);
            return;
        }
        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"")
        {
            UseShellExecute = true
        });
    }

    private void ShowFileNotExit(string path)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(Resource.FileNotExit);
        sb.AppendLine("Path :");
        sb.AppendLine(path);
        MessageBox.Show(sb.ToString(), DefaultSetting.AppName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
    }

    private void CheckCountSelected(ObservableCollection<AddinModel> addinModels, out int result)
    {
        result = 0;
        foreach (var addinModel in addinModels)
        {
            if (addinModel.IsInitiallySelected) result++;
            foreach (var modelChild in addinModel.Children)
            {
                if (modelChild.IsInitiallySelected) result++;
            }
        }
    }

    private void LoadCommandClick()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = @"assembly files (*.dll)|*.dll|All files (*.*)|*.*";
        if (openFileDialog.ShowDialog() != true)
        {
            return;
        }
        var fileName = openFileDialog.FileName;
        if (!File.Exists(fileName)) return;
        LoadAssemblyCommand(fileName);
    }    private void ReloadCommandClick()
    {
        if (SelectedCommandItem == null)
        {
            SortedDictionary<string, Addin> Commands = MCADManagerBase.CADManager.Commands.AddinDict;
            SortedDictionary<string, Addin> OldCommands = new SortedDictionary<string, Addin>(Commands);
            foreach (var Command in OldCommands.Values)
            {
                string fileName = Command.FilePath;
                if (File.Exists(fileName)) MCADManagerBase.CADManager.LoadAddin(fileName, AssemLoader);
            }
            MCADManagerBase.CADManager.SaveToAimIni();
            CommandItems = FreshTreeItems(false, MCADManagerBase.CADManager.Commands);
            return;
        }
        bool flag = MCADManagerBase.ActiveCmd == null;
        if (flag) return;
        string path = MCADManagerBase.ActiveCmd.FilePath;
        if (!File.Exists(path)) return;
        LoadAssemblyCommand(path);
    }

    private bool LoadAssemblyCommand(string fileName)
    {
        var addinType = MCADManagerBase.CADManager.LoadAddin(fileName, AssemLoader);
        switch (addinType)
        {
            case AddinType.Invalid:
                MessageBox.Show(Resource.LoadInvalid);
                return false;
            case AddinType.Command:
                IsTabCmdSelected = true;
                FrmAddInManager.TabCommand.Focus();
                break;
            case AddinType.LispFunction:
                IsTabLispSelected = true;
                FrmAddInManager.TabLispFunction.Focus();
                break;
            case AddinType.Mixed:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        MCADManagerBase.CADManager.SaveToAimIni();
        CommandItems = FreshTreeItems(false, MCADManagerBase.CADManager.Commands);
        LispFunctionItems = FreshTreeItems(false, MCADManagerBase.CADManager.LispFunctions);
        UserPreferences.AddRecentDll(fileName);
        RefreshRecentDlls();
        return true;
    }

    /// <summary>Loads a DLL picked from the recent-files list; no-op when the file is missing.</summary>
    public void LoadRecentDll(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
        LoadAssemblyCommand(path);
    }

    private void RefreshRecentDlls()
    {
        RecentDlls.Clear();
        foreach (var path in UserPreferences.RecentDlls)
        {
            RecentDlls.Add(path);
        }
    }
    private void RemoveAddinClick()
    {
        try
        {
            if (IsTabCmdSelected)
            {
                foreach (var parent in CommandItems)
                {
                    if (parent.IsInitiallySelected)
                    {
                        MCADManagerBase.ActiveCmd = parent.Addin;
                        MCADManagerBase.ActiveCmdItem = parent.AddinItem;
                        if (MCADManagerBase.ActiveCmd != null)
                        {
                            MCADManagerBase.CADManager.Commands.RemoveAddIn(MCADManagerBase.ActiveCmd);
                        }
                        MCADManagerBase.ActiveCmd = null;
                        MCADManagerBase.ActiveCmdItem = null;
                        CommandItems = FreshTreeItems(false, MCADManagerBase.CADManager.Commands);
                        return;
                    }
                    foreach (var addinChild in parent.Children)
                    {
                        if (addinChild.IsInitiallySelected)
                        {
                            //Set Value to run for add-in command
                            MCADManagerBase.ActiveCmd = parent.Addin;
                            MCADManagerBase.ActiveCmdItem = addinChild.AddinItem;
                        }
                    }
                }

                if (MCADManagerBase.ActiveCmdItem != null)
                {
                    MCADManagerBase.ActiveCmd.RemoveItem(MCADManagerBase.ActiveCmdItem);
                    MCADManagerBase.ActiveCmd = null;
                    MCADManagerBase.ActiveCmdItem = null;
                }
                CommandItems = FreshTreeItems(false, MCADManagerBase.CADManager.Commands);
            }
            if (IsTabLispSelected)
            {
                foreach (var parent in LispFunctionItems)
                {
                    if (parent.IsInitiallySelected)
                    {
                        MCADManagerBase.ActiveLisp = parent.Addin;
                        MCADManagerBase.ActiveLispItem = parent.AddinItem;
                        if (MCADManagerBase.ActiveLisp != null)
                        {
                            MCADManagerBase.CADManager.LispFunctions.RemoveAddIn(MCADManagerBase.ActiveLisp);
                        }
                        MCADManagerBase.ActiveLisp = null;
                        MCADManagerBase.ActiveLispItem = null;
                        LispFunctionItems = FreshTreeItems(false, MCADManagerBase.CADManager.LispFunctions);
                        return;
                    }
                    foreach (var addinChild in parent.Children)
                    {
                        if (addinChild.IsInitiallySelected)
                        {
                            //Set Value to run for add-in command
                            MCADManagerBase.ActiveLisp = parent.Addin;
                            MCADManagerBase.ActiveLispItem = addinChild.AddinItem;
                        }
                    }
                }

                if (MCADManagerBase.ActiveLispItem != null)
                {
                    MCADManagerBase.ActiveLisp.RemoveItem(MCADManagerBase.ActiveLispItem);
                    MCADManagerBase.ActiveLisp = null;
                    MCADManagerBase.ActiveLispItem = null;
                }
                LispFunctionItems = FreshTreeItems(false, MCADManagerBase.CADManager.LispFunctions);
            }
            //Save All SetTings
            MCADManagerBase.CADManager.SaveToAimIni();
        }
        catch (Exception e)
        {
            MessageBox.Show(e.ToString());
        }
    }

    private void FreshSearchClick()
    {
        var flag = string.IsNullOrEmpty(searchText);
        if (IsTabCmdSelected)
        {
            if (flag)
            {
                CommandItems = FreshTreeItems(false, MCADManagerBase.CADManager.Commands);
                return;
            }
            CommandItems = FreshTreeItems(true, MCADManagerBase.CADManager.Commands);
        }
    }
    private void ClearCommandClick()
    {
        var tempFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Temp", DefaultSetting.TempFolderName);
        if (Directory.Exists(tempFolder))
        {
            OpenWithShell(tempFolder);
        }
    }

    private static void OpenWithShell(string target)
    {
        Process.Start(new ProcessStartInfo(target)
        {
            UseShellExecute = true
        });
    }

}
