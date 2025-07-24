using System;
using System.IO;

namespace SimpleTwitchEmoteSounds.Services;

public static class AppDataPathService
{
    private const string PackageId = "SimpleTwitchEmoteSounds";

    private static string? _appDataRoot;

    private static string AppDataRoot
    {
        get
        {
            if (_appDataRoot != null)
                return _appDataRoot;
            _appDataRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                PackageId
            );
            Directory.CreateDirectory(_appDataRoot);
            return _appDataRoot;
        }
    }

    public static string GetSettingsPath() => Path.Combine(AppDataRoot, "Settings");

    public static string GetAudioFilesPath() => Path.Combine(AppDataRoot, "AudioFiles");

    public static string GetLogsPath() => Path.Combine(AppDataRoot, "Logs");

    public static string GetSettingsFilePath(string fileName) =>
        Path.Combine(GetSettingsPath(), fileName);

    public static string GetAudioFilePath(string fileName) =>
        Path.Combine(GetAudioFilesPath(), fileName);

    public static string GetLogFilePath(string fileName) => Path.Combine(GetLogsPath(), fileName);

    static AppDataPathService()
    {
        Directory.CreateDirectory(GetSettingsPath());
        Directory.CreateDirectory(GetAudioFilesPath());
        Directory.CreateDirectory(GetLogsPath());
    }
}
