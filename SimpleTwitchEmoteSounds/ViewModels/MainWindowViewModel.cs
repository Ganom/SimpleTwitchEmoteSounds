using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniTwitch.Irc.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Serilog;
using SimpleTwitchEmoteSounds.Common;
using SimpleTwitchEmoteSounds.Extensions;
using SimpleTwitchEmoteSounds.Models;
using SimpleTwitchEmoteSounds.Services;
using SimpleTwitchEmoteSounds.Views;

namespace SimpleTwitchEmoteSounds.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private string _username = ConfigService.State.Username;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _connectButtonText = "Connect";
    [ObservableProperty] private bool _isEnabled = true;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _connectButtonColor = "white";
    private static ObservableCollection<SoundCommand> SoundCommands => ConfigService.Settings.SoundCommands;
    public FilteredObservableCollection<SoundCommand> FilteredSoundCommands { get; }

    private readonly TwitchService _twitchService;

    public MainWindowViewModel(TwitchService twitchService)
    {
        _twitchService = twitchService;
        _twitchService.ConnectionStatus += TwitchServiceConnectionStatus;
        _twitchService.MessageLogged += TwitchServiceMessageLogged;

        if (!string.IsNullOrEmpty(Username))
        {
            _twitchService.ConnectAsync(Username);
        }

        ConfigService.Settings.RefreshSubscriptions();
        FilteredSoundCommands = new FilteredObservableCollection<SoundCommand>(
            ConfigService.Settings.SoundCommands,
            v => string.IsNullOrEmpty(SearchText) ||
                 v.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
        );
    }

    partial void OnUsernameChanged(string value)
    {
        ConfigService.State.Username = value;
    }

    private async void TwitchServiceMessageLogged(Privmsg msg)
    {
        if (!IsEnabled)
        {
            return;
        }

        foreach (var soundCommand in SoundCommands)
        {
            if (!soundCommand.Enabled)
            {
                Log.Information($"Sound command '{soundCommand.Name}' is disabled. Skipping.");
                continue;
            }

            var isMatch = soundCommand.Names.Any(name =>
            {
                return soundCommand.SelectedMatchType switch
                {
                    MatchType.Equals => msg.Content.Trim().Equals(name),
                    MatchType.StartsWith => msg.Content.Trim().StartsWith(name),
                    MatchType.ContainsWord => Regex.IsMatch(msg.Content, $@"\b{Regex.Escape(name)}\b"),
                    _ => throw new ArgumentOutOfRangeException()
                };
            });

            if (!isMatch)
            {
                continue;
            }

            var shouldPlay = ShouldPlaySound(float.Parse(soundCommand.PlayChance));
            Log.Information(
                $"Command '{soundCommand.Name}' matched. Play chance: {soundCommand.PlayChance}%. Should play: {shouldPlay}");

            if (!shouldPlay)
            {
                Log.Information(
                    $"Command '{soundCommand.Name}' matched but didn't pass the play chance check. Continuing to next command.");
                continue;
            }

            Log.Information($"Playing sound for command: {soundCommand.Name}");
            await AudioService.PlaySound(soundCommand);
            break;
        }
    }

    private static bool ShouldPlaySound(float playChance)
    {
        var randomValue = (float)Random.Shared.NextDouble();
        var shouldPlay = randomValue <= playChance;
        Log.Information(
            $"Play chance check: Random value: {randomValue:F4}, Play chance: {playChance:F4}, Should play: {shouldPlay}");
        return shouldPlay;
    }

    private void TwitchServiceConnectionStatus(TwitchStatus obj)
    {
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
                IsConnected = true;
                ConnectButtonText = "Reconnected";
                ConnectButtonColor = "#ffae43";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        Debouncer.Debounce("refresh", () => FilteredSoundCommands.Refresh(), 300);
    }

    [RelayCommand]
    private void Connect()
    {
        if (IsConnected)
        {
            _twitchService.DisconnectAsync();
        }
        else
        {
            _twitchService.ConnectAsync(Username);
        }
    }

    [RelayCommand]
    private void ToggleEnabled()
    {
        IsEnabled = !IsEnabled;
    }

    public string EnabledButtonColor => IsEnabled ? "#5dc264" : "#fc725a";

    partial void OnIsEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(EnabledButtonColor));
    }

    [RelayCommand]
    private async Task OpenStandardDialog()
    {
        var mainWindow = GetMainWindow();
        var dialog = new NewSoundCommandDialog
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var result = await dialog.ShowDialog<NewSoundCommandResult>(mainWindow);

        if (result is null)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(mainWindow);

        var files = await topLevel?.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Audio Files",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("Audio Files") { Patterns = ["*.mp3", "*.wav", "*.ogg"] }
            ]
        })!;

        if (files is { Count: >= 1 })
        {
            var sc = new SoundCommand
            {
                Name = result.Name,
                Category = result.Category,
                SoundFiles = []
            };

            foreach (var f in files)
            {
                sc.SoundFiles.Add(new SoundFile
                {
                    FileName = f.Name,
                    FilePath = f.Path.LocalPath,
                    Percentage = (100 / files.Count).ToString()
                });
            }

            SoundCommands.Add(sc);
            FilteredSoundCommands.Refresh();
            ConfigService.Settings.RefreshSubscriptions();
        }
    }

    [RelayCommand]
    private async Task AddSoundFile(SoundCommand soundCommand)
    {
        var topLevel = TopLevel.GetTopLevel(
            ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!)
            .MainWindow);

        var files = await topLevel?.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Audio File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Audio Files") { Patterns = ["*.mp3", "*.wav", "*.ogg"] }
            ]
        })!;

        if (files is { Count: 1 })
        {
            var f = files[0];
            soundCommand.SoundFiles.Add(new SoundFile
            {
                FileName = f.Name,
                FilePath = f.Path.LocalPath,
                Percentage = (100 / (soundCommand.SoundFiles.Count + 1)).ToString()
            });
        }
    }

    [RelayCommand]
    private async Task RemoveItem(SoundCommand soundCommand)
    {
        var result = await ShowConfirmationDialog(
            "Remove Sound",
            $"Are you sure you want to remove the sound '{soundCommand.Name}'?");

        if (result == ButtonResult.Yes)
        {
            SoundCommands.Remove(soundCommand);
            FilteredSoundCommands.Refresh();
            ConfigService.Settings.RefreshSubscriptions();
        }
    }


    [RelayCommand]
    private void ExpandAll()
    {
        foreach (var soundCommand in SoundCommands)
        {
            soundCommand.IsExpanded = true;
        }
    }

    [RelayCommand]
    private void CollapseAll()
    {
        foreach (var soundCommand in SoundCommands)
        {
            soundCommand.IsExpanded = false;
        }
    }

    [RelayCommand]
    private async Task PreviewSound(SoundCommand soundCommand)
    {
        await AudioService.PlaySound(soundCommand);
    }

    [RelayCommand]
    private void RemoveSoundFile(SoundFile soundFile)
    {
        foreach (var soundCommand in SoundCommands)
        {
            if (soundCommand.SoundFiles.Remove(soundFile))
            {
                break;
            }
        }
    }

    private static async Task<ButtonResult> ShowConfirmationDialog(string title, string message)
    {
        var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandard(
            title,
            message,
            ButtonEnum.YesNo,
            Icon.Question);

        return await messageBoxStandardWindow.ShowAsync();
    }

    private static Window GetMainWindow()
    {
        return ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!;
    }
}