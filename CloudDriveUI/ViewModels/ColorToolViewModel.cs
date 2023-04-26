using Prism.Commands;
using System.Windows.Media;
using CloudDriveUI.Configurations;
using MaterialDesignThemes.Wpf;

namespace CloudDriveUI.ViewModels;

public class ColorToolViewModel : BindableBase
{
    private readonly AppConfiguration appConfiguration;

    public ColorToolViewModel(AppConfiguration appConfiguration)
    {
        this.appConfiguration = appConfiguration;
        ActiveScheme = ColorScheme.Primary;
        SelectedColor = appConfiguration.AppTheme.GetColor(ActiveScheme);
        ChangeHueCommand = new(ChangeHue);
        ChangeActiveSchemeCommand = new(ChangeActiveScheme);
    }

    private ColorScheme _activeScheme;
    public ColorScheme ActiveScheme
    {
        get => _activeScheme;
        set
        {
            if (_activeScheme != value)
            {
                _activeScheme = value;
                RaisePropertyChanged();
            }
        }
    }

    public DelegateCommand<object> ChangeActiveSchemeCommand { get; set; }
    public DelegateCommand<object> ChangeHueCommand { get; set; }

    private Color? _selectedColor;
    public Color? SelectedColor
    {
        get => _selectedColor;
        set
        {
            _selectedColor = value;
            RaisePropertyChanged();
        }
    }

    private void ChangeActiveScheme(object? obj)
    {
        if (obj is ColorScheme scheme)
        {
            ActiveScheme = scheme;
            SelectedColor = appConfiguration.AppTheme.GetColor(scheme);
        }
    }
    private void ChangeHue(object? obj)
    {
        if (obj is Color color)
        {
            SelectedColor = color;
            appConfiguration.AppTheme.ChangeColor(ActiveScheme, color);
        }
    }
}
