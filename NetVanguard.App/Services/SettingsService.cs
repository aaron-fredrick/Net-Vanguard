using Microsoft.UI.Xaml;
using System.Text.Json;

namespace NetVanguard.App.Services;

public class AppSettings
{
    public ElementTheme Theme { get; set; } = ElementTheme.Default;
}

public class SettingsService
{
    private static readonly string _appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NetVanguard");
    private static readonly string _settingsFilePath = Path.Combine(_appDataFolder, "settings.json");

    private AppSettings _currentSettings;

    public ElementTheme Theme
    {
        get => _currentSettings.Theme;
        set
        {
            if (_currentSettings.Theme != value)
            {
                _currentSettings.Theme = value;
                SaveSettings();
                ThemeChanged?.Invoke(this, value);
            }
        }
    }

    public event EventHandler<ElementTheme>? ThemeChanged;

    public SettingsService()
    {
        _currentSettings = LoadSettings();
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
        }
        catch (Exception)
        {
            // Ignore corrupted settings
        }
        return new AppSettings();
    }

    private void SaveSettings()
    {
        try
        {
            if (!Directory.Exists(_appDataFolder))
            {
                Directory.CreateDirectory(_appDataFolder);
            }
            var json = JsonSerializer.Serialize(_currentSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception)
        {
            // In a production app, log this
        }
    }
}
