using System;

namespace Soenneker.Quark.Gen.Themes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateQuarkThemeCssAttribute : Attribute
{
    public GenerateQuarkThemeCssAttribute(string outputFilePath)
    {
        OutputFilePath = outputFilePath;
    }

    public string OutputFilePath { get; }

    public bool BuildUnminified { get; set; } = true;

    public bool BuildMinified { get; set; } = true;

    public string TailwindThemeOutputFilePath { get; set; } = @"tailwind\quark-theme.generated.css";
}
