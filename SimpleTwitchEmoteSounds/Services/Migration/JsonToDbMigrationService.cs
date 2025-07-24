using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Serilog;
using SharpHook.Data;
using SimpleTwitchEmoteSounds.Data;
using SimpleTwitchEmoteSounds.Data.Entities;
using SimpleTwitchEmoteSounds.Models;
using SimpleTwitchEmoteSounds.Services;

namespace SimpleTwitchEmoteSounds.Services.Migration;

public class JsonToDbMigrationService
{
    private readonly AppDbContext _context;
    private readonly IAudioPlaybackService _audioPlaybackService;
    private readonly string _settingsFolder;

    public JsonToDbMigrationService(AppDbContext context, IAudioPlaybackService audioPlaybackService)
    {
        _context = context;
        _audioPlaybackService = audioPlaybackService;
        _settingsFolder = AppDataPathService.GetSettingsPath();
    }

    public bool ShouldMigrate()
    {
        Log.Information("Checking if migration should run...");
        _context.Database.EnsureCreated();

        var hasJsonFiles = File.Exists(Path.Combine(_settingsFolder, "sounds.json")) || File.Exists(Path.Combine(_settingsFolder, "user_state.json"));
        Log.Debug("JSON files exist: {HasJsonFiles}", hasJsonFiles);

        var hasDbData = _context.AppSettings.Any() || _context.UserStates.Any();
        Log.Debug("Database has data: {HasDbData}", hasDbData);

        var shouldMigrate = hasJsonFiles && !hasDbData;
        Log.Information("Should migrate: {ShouldMigrate}", shouldMigrate);

        return shouldMigrate;
    }

    public void Migrate()
    {
        Log.Information("Starting migration from JSON files to SQLite database.");

        try
        {
            _context.Database.EnsureCreated();

            MigrateAppSettings();
            MigrateUserState();

            Log.Information("Saving changes to the database...");
            _context.SaveChanges();
            Log.Information("Changes saved successfully.");

            BackupJsonFiles();

            Log.Information("Migration completed successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during migration.");
            throw;
        }
    }

    public void MigrateFromSpecificFile(string soundsJsonPath)
    {
        Log.Information("Starting manual migration from specific sounds.json file: {FilePath}", soundsJsonPath);

        try
        {
            _context.Database.EnsureCreated();

            // Clear existing data
            var existingSettings = _context.AppSettings.FirstOrDefault();
            if (existingSettings != null)
            {
                _context.AppSettings.Remove(existingSettings);
                Log.Information("Cleared existing app settings from database.");
            }

            // Migrate from the specific file
            MigrateAppSettingsFromFile(soundsJsonPath);

            Log.Information("Saving changes to the database...");
            _context.SaveChanges();
            Log.Information("Changes saved successfully.");

            Log.Information("Manual migration completed successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during manual migration.");
            throw;
        }
    }

