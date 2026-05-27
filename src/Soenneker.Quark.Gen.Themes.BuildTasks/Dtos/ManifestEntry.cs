namespace Soenneker.Quark.Gen.Themes.BuildTasks.Dtos;

internal readonly struct ManifestEntry
{
    public readonly string ThemeTypeName;
    public readonly string OutputPath;
    public readonly bool BuildUnminified;
    public readonly bool BuildMinified;

    public ManifestEntry(string themeTypeName, string outputPath, bool buildUnminified, bool buildMinified)
    {
        ThemeTypeName = themeTypeName;
        OutputPath = outputPath;
        BuildUnminified = buildUnminified;
        BuildMinified = buildMinified;
    }
}
