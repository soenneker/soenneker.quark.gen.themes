using Soenneker.Quark;
using Soenneker.Quark.Gen.Themes;
using Soenneker.Quark.Tokens;

namespace Soenneker.Quark.Gen.Themes.PackageSmoke;

[GenerateQuarkThemeCss("wwwroot/css/theme.css")]
public static class SmokeTheme
{
    public static Theme Build() => new()
    {
        Name = "Package smoke",
        Buttons = new ButtonOptions { BackgroundColor = BackgroundColor.Primary }
    };
}
