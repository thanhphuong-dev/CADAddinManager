using System;
using System.Linq;
using System.Windows;
using CADAddinManager.Model;

namespace CADAddinManager.View;

/// <summary>
/// Applies the light/dark theme to the add-in manager window. The user picks a fixed
/// theme (light or dark); there is no auto mode.
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
        return UserPreferences.Theme == "dark" ? "dark" : "light";
    }
}