    private void MigrateAppSettings()
    {
        Log.Information("Starting AppSettings migration...");
        var soundsFilePath = Path.Combine(_settingsFolder, "sounds.json");
        if (!File.Exists(soundsFilePath))
        {
            Log.Warning("sounds.json not found, skipping AppSettings migration.");
            return;
        }

        Log.Debug("Reading AppSettings from {FilePath}", soundsFilePath);
        var jsonContent = File.ReadAllText(soundsFilePath);
        var appSettings = JsonConvert.DeserializeObject<LegacyAppSettings>(jsonContent);

        if (appSettings == null)
        {
            Log.Error("Failed to deserialize sounds.json or the file is empty. Skipping AppSettings migration.");
            return;
        }

        Log.Information("Successfully deserialized sounds.json. Migrating {Count} sound commands.", appSettings.SoundCommands.Count);

        var entity = new AppSettingsEntity
        {
            Id = 1,
            EnableHotkeyData = JsonConvert.SerializeObject(appSettings.EnableHotkey),
            SoundCommands = appSettings.SoundCommands.Select(sc => new SoundCommandEntity
            {
                Name = sc.Name,
                Category = sc.Category,
                Enabled = sc.Enabled,
                IsExpanded = sc.IsExpanded,
                PlayChance = sc.PlayChance,
                SelectedMatchType = (int)sc.SelectedMatchType,
                Volume = sc.Volume,
                TimesPlayed = sc.TimesPlayed,
                CooldownSeconds = sc.CooldownSeconds,
                AppSettingsId = 1,
                SoundFiles = sc.SoundFiles.Select(sf =>
                {
                    var fileName = sf.FileName;

                    if (!string.IsNullOrEmpty(sf.FilePath) && sf.FilePath != sf.FileName && File.Exists(sf.FilePath))
                    {
                        try
                        {
                            fileName = CopyFileToManagedStorage(sf.FilePath);
                            Log.Information("Migrated audio file: {OldPath} -> {NewFileName}", sf.FilePath, fileName);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "Failed to migrate audio file {FilePath}, keeping original filename {FileName}", sf.FilePath, sf.FileName);
                            fileName = sf.FileName;
                        }
                    }

                    return new SoundFileEntity
                    {
                        FileName = fileName,
                        Percentage = sf.Percentage
                    };
                }).ToList()
            }).ToList()
        };

