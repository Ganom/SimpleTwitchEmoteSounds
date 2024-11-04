using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using SimpleTwitchEmoteSounds.Extensions;
using SimpleTwitchEmoteSounds.Models;
using SimpleTwitchEmoteSounds.Services;
using SimpleTwitchEmoteSounds.Views;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedParameterInPartialMethod

namespace SimpleTwitchEmoteSounds.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private static readonly string postfixInstalledIndicator =
        #if CUSTOM_FEATURE_INSTALLED
        "-installed";
        #else
        "-portable";
        #endif

    [ObservableProperty] private string _username = ConfigService.State.Username;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _connectButtonText = "Connect";
    [ObservableProperty] private string _connectButtonColor = "white";
    [ObservableProperty] private bool _isEnabled = true;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _toggleButtonText = "Register Hotkey";
    [ObservableProperty] private string _updateButtonText = "v1.2.1" + postfixInstalledIndicator;
    [ObservableProperty] private bool _isListening;
    private static Hotkey ToggleHotkey => ConfigService.Settings.EnableHotkey;
    private static ObservableCollection<SoundCommand> SoundCommands => ConfigService.Settings.SoundCommands;
    public FilteredObservableCollection<SoundCommand> FilteredSoundCommands { get; }

    private readonly TwitchService _twitchService;
    private readonly IHotkeyService _hotkeyService;

    public DashboardViewModel(TwitchService twitchService, IHotkeyService hotkeyService)
    {
        _twitchService = twitchService;
        _hotkeyService = hotkeyService;
        _twitchService.ConnectionStatus += TwitchServiceConnectionStatus;
        _twitchService.MessageLogged += TwitchServiceMessageLogged;
        _hotkeyService.RegisterHotkey(ToggleHotkey, ToggleEnabled);
        ToggleButtonText = ToggleHotkey.ToString();

        ConfigService.Settings.RefreshSubscriptions();
        FilteredSoundCommands = new FilteredObservableCollection<SoundCommand>(
            ConfigService.Settings.SoundCommands,
            v => string.IsNullOrEmpty(SearchText) ||
                 v.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
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
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var result = await dialog.ShowDialog<NewSoundCommandResult?>(mainWindow);

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
                    Percentage = "1"
                });
            }

            SoundCommands.Add(sc);
            FilteredSoundCommands.Refresh();
            ConfigService.Settings.RefreshSubscriptions();
        }
    }

    [RelayCommand]
    private Task PreviewSound(SoundCommand soundCommand)
    {
        _ = AudioService.PlaySound(soundCommand);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task EditSound(SoundCommand soundCommand)
    {
        var dialog = new EditSoundCommandDialog(soundCommand)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var result = await dialog.ShowDialog<SoundCommand?>(GetMainWindow());

        if (result != null)
        {
            FilteredSoundCommands.Refresh();
            ConfigService.Settings.RefreshSubscriptions();
        }
    }

    [RelayCommand]
    private async Task RemoveSound(SoundCommand soundCommand)
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
        ConfigService.Settings.EnableHotkey = combo;
        _hotkeyService.RegisterHotkey(ToggleHotkey, ToggleEnabled);
        ResetState();
    }

    [RelayCommand]
    private void ToggleSound(SoundCommand soundCommand)
    {
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
        ConfigService.State.Username = value;
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
                    MatchType.StartsWithWord => msg.Content.Trim().Split(' ')[0].Equals(name.Trim()),
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

    private static async Task<ButtonResult> ShowConfirmationDialog(string title, string message)
    {
        var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandard(
            title,
            message,
            ButtonEnum.YesNo,
            Icon.Question,
            WindowStartupLocation.CenterOwner
        );

        return await messageBoxStandardWindow.ShowWindowDialogAsync(GetMainWindow());
    }

    private static Window GetMainWindow()
    {
        return ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!;
    }
}
