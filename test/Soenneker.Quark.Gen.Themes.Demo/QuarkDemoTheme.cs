namespace Soenneker.Quark.Gen.Themes.Demo;

[GenerateQuarkThemeCss("wwwroot/css/quark-theme.css")]
public static class QuarkDemoTheme
{
    public static Theme Build() => new()
    {
        Name = "Quark Demo",
        Tokens = new ThemeTokens
        {
            Light =
            {
                Primary = "oklch(0.623 0.214 259.815)",
                PrimaryForeground = "oklch(0.985 0 0)"
            },
            Dark =
            {
                Primary = "oklch(0.809 0.105 251.813)",
                PrimaryForeground = "oklch(0.205 0 0)"
            }
        },
        Anchors = new AnchorOptions
        {
            Selector = "a",
            TextDecoration = TextDecoration.None,
            TextColor = "#2563ba"
        },
        Buttons = new ButtonOptions
        {
            BackgroundColor = "#2563eb",
            TextColor = "#ffffff",
            Rounded = "0.5rem",
            Padding = "0.5rem 1rem"
        },
        Cards = new CardOptions
        {
            BackgroundColor = "#ffffff",
            Rounded = "0.75rem",
            BoxShadow = "0 0.25rem 0.75rem rgba(0,0,0,0.08)"
        }
    };
}
