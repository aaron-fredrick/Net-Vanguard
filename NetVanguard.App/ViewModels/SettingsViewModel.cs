using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using NetVanguard.App.Services;
using System.Collections.ObjectModel;

namespace NetVanguard.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;

    public ObservableCollection<string> AvailableThemes { get; } = new ObservableCollection<string>
    {
        "System Default",
        "Light",
        "Dark"
    };

    private string _selectedThemeString;
    public string SelectedThemeString
    {
        get => _selectedThemeString;
        set
        {
            if (SetProperty(ref _selectedThemeString, value))
            {
                OnSelectedThemeStringChanged(value);
            }
        }
    }

    public SettingsViewModel()
    {
        _settingsService = App.AppSettings;
        
        _selectedThemeString = _settingsService.Theme switch
        {
            ElementTheme.Light => "Light",
            ElementTheme.Dark => "Dark",
            _ => "System Default"
        };
    }

    private void OnSelectedThemeStringChanged(string value)
    {
        var theme = value switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        if (_settingsService.Theme != theme)
        {
            _settingsService.Theme = theme;
        }
    }
}
