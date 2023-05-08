using MaterialDesignColors;
using MaterialDesignColors.ColorManipulation;
using MaterialDesignThemes.Wpf;
using System.Windows.Media;

namespace CloudDriveUI.Configurations;

public class ThemeConfiguration
{
    private static readonly PaletteHelper _paletteHelper = new();
    private readonly ITheme _theme;
    private Color primary;
    private Color secondary;
    private Color primaryForeground;
    private Color secondaryForeground;

    public Color Primary
    {
        get => primary; set
        {
            primary = value;
            _theme.PrimaryLight = new ColorPair(value.Lighten());
            _theme.PrimaryMid = new ColorPair(value);
            _theme.PrimaryDark = new ColorPair(value.Darken());
            _paletteHelper.SetTheme(_theme);
        }
    }
    public Color Secondary
    {
        get => secondary; set
        {
            secondary = value;
            _theme.SecondaryLight = new ColorPair(value.Lighten());
            _theme.SecondaryMid = new ColorPair(value);
            _theme.SecondaryDark = new ColorPair(value.Darken());
            _paletteHelper.SetTheme(_theme);
        }
    }
    public Color PrimaryForeground
    {
        get => primaryForeground; set
        {
            primaryForeground = value;
            _theme.PrimaryLight = new ColorPair(_theme.PrimaryLight.Color, value);
            _theme.PrimaryMid = new ColorPair(_theme.PrimaryMid.Color, value);
            _theme.PrimaryDark = new ColorPair(_theme.PrimaryDark.Color, value);
            _paletteHelper.SetTheme(_theme);
        }
    }
    public Color SecondaryForeground
    {
        get => secondaryForeground; set
        {
            secondaryForeground = value;
            _theme.SecondaryLight = new ColorPair(_theme.SecondaryLight.Color, value);
            _theme.SecondaryMid = new ColorPair(_theme.SecondaryMid.Color, value);
            _theme.SecondaryDark = new ColorPair(_theme.SecondaryDark.Color, value);
            _paletteHelper.SetTheme(_theme);
        }
    }

    public ThemeConfiguration()
    {
        _theme = _paletteHelper.GetTheme();
        Primary = _theme.PrimaryMid.Color;
        Secondary = _theme.SecondaryMid.Color;
        PrimaryForeground = _theme.PrimaryMid.GetForegroundColor();
        SecondaryForeground = _theme.SecondaryMid.GetForegroundColor();
    }


    public Color GetColor(ColorScheme scheme)
    {
        return scheme switch
        {
            ColorScheme.Primary => Primary,
            ColorScheme.Secondary => Secondary,
            ColorScheme.PrimaryForeground => PrimaryForeground,
            ColorScheme.SecondaryForeground => SecondaryForeground,
            _ => throw new InvalidOperationException("switch ColorScheme 未完全"),
        };
    }
    public void ChangeColor(ColorScheme scheme, Color color)
    {
        switch (scheme)
        {
            case ColorScheme.Primary: Primary = color; break;
            case ColorScheme.Secondary: Secondary = color; break;
            case ColorScheme.PrimaryForeground: PrimaryForeground = color; break;
            case ColorScheme.SecondaryForeground: SecondaryForeground = color; break;
            default: throw new InvalidOperationException("switch ColorScheme 未完全");
        }
    }
}
