using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleTwitchEmoteSounds.Models;
using SimpleTwitchEmoteSounds.Services;

namespace SimpleTwitchEmoteSounds.ViewModels;

public partial class EditSoundCommandDialogViewModel(SoundCommand soundCommand) : ObservableObject
{
    [ObservableProperty] private SoundCommand _soundCommand = soundCommand;

    [RelayCommand]
    private void Ok()
    {
        CloseRequested.Invoke(this, SoundCommand);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested.Invoke(this, null);
    }

    [RelayCommand]
    private void KeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Ok();
        }
    }

    [RelayCommand]
    private async Task AddSoundFile()
    {
        var topLevel = TopLevel.GetTopLevel(
            ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!)
            .MainWindow);

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
            foreach (var f in files)
            {
                SoundCommand.SoundFiles.Add(new SoundFile
                {
                    FileName = f.Name,
                    FilePath = f.Path.LocalPath,
                    Percentage = "1"
                });
            }
        }
    }

    [RelayCommand]
    private void RemoveSoundFile(SoundFile soundFile)
    {
        SoundCommand.SoundFiles.Remove(soundFile);
    }

    [RelayCommand]
    private Task PreviewSound(SoundCommand soundCommand)
    {
        _ = AudioService.PlaySound(soundCommand);
        return Task.CompletedTask;
    }

    public event EventHandler<SoundCommand?> CloseRequested = null!;
}