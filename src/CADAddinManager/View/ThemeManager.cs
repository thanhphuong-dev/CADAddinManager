using System;
using System.Linq;
using System.Windows;
using CADAddinManager.Model;
using Microsoft.Win32;

namespace CADAddinManager.View;

/// <summary>
/// Resolves and applies the light/dark theme to the add-in manager window.
/// "auto" follows the host application color scheme (AutoCAD registry profile);
/// light/dark force a fixed theme. VinaCAD and ZWCAD have no exposed color-scheme
/// setting, so "auto" falls back to light for them.
/// </summary>
public static class ThemeManager
{
    // Matches both the XAML-relative source ("Themes/Theme.Light.xaml") and the
    // runtime pack URI ("/CADAddinManager;component/View/Themes/Theme.Dark.xaml").
    private const string ThemeMarker = "Themes/Theme.";

    public static void Apply(Window window)
    {
        if (window == null) return;
        var theme = ResolveTheme();
        var dictionary = new ResourceDictionary
        {
            Source = new Uri($"/CADAddinManager;component/View/Themes/Theme.{theme}.xaml", UriKind.Relative)
        };

        var existing = window.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains(ThemeMarker) == true);
        if (existing != null)
        {
            window.Resources.MergedDictionaries.Remove(existing);
        }

        window.Resources.MergedDictionaries.Insert(0, dictionary);
    }

    public static string ResolveTheme()
    {
        var preference = UserPreferences.Theme;
        if (preference == "light" || preference == "dark")
        {
            return preference;
        }

        return DetectHostDarkTheme() ? "dark" : "light";
    }

    private static bool DetectHostDarkTheme()
    {
        try
        {
            using var autoCadKey = Registry.CurrentUser.OpenSubKey(@"Software\Autodesk\AutoCAD");
            if (autoCadKey == null) return false;

            foreach (var releaseName in autoCadKey.GetSubKeyNames())
            {
                using var releaseKey = autoCadKey.OpenSubKey(releaseName);
                var acads = releaseKey?.GetSubKeyNames() ?? Array.Empty<string>();
                foreach (var acadName in acads.Where(n => n.StartsWith("ACAD-", StringComparison.OrdinalIgnoreCase)))
                {
                    using var profilesKey = releaseKey.OpenSubKey(acadName + @"\Profiles");
                    foreach (var profileName in profilesKey?.GetSubKeyNames() ?? Array.Empty<string>())
                    {
                        using var generalKey = profilesKey.OpenSubKey(profileName + @"\General");
                        var scheme = generalKey?.GetValue("ColorScheme") as string;
                        if (scheme != null && scheme.IndexOf("Dark", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        catch
        {
            // Registry layout differs between versions; fall back to light.
        }

        return false;
    }
}
