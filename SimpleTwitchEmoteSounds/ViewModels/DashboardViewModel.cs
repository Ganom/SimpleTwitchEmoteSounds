﻿#region

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniTwitch.Irc.Models;
using Serilog;
using SimpleTwitchEmoteSounds.Extensions;
using SimpleTwitchEmoteSounds.Models;
using SimpleTwitchEmoteSounds.Services;
using SimpleTwitchEmoteSounds.Services.Database;
using SimpleTwitchEmoteSounds.Views;
using SukiUI.Dialogs;

#endregion

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedParameterInPartialMethod

namespace SimpleTwitchEmoteSounds.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _username;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectButtonText = "Connect";

    [ObservableProperty]
    private string _connectButtonColor = "white";

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _toggleButtonText = "Register Hotkey";

    [ObservableProperty]
    private string _updateButtonText = "v1.3.2";

    [ObservableProperty]
    private bool _isListening;
    private Hotkey ToggleHotkey => _configService.Settings.EnableHotkey;
    private ObservableCollection<SoundCommand> SoundCommands =>
        _configService.Settings.SoundCommands;
    public FilteredObservableCollection<SoundCommand> FilteredSoundCommands { get; }

    private readonly TwitchService _twitchService;
    private readonly IHotkeyService _hotkeyService;
    private readonly DatabaseConfigService _configService;
    private readonly IAudioPlaybackService _audioPlaybackService;
    private readonly ISukiDialogManager _dialogManager;

    public DashboardViewModel(
        TwitchService twitchService,
        IHotkeyService hotkeyService,
        DatabaseConfigService configService,
        IAudioPlaybackService audioPlaybackService,
        ISukiDialogManager dialogManager
    )
    {
        _twitchService = twitchService;
        _hotkeyService = hotkeyService;
        _configService = configService;
        _audioPlaybackService = audioPlaybackService;
        _dialogManager = dialogManager;

        Username = _configService.State.Username;
        _twitchService.ConnectionStatus += TwitchServiceConnectionStatus;
        _twitchService.MessageLogged += TwitchServiceMessageLogged;
        _hotkeyService.RegisterHotkey(ToggleHotkey, ToggleEnabled);
        ToggleButtonText = ToggleHotkey.ToString();

        _configService.Settings.RefreshSubscriptions();
        FilteredSoundCommands = new FilteredObservableCollection<SoundCommand>(
            _configService.Settings.SoundCommands,
            v =>
                string.IsNullOrEmpty(SearchText)
                || v.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
        );

        if (!string.IsNullOrEmpty(Username))
        {
            _ = _twitchService.ConnectAsync(Username);
        }
    }

    [RelayCommand]
    private async Task Connect()
    {
        if (IsConnected)
        {
            await _twitchService.DisconnectAsync();
        }
        else
        {
            await _twitchService.ConnectAsync(Username);
        }
    }

    [RelayCommand]
    private void ToggleEnabled()
    {
        IsEnabled = !IsEnabled;
    }

    [RelayCommand]
    private async Task OpenStandardDialog()
    {
        var mainWindow = GetMainWindow();
        var dialog = new NewSoundCommandDialog
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var result = await dialog.ShowDialog<NewSoundCommandResult?>(mainWindow);

        if (result is null)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(mainWindow);

        var files = await topLevel?.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Select Audio Files",
                AllowMultiple = true,
                FileTypeFilter =
                [
                    new FilePickerFileType("Audio Files")
                    {
                        Patterns = ["*.mp3", "*.wav", "*.ogg"],
                    },
                ],
            }
        )!;

        if (files is { Count: >= 1 })
        {
            var sc = new SoundCommand
            {
                Name = result.Name,
                Category = result.Category,
                SoundFiles = [],
            };

            foreach (var f in files)
            {
                var managedFileName = await _audioPlaybackService.CopyToManagedAudio(
                    f.Path.LocalPath
                );
                sc.SoundFiles.Add(new SoundFile
                {
                    FileName = managedFileName,
                    Percentage = "1"
                });
            }

            SoundCommands.Add(sc);
            FilteredSoundCommands.Refresh();
            _configService.Settings.RefreshSubscriptions();
        }
    }

    [RelayCommand]
    private Task PreviewSound(SoundCommand soundCommand)
    {
        _ = _audioPlaybackService.PlaySound(soundCommand);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task EditSound(SoundCommand soundCommand)
    {
        var dialog = new EditSoundCommandDialog(soundCommand, _audioPlaybackService)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var result = await dialog.ShowDialog<SoundCommand?>(GetMainWindow());

        if (result != null)
        {
            FilteredSoundCommands.Refresh();
            _configService.Settings.RefreshSubscriptions();
        }
    }

    [RelayCommand]
    private async Task RemoveSound(SoundCommand soundCommand)
    {
        var task = _dialogManager.CreateDialog()
            .OfType(NotificationType.Warning)
            .WithTitle("Remove Sound")
            .WithContent($"Are you sure you want to remove the sound '{soundCommand.Name}'?")
            .WithYesNoResult("Yes", "No")
            .Dismiss().ByClickingBackground()
            .TryShowAsync();

        var result = await task;

        if (result)
        {
            SoundCommands.Remove(soundCommand);
            FilteredSoundCommands.Refresh();
            _configService.Settings.RefreshSubscriptions();
        }
    }

    [RelayCommand]
    private void ToggleListening()
    {
        IsListening = !IsListening;
        if (IsListening)
        {
            ToggleButtonText = "Cancel";
            _hotkeyService.StartListeningForNextKey(RegisterHotkey);
        }
        else
        {
            ResetState();
        }
    }

    [RelayCommand]
    private void RegisterHotkey(Hotkey combo)
    {
        _hotkeyService.UnregisterHotkey(ToggleHotkey);
        _configService.Settings.EnableHotkey = combo;
        _hotkeyService.RegisterHotkey(ToggleHotkey, ToggleEnabled);
        ResetState();
    }

    [RelayCommand]
    private void ToggleSound(SoundCommand soundCommand)
    {
        var focusedElement = GetMainWindow().FocusManager?.GetFocusedElement();

        if (focusedElement is Button)
        {
            return;
        }

        soundCommand.Enabled = !soundCommand.Enabled;
    }

    [RelayCommand]
    private void UpdateButton()
    {
        const string url = "https://github.com/Ganom/SimpleTwitchEmoteSounds/releases";
        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }

    partial void OnUsernameChanged(string value)
    {
        _configService.State.Username = value;
    }

    partial void OnSearchTextChanged(string value)
    {
        FilteredSoundCommands.Refresh();
    }

    partial void OnIsEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(EnabledButtonColor));
    }

    public string EnabledButtonColor => IsEnabled ? "#5dc264" : "#fc725a";

    private async void TwitchServiceMessageLogged(Privmsg msg)
    {
        try
        {
            if (!IsEnabled)
            {
                return;
            }

            foreach (var soundCommand in SoundCommands)
            {
                if (!soundCommand.Enabled)
                {
                    Log.Debug($"Sound command '{soundCommand.Name}' is disabled. Skipping.");
                    continue;
                }

                if (soundCommand.IsOnCooldown)
                {
                    Log.Debug($"Sound command '{soundCommand.Name}' is on cooldown. Skipping.");
                    continue;
                }

                var isMatch = soundCommand.Names.Any(name =>
                {
                    return soundCommand.SelectedMatchType switch
                    {
                        MatchType.Equals => msg.Content.Trim().Equals(name),
                        MatchType.StartsWith => msg.Content.Trim().StartsWith(name),
                        MatchType.StartsWithWord => msg
                            .Content.Trim()
                            .Split(' ')[0]
                            .Equals(name.Trim()),
                        MatchType.ContainsWord => Regex.IsMatch(
                            msg.Content,
                            $@"\b{Regex.Escape(name)}\b"
                        ),
                        _ => throw new ArgumentOutOfRangeException(),
                    };
                });

                if (!isMatch)
                {
                    continue;
                }

                var shouldPlay = ShouldPlaySound(float.Parse(soundCommand.PlayChance));
                Log.Debug(
                    $"Command '{soundCommand.Name}' matched. Play chance: {soundCommand.PlayChance}%. Should play: {shouldPlay}"
                );

                if (!shouldPlay)
                {
                    Log.Debug(
                        $"Command '{soundCommand.Name}' matched but didn't pass the play chance check. Continuing to next command."
                    );
                    continue;
                }

                Log.Debug($"Playing sound for command: {soundCommand.Name}");
                soundCommand.UpdateLastPlayedTime();
                await _audioPlaybackService.PlaySound(soundCommand);
                break;
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception when attempting to play sound command, ");
        }
    }

    private static bool ShouldPlaySound(float playChance)
    {
        var randomValue = (float)Random.Shared.NextDouble();
        var shouldPlay = randomValue <= playChance;
        Log.Debug(
            $"Play chance check: Random value: {randomValue:F4}, Play chance: {playChance:F4}, Should play: {shouldPlay}"
        );
        return shouldPlay;
    }

    private void TwitchServiceConnectionStatus(TwitchStatus obj)
    {
        Log.Information($"Status: {obj.ToString()}");
        switch (obj)
        {
            case TwitchStatus.Disconnected:
                IsConnected = false;
                ConnectButtonText = "Disconnected";
                ConnectButtonColor = "#fc725a";
                break;
            case TwitchStatus.Connected:
                IsConnected = true;
                ConnectButtonText = "Connected";
                ConnectButtonColor = "#5dc264";
                break;
            case TwitchStatus.Reconnecting:
                IsConnected = false;
                ConnectButtonText = "Reconnecting";
                ConnectButtonColor = "#ffae43";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
        }
    }

    private void ResetState()
    {
        IsListening = false;
        ToggleButtonText = ToggleHotkey.ToString();
        _hotkeyService.StopListeningForNextKey();
    }

    public void RefreshAfterMigration()
    {
        Log.Information("Refreshing DashboardViewModel after migration");
        Username = _configService.State.Username;
        FilteredSoundCommands.UpdateSource(_configService.Settings.SoundCommands);
        FilteredSoundCommands.Refresh();
        ToggleButtonText = ToggleHotkey.ToString();
    }

    private static Window GetMainWindow()
    {
        return (
            (IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!
        ).MainWindow!;
    }
}
