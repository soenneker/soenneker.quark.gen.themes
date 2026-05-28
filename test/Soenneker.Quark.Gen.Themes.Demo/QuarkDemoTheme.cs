using Soenneker.Quark.Tokens;

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
            DecorationLine = DecorationLine.None,
            TextColor = TextColor.Primary
        },
        Buttons = new ButtonOptions
        {
            BackgroundColor = BackgroundColor.Primary,
            TextColor = TextColor.Blue.Is100,
            Rounded = Rounded.Lg,
            Padding = Padding.OnY.Is2.OnX.Is4
        },
        Cards = new CardOptions
        {
            BackgroundColor = BackgroundColor.Card,
            Rounded = Rounded.Xl,
            Shadow = Shadow.Sm
        }
    };
}
