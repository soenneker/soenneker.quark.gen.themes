[![](https://img.shields.io/nuget/v/soenneker.quark.gen.themes.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.quark.gen.themes/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.quark.gen.themes/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.quark.gen.themes/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.quark.gen.themes.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.quark.gen.themes/)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Quark.Gen.Themes
### A source generator for generating Quark css files at compile time

## Installation

```
dotnet add package Soenneker.Quark.Gen.Themes
```

## Usage

Add the attribute to a class that exposes a single public static method or property returning a `Theme`.

```csharp
using Soenneker.Quark;
using Soenneker.Quark.Gen.Themes;

[GenerateQuarkThemeCss("wwwroot/css/quark-theme.css")]
public static class MyTheme
{
    public static Theme Build() => new()
    {
        Name = "MyTheme",
        Buttons = new ButtonOptions
        {
            // theme options here
        }
    };
}
```

The generator also recognizes `Soenneker.Quark.Suite.Attributes.GenerateQuarkThemeCssAttribute` if you already reference `Soenneker.Quark.Suite`.

During compilation, the generator will write the CSS file to the specified path so it can be referenced from HTML.

This package uses an MSBuild target to materialize the CSS file after `CoreCompile`. The generated CSS is embedded in compiler-generated files and extracted into your output path during the build.
