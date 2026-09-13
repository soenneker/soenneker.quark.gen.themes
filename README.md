[![](https://img.shields.io/nuget/v/soenneker.quark.gen.themes.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.quark.gen.themes/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.quark.gen.themes/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.quark.gen.themes/actions/workflows/publish-package.yml)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.quark.gen.themes/build-and-test.yml?label=Build&style=for-the-badge)](https://github.com/soenneker/soenneker.quark.gen.themes/actions/workflows/build-and-test.yml)
[![](https://img.shields.io/nuget/dt/soenneker.quark.gen.themes.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.quark.gen.themes/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.quark.gen.themes/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.quark.gen.themes/actions/workflows/codeql.yml)

# Soenneker.Quark.Gen.Themes

Generates Quark component CSS and Tailwind theme tokens from a strongly typed `Theme` during the build.

## Install

```bash
dotnet add package Soenneker.Quark.Gen.Themes
```

## Usage

Add the attribute to a class that exposes exactly one public static method or property returning `Theme`. A factory method may optionally accept `IServiceProvider`.

```csharp
using Soenneker.Quark;
using Soenneker.Quark.Gen.Themes;
using Soenneker.Quark.Tokens;

[GenerateQuarkThemeCss("wwwroot/css/quark-theme.css")]
public static class MyTheme
{
    public static Theme Build() => new()
    {
        Name = "MyTheme",
        Tokens = new ThemeTokens
        {
            Light =
            {
                Primary = "oklch(0.623 0.214 259.815)",
                PrimaryForeground = "oklch(0.985 0 0)"
            }
        },
        Buttons = new ButtonOptions
        {
            Rounded = Rounded.Lg
        }
    };
}
```

With the example above, a successful build writes:

- `wwwroot/css/quark-theme.css`
- `wwwroot/css/quark-theme.min.css`
- `tailwind/quark-theme.generated.css`

Load either the full or minified runtime stylesheet from the application shell:

```html
<link rel="stylesheet" href="css/quark-theme.min.css" />
```

The Tailwind token file is consumed by `Soenneker.Quark.Gen.Tailwind`; it is not a replacement for the runtime component stylesheet.

The example's `Buttons` rule creates component CSS. A theme containing only tokens generates the Tailwind token input; the runner skips an empty component stylesheet. Load the separate runtime theme stylesheet only when your theme produces component rules.

Load the runtime theme CSS after the compiled Tailwind stylesheet:

```html
<link rel="stylesheet" href="css/quark-tailwind.min.css" />
<link rel="stylesheet" href="css/quark-theme.min.css" />
```

When this C# factory owns the tokens, avoid a competing explicit shadcn configuration in the Tailwind generator that would replace them. Runtime light/dark switching is a separate concern: use the `IThemeInterop` registered by Quark to select the active scheme. See the [theme walkthrough](https://quark.soenneker.com/themes) for both token-authoring approaches and a complete toggle example.

## Output options

```csharp
[GenerateQuarkThemeCss(
    "wwwroot/css/quark-theme.css",
    BuildUnminified = false,
    BuildMinified = true,
    BuildTailwind = true,
    TailwindOutputFilePath = "tailwind/quark-theme.generated.css")]
public static class MyTheme
{
    public static Theme Value { get; } = new() { Name = "MyTheme" };
}
```

Paths are resolved from the consuming project directory unless absolute. Build-wide MSBuild switches (`QuarkThemeBuildUnminified`, `QuarkThemeBuildMinified`, and `QuarkThemeBuildTailwind`) can disable an output category for all attributed themes in the project.

Theme factories execute during the build. Keep them deterministic and avoid runtime-only state, network access, or side effects.
