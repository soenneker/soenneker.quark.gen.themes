using System;

namespace Soenneker.Quark.Gen.Themes;

/// <summary>
/// Represents the generate quark theme css attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateQuarkThemeCssAttribute : Attribute
{
    public GenerateQuarkThemeCssAttribute(string outputFilePath)
    {
        OutputFilePath = outputFilePath;
    }

    /// <summary>
    /// Gets output file path.
    /// </summary>
    public string OutputFilePath { get; }

    /// <summary>
    /// Gets or sets a value indicating whether build unminified.
    /// </summary>
    public bool BuildUnminified { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether build minified.
    /// </summary>
    public bool BuildMinified { get; set; } = true;

    /// <summary>
    /// Gets or sets the Tailwind token CSS output file path.
    /// </summary>
    public string TailwindOutputFilePath { get; set; } = "tailwind/quark-theme.generated.css";

    /// <summary>
    /// Gets or sets a value indicating whether Tailwind token CSS should be built.
    /// </summary>
    public bool BuildTailwind { get; set; } = true;
}
