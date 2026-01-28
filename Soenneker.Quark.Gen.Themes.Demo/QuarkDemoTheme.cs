using Soenneker.Quark.Suite.Attributes;

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
            TextColor = "#2563ec"
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
        },
        BootstrapCssVariables = new BootstrapCssVariables
        {
            Colors = new BootstrapColorsCssVariables
            {
                Primary = "#2563eb",
                PrimaryRgb = "37, 99, 235",
                PrimaryTextEmphasis = "#1e40af",
                PrimaryBgSubtle = "#dbeafe",
                PrimaryBorderSubtle = "#93c5fd",
                Secondary = "#7c3aed",
                SecondaryRgb = "124, 58, 237",
                SecondaryTextEmphasis = "#5b21b6",
                SecondaryBgSubtle = "#ede9fe",
                SecondaryBorderSubtle = "#c4b5fd",
                Success = "#059669",
                SuccessRgb = "5, 150, 105",
                SuccessTextEmphasis = "#047857",
                SuccessBgSubtle = "#d1fae5",
                SuccessBorderSubtle = "#6ee7b7",
                Danger = "#dc2626",
                DangerRgb = "220, 38, 38",
                DangerTextEmphasis = "#b91c1c",
                DangerBgSubtle = "#fee2e2",
                DangerBorderSubtle = "#fca5a5",
                Warning = "#ea580c",
                WarningRgb = "234, 88, 12",
                WarningTextEmphasis = "#c2410c",
                WarningBgSubtle = "#fed7aa",
                WarningBorderSubtle = "#fdba74",
                Info = "#0891b2",
                InfoRgb = "8, 145, 178",
                InfoTextEmphasis = "#0e7490",
                InfoBgSubtle = "#cffafe",
                InfoBorderSubtle = "#67e8f9",
                Dark = "#1f2937",
                Light = "#f9fafb",
                Gray100 = "#f3f4f6",
                Gray200 = "#e5e7eb",
                Gray300 = "#d1d5db",
                Gray400 = "#9ca3af",
                Gray500 = "#6b7280",
                Gray600 = "#4b5563",
                Gray700 = "#374151",
                Gray800 = "#1f2937",
                Gray900 = "#111827"
            }
        }
    };
}
