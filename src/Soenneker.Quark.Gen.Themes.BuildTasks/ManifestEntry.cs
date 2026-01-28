namespace Soenneker.Quark.Gen.Themes.CssWriter;

internal readonly struct ManifestEntry
{
    public readonly string ThemeTypeName;
    public readonly string OutputPath;

    public ManifestEntry(string themeTypeName, string outputPath)
    {
        ThemeTypeName = themeTypeName;
        OutputPath = outputPath;
    }
}
