namespace Soenneker.Quark.Gen.Themes.Demo;

[GenerateQuarkThemeCss("wwwroot/css/quark-theme.css")]
public static class QuarkDemoTheme
{
    public static Theme Build() => new()
    {
        Name = "Quark Demo",
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
            BorderRadius = "0.5rem",
            Padding = "0.5rem 1rem"
        },
        Cards = new CardOptions
        {
            BackgroundColor = "#ffffff",
            BorderRadius = "0.75rem",
            BoxShadow = "0 0.25rem 0.75rem rgba(0,0,0,0.08)"
        }
    };
}
