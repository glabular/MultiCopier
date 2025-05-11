using MultiCopierWPF.Interfaces;
using System.IO;
using System.Text.Json;

namespace MultiCopierWPF.Shared;

public class SettingsManager : ISettingsService
{
    private readonly object _lock = new();

    private readonly string _settingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MultiCopier", "settings.json");

    // CA1869
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public Settings Load()
    {
        lock (_lock)
        {
            var settingsDirectory = Path.GetDirectoryName(_settingsFilePath) ?? throw new InvalidOperationException("Settings file path does not contain a directory.");

            if (!Directory.Exists(settingsDirectory))
            {
                Directory.CreateDirectory(settingsDirectory);
            }

            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<Settings>(json);

                if (settings is not null)
                {
                    return settings;
                }
                else
                {
                    throw new InvalidDataException("Failed to deserialize settings. JSON content is invalid or does not match the expected format.");
                }
            }
            else
            {
                var settings = new Settings();
                WriteSettingsToFile(settings);

                return settings;
            }
        }
    }

    public void Save(Settings settings)
    {
        lock (_lock)
        {
            WriteSettingsToFile(settings);
        }
    }

    private void WriteSettingsToFile(Settings settings)
    {
        var defaultJson = JsonSerializer.Serialize(settings, _jsonOptions);
        File.WriteAllText(_settingsFilePath, defaultJson);
    }
}
