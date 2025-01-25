using System;
using System.IO;
using Newtonsoft.Json;
using Serilog;
using SimpleTwitchEmoteSounds.Common;
using SimpleTwitchEmoteSounds.Models;

namespace SimpleTwitchEmoteSounds.Services;

public static class ConfigService
{
    private static readonly JsonSerializerSettings Options = new()
    {
        Formatting = Formatting.Indented
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
                Log.Debug("User Property Changed");
                SaveConfig("user_state", State);
            });
        };
        Settings.PropertyChanged += (_, _) =>
        {
            Debouncer.Debounce("Sound Prop Changed", () =>
            {
                Log.Debug("Sound Property Changed");
                SaveConfig("sounds", Settings);
            });
        };
        Settings.CollectionChanged += (_, _) =>
        {
            Debouncer.Debounce("Sound Collection Changed", () =>
            {
                Log.Debug("Sound Settings Collection Changed");
                SaveConfig("sounds", Settings);
            });
        };
        Settings.SoundCommandPropertyChanged += (_, _) =>
        {
            Debouncer.Debounce("Sound Property Changed", () =>
            {
                Log.Debug("Sound Property Changed");
                SaveConfig("sounds", Settings);
            });
        };
        Settings.SoundCommands.CollectionChanged += (_, _) =>
        {
            Debouncer.Debounce("Sound Sub Collection Changed", () =>
            {
                Log.Debug("Sound Collection Changed");
                SaveConfig("sounds", Settings);
            });
        };
    }

    private static string SettingsFolder()
    {
        var appLocation = AppDomain.CurrentDomain.BaseDirectory;
        var settingsFolder = Path.Combine(appLocation, "Settings");
        #if CUSTOM_FEATURE_INSTALLED
        // On Windows this resolves to %AppData%\program (AppData\Roaming\program)
        // On Linux this resolves to   ~/.config/program
        settingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SimpleTwitchEmoteSounds", "Settings");
        #endif
        return settingsFolder;
    }

    private static T InitConfig<T>(string name) where T : class, new()
    {
        var settingsFolder = SettingsFolder();
        var configFilePath = Path.Combine(settingsFolder, $"{name}.json");

        if (!File.Exists(configFilePath))
        {
            Directory.CreateDirectory(settingsFolder);
            var defaultConfig = new T();
            var defaultConfigJson = JsonConvert.SerializeObject(defaultConfig, Options);
            File.WriteAllText(configFilePath, defaultConfigJson);
            return defaultConfig;
        }

        var configJson = File.ReadAllText(configFilePath);
        return JsonConvert.DeserializeObject<T>(configJson) ?? new T();
    }

    private static void SaveConfig<T>(string name, T config) where T : class
    {
        var settingsFolder = SettingsFolder();
        var configFilePath = Path.Combine(settingsFolder, $"{name}.json");
        var configJson = JsonConvert.SerializeObject(config, Options);
        File.WriteAllText(configFilePath, configJson);
    }
}