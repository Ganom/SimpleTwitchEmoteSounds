#region

using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SimpleTwitchEmoteSounds.Models;
using SimpleTwitchEmoteSounds.Services;

#endregion

namespace SimpleTwitchEmoteSounds.ViewModels;

public partial class EditSoundCommandDialogViewModel(
    SoundCommand soundCommand,
    IAudioPlaybackService audioPlaybackService
) : ObservableObject
{
    [ObservableProperty]
    private SoundCommand _soundCommand = soundCommand;

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
            (
                (IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!
            ).MainWindow
        );

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
            foreach (var f in files)
            {
                try
                {
                    var managedFileName = await audioPlaybackService.CopyToManagedAudio(
                        f.Path.LocalPath
                    );
                    SoundCommand.SoundFiles.Add(
                        new SoundFile { FileName = managedFileName, Percentage = "1" }
                    );
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to copy audio file: {FName}", f.Name);
                }
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
        _ = audioPlaybackService.PlaySound(soundCommand);
        return Task.CompletedTask;
    }

    public event EventHandler<SoundCommand?> CloseRequested = null!;
}
