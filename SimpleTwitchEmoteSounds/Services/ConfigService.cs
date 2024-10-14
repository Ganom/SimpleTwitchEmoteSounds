using System;
using System.IO;
using System.Text.Json;
using Serilog;
using SimpleTwitchEmoteSounds.Common;
using SimpleTwitchEmoteSounds.Models;

namespace SimpleTwitchEmoteSounds.Services;

public static class ConfigService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public static readonly AppSettings Settings = InitConfig<AppSettings>("sounds");
    public static readonly UserState State = InitConfig<UserState>("user_state");

    static ConfigService()
    {
        SubscribeToChanges();
        Settings.RefreshSubscriptions();
    }

    private static void SubscribeToChanges()
    {
        State.PropertyChanged += (_, _) =>
        {
            Debouncer.Debounce("User Property Changed", () =>
            {
                Log.Information("User Property Changed");
                SaveConfig("user_state", State);
            });
        };
        Settings.PropertyChanged += (_, _) =>
        {
            Debouncer.Debounce("Sound Prop Changed", () =>
            {
                Log.Information("Sound Property Changed");
                SaveConfig("sounds", Settings);
            });
        };
        Settings.CollectionChanged += (_, _) =>
        {
            Debouncer.Debounce("Sound Collection Changed", () =>
            {
                Log.Information("Sound Settings Collection Changed");
                SaveConfig("sounds", Settings);
            });
        };
        Settings.SoundCommandPropertyChanged += (_, _) =>
        {
            Debouncer.Debounce("Sound Property Changed", () =>
            {
                Log.Information("Sound Property Changed");
                SaveConfig("sounds", Settings);
            });
        };
        Settings.SoundCommands.CollectionChanged += (_, _) =>
        {
            Debouncer.Debounce("Sound Sub Collection Changed", () =>
            {
                Log.Information("Sound Collection Changed");
                SaveConfig("sounds", Settings);
            });
        };
    }

    private static T InitConfig<T>(string name) where T : class, new()
    {
        var appLocation = AppDomain.CurrentDomain.BaseDirectory;
        var settingsFolder = Path.Combine(appLocation, "Settings");
        var configFilePath = Path.Combine(settingsFolder, $"{name}.json");

        if (!File.Exists(configFilePath))
        {
            Directory.CreateDirectory(settingsFolder);
            var defaultConfig = new T();
            SaveConfig(name, defaultConfig);
            return defaultConfig;
        }

        var configJson = File.ReadAllText(configFilePath);
        var config = new T();
        var configChanged = false;

        try
        {
            using var document = JsonDocument.Parse(configJson);
            var root = document.RootElement;
            foreach (var prop in typeof(T).GetProperties())
            {
                if (root.TryGetProperty(prop.Name, out var element))
                {
                    try
                    {
                        var value =
                            JsonSerializer.Deserialize(element.GetRawText(), prop.PropertyType, Options);
                        prop.SetValue(config, value);
                    }
                    catch (JsonException)
                    {
                        Log.Warning($"Failed to deserialize property {prop.Name}. Using default value.");
                        configChanged = true;
                    }
                }
                else
                {
                    Log.Warning($"Property {prop.Name} not found in config. Using default value.");
                    configChanged = true;
                }
            }
        }
        catch (JsonException ex)
        {
            Log.Error(ex, $"Error parsing {name} config. Using default values.");
            config = new T();
            configChanged = true;
        }

        if (configChanged)
        {
            SaveConfig(name, config, true);
        }

        return config;
    }

    private static void SaveConfig<T>(string name, T config, bool createBackup = false) where T : class
    {
        var appLocation = AppDomain.CurrentDomain.BaseDirectory;
        var settingsFolder = Path.Combine(appLocation, "Settings");
        var configFilePath = Path.Combine(settingsFolder, $"{name}.json");
        
        if (createBackup && File.Exists(configFilePath))
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var backupFilePath = Path.Combine(settingsFolder, $"{name}_backup_{timestamp}.json");
            File.Copy(configFilePath, backupFilePath);
            Log.Information($"Created backup of {name} config due to invalid properties: {backupFilePath}");
        }
        
        var configJson = JsonSerializer.Serialize(config, Options);
        File.WriteAllText(configFilePath, configJson);
        Log.Information($"Saved updated {name} config");
    }
}