using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CADAddinManager.Model;

/// <summary>
/// Persisted user preferences (language, theme, recent DLLs) stored in a local ini file
/// under %LOCALAPPDATA%\CADAddinManager so they survive across CAD sessions.
/// </summary>
public static class UserPreferences
{
    private static readonly string prefsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CADAddinManager");

    private static readonly string prefsPath = Path.Combine(prefsFolder, "prefs.ini");

    private static IniFile ini;

    private static IniFile Prefs => ini ??= CreateIni();

    private static IniFile CreateIni()
    {
        Directory.CreateDirectory(prefsFolder);
        return new IniFile(prefsPath);
    }

    /// <summary>Interface language preference: "en" or "vi". Defaults to the Windows language until changed.</summary>
    public static string Language
    {
        get
        {
            var value = Read("General", "Language", string.Empty);
            if (value != "en" && value != "vi")
            {
                value = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "vi" ? "vi" : "en";
            }

            return value;
        }
        set => Prefs.Write("General", "Language", value);
    }

    /// <summary>Theme preference: "light" or "dark". Defaults to light.</summary>
    public static string Theme
    {
        get
        {
            var value = Read("General", "Theme", "light");
            return value == "dark" ? "dark" : "light";
        }
        set => Prefs.Write("General", "Theme", value);
    }

    public static List<string> RecentDlls
    {
        get
        {
            var list = new List<string>();
            for (var i = 0; i < MaxRecentDlls; i++)
            {
                var path = Read("RecentDlls", i.ToString(), string.Empty);
                if (!string.IsNullOrWhiteSpace(path) && !list.Contains(path))
                {
                    list.Add(path);
                }
            }

            return list;
        }
    }

    public const int MaxRecentDlls = 10;

    public static void AddRecentDll(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var items = RecentDlls;
        items.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
        items.Insert(0, path);
        Prefs.WriteSection("RecentDlls");
        for (var i = 0; i < Math.Min(items.Count, MaxRecentDlls); i++)
        {
            Prefs.Write("RecentDlls", i.ToString(), items[i]);
        }
    }

    private static string Read(string section, string key, string fallback)
    {
        var value = Prefs.ReadString(section, key);
        return string.IsNullOrEmpty(value) ? fallback : value;
    }
}
