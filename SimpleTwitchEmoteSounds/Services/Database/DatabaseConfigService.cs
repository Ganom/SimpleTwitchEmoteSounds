#region

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;
using SharpHook.Data;
using SimpleTwitchEmoteSounds.Data;
using SimpleTwitchEmoteSounds.Data.Entities;
using SimpleTwitchEmoteSounds.Models;

#endregion

namespace SimpleTwitchEmoteSounds.Services.Database;

public class DatabaseConfigService
{
    private readonly AppDbContext _context;
    private AppSettings? _cachedSettings;
    private UserState? _cachedUserState;
    private readonly Timer _saveTimer;
    private bool _isDirty;

    private readonly PropertyChangedEventHandler _markDirtyHandler;
    private readonly NotifyCollectionChangedEventHandler _markDirtyCollectionHandler;

    public AppSettings Settings => _cachedSettings ??= LoadAppSettings();
    public UserState State => _cachedUserState ??= LoadUserState();

    public DatabaseConfigService(AppDbContext context)
    {
        _context = context;
        _context.Database.EnsureCreated();
        _saveTimer = new Timer(SaveIfDirty, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

        _markDirtyHandler = (_, _) => MarkDirty();
        _markDirtyCollectionHandler = (_, _) => MarkDirty();

        SubscribeToChanges();
    }

    private AppSettings LoadAppSettings()
    {
        var entity = _context
            .AppSettings.Include(a => a.SoundCommands)
            .ThenInclude(sc => sc.SoundFiles)
            .FirstOrDefault();

        if (entity == null)
        {
            var defaultSettings = new AppSettings();
            return defaultSettings;
        }

        var settings = new AppSettings
        {
            EnableHotkey =
                JsonConvert.DeserializeObject<Hotkey>(entity.EnableHotkeyData)
                ?? new Hotkey([KeyCode.VcF20]),
            SoundCommands = new ObservableCollection<SoundCommand>(
                entity.SoundCommands.Select(sc => new SoundCommand
                {
                    Name = sc.Name,
                    Category = sc.Category,
                    Enabled = sc.Enabled,
                    IsExpanded = sc.IsExpanded,
                    PlayChance = sc.PlayChance,
                    SelectedMatchType = (MatchType)sc.SelectedMatchType,
                    Volume = sc.Volume,
                    TimesPlayed = sc.TimesPlayed,
                    CooldownSeconds = sc.CooldownSeconds,
                    SoundFiles = new ObservableCollection<SoundFile>(
                        sc.SoundFiles.Select(sf => new SoundFile
                        {
                            FileName = sf.FileName,
                            Percentage = sf.Percentage,
                        })
                    ),
                })
            ),
        };

        settings.RefreshSubscriptions();
        return settings;
    }

    private UserState LoadUserState()
    {
        var entity = _context.UserStates.FirstOrDefault();

        if (entity == null)
        {
            return new UserState();
        }

        return new UserState
        {
            Username = entity.Username,
            Height = entity.Height,
            Width = entity.Width,
            PosX = entity.PosX,
            PosY = entity.PosY,
        };
    }

    public void ReloadSettingsAfterMigration()
    {
        Log.Information("Reloading settings from database after migration.");

        UnsubscribeFromChanges();

        _cachedSettings = LoadAppSettings();
        _cachedUserState = LoadUserState();

        SubscribeToChanges();
    }

    private void SubscribeToChanges()
    {
        UnsubscribeFromChanges();

        Settings.PropertyChanged += _markDirtyHandler;
        Settings.CollectionChanged += _markDirtyCollectionHandler;
        Settings.SoundCommandPropertyChanged += _markDirtyHandler;
        Settings.SoundCommands.CollectionChanged += _markDirtyCollectionHandler;

        State.PropertyChanged += _markDirtyHandler;
    }

    private void UnsubscribeFromChanges()
    {
        if (_cachedSettings != null)
        {
            _cachedSettings.PropertyChanged -= _markDirtyHandler;
            _cachedSettings.CollectionChanged -= _markDirtyCollectionHandler;
            _cachedSettings.SoundCommandPropertyChanged -= _markDirtyHandler;
            _cachedSettings.SoundCommands.CollectionChanged -= _markDirtyCollectionHandler;
        }

        if (_cachedUserState != null)
        {
            _cachedUserState.PropertyChanged -= _markDirtyHandler;
        }
    }

    private void MarkDirty()
    {
        _isDirty = true;
    }

    private async void SaveIfDirty(object? state)
    {
        try
        {
            if (!_isDirty)
                return;

            await SaveToDatabase();
            _isDirty = false;
            Log.Debug("Settings saved to database");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save settings to database");
        }
    }

    private async Task SaveToDatabase()
    {
        await SaveAppSettings();
        await SaveUserState();
        await _context.SaveChangesAsync();
    }

    private async Task SaveAppSettings()
    {
        var entity = await _context
            .AppSettings.Include(a => a.SoundCommands)
            .ThenInclude(sc => sc.SoundFiles)
            .FirstOrDefaultAsync();

        if (entity == null)
        {
            entity = new AppSettingsEntity { Id = 1 };
            _context.AppSettings.Add(entity);
        }

        entity.EnableHotkeyData = JsonConvert.SerializeObject(Settings.EnableHotkey);

        var soundCommandsToRemove = entity
            .SoundCommands.Where(ec =>
                !Settings.SoundCommands.Any(sc => sc.Name == ec.Name && sc.Category == ec.Category)
            )
            .ToList();
        _context.SoundCommands.RemoveRange(soundCommandsToRemove);

        foreach (var sc in Settings.SoundCommands)
        {
            var existingCommand = entity.SoundCommands.FirstOrDefault(ec =>
                ec.Name == sc.Name && ec.Category == sc.Category
            );

            if (existingCommand != null)
            {
                existingCommand.Enabled = sc.Enabled;
                existingCommand.IsExpanded = sc.IsExpanded;
                existingCommand.PlayChance = sc.PlayChance;
                existingCommand.SelectedMatchType = (int)sc.SelectedMatchType;
                existingCommand.Volume = sc.Volume;
                existingCommand.TimesPlayed = sc.TimesPlayed;
                existingCommand.CooldownSeconds = sc.CooldownSeconds;

                _context.SoundFiles.RemoveRange(existingCommand.SoundFiles);
                existingCommand.SoundFiles = sc
                    .SoundFiles.Select(sf => new SoundFileEntity
                    {
                        FileName = sf.FileName,
                        Percentage = sf.Percentage,
                    })
                    .ToList();
            }
            else
            {
                entity.SoundCommands.Add(
                    new SoundCommandEntity
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
                        SoundFiles = sc
                            .SoundFiles.Select(sf => new SoundFileEntity
                            {
                                FileName = sf.FileName,
                                Percentage = sf.Percentage,
                            })
                            .ToList(),
                    }
                );
            }
        }
    }

    private async Task SaveUserState()
    {
        var entity = await _context.UserStates.FirstOrDefaultAsync();

        if (entity == null)
        {
            entity = new UserStateEntity { Id = 1 };
            _context.UserStates.Add(entity);
        }

        entity.Username = State.Username;
        entity.Height = State.Height;
        entity.Width = State.Width;
        entity.PosX = State.PosX;
        entity.PosY = State.PosY;
    }

    public async Task SaveAndShutdown()
    {
        _saveTimer.Dispose();
        if (_isDirty)
        {
            await SaveToDatabase();
        }
    }
}
