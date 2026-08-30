using System;

namespace Soenneker.Quark.Gen.Themes;

/// <summary>
/// Marks a class whose static <c>Theme</c> factory should be written as CSS during the build.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateQuarkThemeCssAttribute : Attribute
{
    public GenerateQuarkThemeCssAttribute(string outputFilePath)
    {
        OutputFilePath = outputFilePath;
    }

    /// <summary>
    /// Gets the component CSS output path, relative to the project directory unless absolute.
    /// </summary>
    public string OutputFilePath { get; }

    /// <summary>
    /// Gets or sets whether to write the unminified component stylesheet.
    /// </summary>
    public bool BuildUnminified { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to write the minified component stylesheet.
    /// </summary>
    public bool BuildMinified { get; set; } = true;

    /// <summary>
    /// Gets or sets the Tailwind token CSS output path, relative to the project directory unless absolute.
    /// </summary>
    public string TailwindOutputFilePath { get; set; } = "tailwind/quark-theme.generated.css";

    /// <summary>
    /// Gets or sets whether to write Tailwind token CSS from the theme tokens.
    /// </summary>
    public bool BuildTailwind { get; set; } = true;
}
