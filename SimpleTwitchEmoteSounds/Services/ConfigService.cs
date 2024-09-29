﻿using System;
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
            var defaultConfigJson =
                JsonSerializer.Serialize(defaultConfig, Options);
            File.WriteAllText(configFilePath, defaultConfigJson);
            return defaultConfig;
        }

        var configJson = File.ReadAllText(configFilePath);
        return JsonSerializer.Deserialize<T>(configJson) ?? new T();
    }

    private static void SaveConfig<T>(string name, T config) where T : class
    {
        var appLocation = AppDomain.CurrentDomain.BaseDirectory;
        var settingsFolder = Path.Combine(appLocation, "Settings");
        var configFilePath = Path.Combine(settingsFolder, $"{name}.json");
        var configJson = JsonSerializer.Serialize(config, Options);
        File.WriteAllText(configFilePath, configJson);
    }
}