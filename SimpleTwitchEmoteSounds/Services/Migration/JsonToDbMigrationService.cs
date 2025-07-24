using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Serilog;
using SimpleTwitchEmoteSounds.Data;
using SimpleTwitchEmoteSounds.Data.Entities;
using SimpleTwitchEmoteSounds.Models;

namespace SimpleTwitchEmoteSounds.Services.Migration;

public class JsonToDbMigrationService
{
    private readonly AppDbContext _context;
    private readonly string _settingsFolder;

    public JsonToDbMigrationService(AppDbContext context)
    {
        _context = context;
        var appLocation = AppDomain.CurrentDomain.BaseDirectory;
        _settingsFolder = Path.Combine(appLocation, "Settings");
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
        var appSettings = JsonConvert.DeserializeObject<AppSettings>(jsonContent);

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
                SoundFiles = sc.SoundFiles.Select(sf => new SoundFileEntity
                {
                    FileName = sf.FileName,
                    FilePath = sf.FilePath,
                    Percentage = sf.Percentage
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
}
