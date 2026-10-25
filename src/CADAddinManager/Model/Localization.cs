using System.Globalization;

namespace CADAddinManager.Model;

/// <summary>
/// Applies the configured language preference to the resource manager used by all UI strings.
/// </summary>
public static class Localization
{
    public static void Apply(string preference)
    {
        Resource.Culture = ResolveCulture(preference);
    }

    /// <summary>
    /// Returns the culture for a stored preference, or null to follow the operating system language.
    /// </summary>
    public static CultureInfo ResolveCulture(string preference)
    {
        switch (preference)
        {
            case "en":
                return new CultureInfo("en");
            case "vi":
                return new CultureInfo("vi");
            default:
                return null;
        }
    }
}