        _context.AppSettings.Add(entity);
        Log.Information("AppSettings entity prepared and added to the context.");
    }

    private void MigrateAppSettingsFromFile(string soundsFilePath)
    {
        Log.Information("Starting AppSettings migration from file: {FilePath}", soundsFilePath);
        
        if (!File.Exists(soundsFilePath))
        {
            Log.Error("Specified sounds.json file not found: {FilePath}", soundsFilePath);
            throw new FileNotFoundException($"Sounds file not found: {soundsFilePath}");
        }

        Log.Debug("Reading AppSettings from {FilePath}", soundsFilePath);
        var jsonContent = File.ReadAllText(soundsFilePath);
        var appSettings = JsonConvert.DeserializeObject<LegacyAppSettings>(jsonContent);

        if (appSettings == null)
        {
            Log.Error("Failed to deserialize sounds.json or the file is empty: {FilePath}", soundsFilePath);
            throw new InvalidOperationException($"Failed to deserialize sounds.json: {soundsFilePath}");
        }

        Log.Information("Successfully deserialized sounds.json. Migrating {Count} sound commands.", appSettings.SoundCommands.Count);

        var entity = new AppSettingsEntity
        {
            Id = 1,
            EnableHotkeyData = JsonConvert.SerializeObject(appSettings.EnableHotkey),
            SoundCommands = appSettings.SoundCommands.Select(sc => new SoundCommandEntity
            {
                Name = sc.Name,
                Category = sc.Category,
                Enabled = sc.Enabled,
                IsExpanded = sc.IsExpanded,
                PlayChance = sc.PlayChance,
                SelectedMatchType = (int)sc.SelectedMatchType,
                Volume = sc.Volume,
                TimesPlayed = sc.TimesPlayed,
                CooldownSeconds = sc.CooldownSeconds,
                AppSettingsId = 1,
                SoundFiles = sc.SoundFiles.Select(sf =>
                {
                    var fileName = sf.FileName;

                    if (!string.IsNullOrEmpty(sf.FilePath) && sf.FilePath != sf.FileName && File.Exists(sf.FilePath))
                    {
                        try
                        {
                            fileName = CopyFileToManagedStorage(sf.FilePath);
                            Log.Information("Migrated audio file: {OldPath} -> {NewFileName}", sf.FilePath, fileName);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "Failed to migrate audio file {FilePath}, keeping original filename {FileName}", sf.FilePath, sf.FileName);
                            fileName = sf.FileName;
                        }
                    }

                    return new SoundFileEntity
                    {
                        FileName = fileName,
                        Percentage = sf.Percentage
                    };
                }).ToList()
            }).ToList()
        };

        _context.AppSettings.Add(entity);
        Log.Information("AppSettings entity prepared and added to the context.");
    }

    private void MigrateUserState()
    {
        Log.Information("Starting UserState migration...");
        var userStateFilePath = Path.Combine(_settingsFolder, "user_state.json");
        if (!File.Exists(userStateFilePath))
        {
            Log.Warning("user_state.json not found, skipping UserState migration.");
            return;
        }

        Log.Debug("Reading UserState from {FilePath}", userStateFilePath);
        var jsonContent = File.ReadAllText(userStateFilePath);
        var userState = JsonConvert.DeserializeObject<UserState>(jsonContent);

        if (userState == null)
        {
            Log.Error("Failed to deserialize user_state.json or the file is empty. Skipping UserState migration.");
            return;
        }

        Log.Information("Successfully deserialized user_state.json for user: {Username}", userState.Username);

        var entity = new UserStateEntity
        {
            Id = 1,
            Username = userState.Username,
            Height = userState.Height,
            Width = userState.Width,
            PosX = userState.PosX,
            PosY = userState.PosY
        };

        _context.UserStates.Add(entity);
        Log.Information("UserState entity prepared and added to the context.");
    }

    private void BackupJsonFiles()
    {
        Log.Information("Starting JSON file backup process...");
        var backupFolder = Path.Combine(_settingsFolder, "JsonBackup");
        Directory.CreateDirectory(backupFolder);
        Log.Debug("Backup directory is {BackupFolder}", backupFolder);

        var files = new[]
        {
            "sounds.json",
            "user_state.json"
        };
        foreach (var file in files)
        {
            var sourcePath = Path.Combine(_settingsFolder, file);
            if (File.Exists(sourcePath))
            {
                var backupPath = Path.Combine(backupFolder, $"{file}.backup");
                File.Copy(sourcePath, backupPath, true);
                File.Delete(sourcePath);
                Log.Information("Backed up {SourceFile} to {BackupFile} and removed original.", file, backupPath);
            }
            else
            {
                Log.Warning("Did not find {File} to back up, skipping.", file);
            }
        }
        Log.Information("JSON file backup process completed.");
    }

    private string CopyFileToManagedStorage(string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException($"Source file not found: {sourceFilePath}");

        var fileName = Path.GetFileName(sourceFilePath);
        var uniqueFileName = GetUniqueFileName(fileName);
        var destinationPath = _audioPlaybackService.GetManagedAudioPath(uniqueFileName);

        File.Copy(sourceFilePath, destinationPath, overwrite: false);
        Log.Information("Copied audio file to managed storage: {UniqueFileName}", uniqueFileName);

        return uniqueFileName;
    }

    private string GetUniqueFileName(string originalFileName)
    {
        var name = Path.GetFileNameWithoutExtension(originalFileName);
        var extension = Path.GetExtension(originalFileName);
        var counter = 1;
        var fileName = originalFileName;

        while (File.Exists(_audioPlaybackService.GetManagedAudioPath(fileName)))
        {
            fileName = $"{name}_{counter}{extension}";
            counter++;
        }

        return fileName;
    }
}

// Legacy models for migration only
internal class LegacyAppSettings
{
    public Hotkey EnableHotkey { get; set; } = new([]);
    public ObservableCollection<LegacySoundCommand> SoundCommands { get; set; } = [];
}

internal class LegacySoundCommand
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public ObservableCollection<LegacySoundFile> SoundFiles { get; set; } = [];
    public bool Enabled { get; set; } = true;
    public bool IsExpanded { get; set; } = true;
    public string PlayChance { get; set; } = "1";
    public SimpleTwitchEmoteSounds.Models.MatchType SelectedMatchType { get; set; } = SimpleTwitchEmoteSounds.Models.MatchType.StartsWith;
    public string Volume { get; set; } = "1";
    public int TimesPlayed { get; set; }
    public string CooldownSeconds { get; set; } = "0";
}

internal class LegacySoundFile
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Percentage { get; set; } = "1";
}
