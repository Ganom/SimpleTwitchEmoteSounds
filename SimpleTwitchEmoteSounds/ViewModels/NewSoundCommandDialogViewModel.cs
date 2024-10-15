using System;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SimpleTwitchEmoteSounds.ViewModels;

public partial class NewSoundCommandDialogViewModel : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _category = string.Empty;

    [RelayCommand]
    private void Ok()
    {
        CloseRequested.Invoke(this, new NewSoundCommandResult(Name, Category));
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested.Invoke(this, null);
    }

    [RelayCommand]
    private void KeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Ok();
        }
    }

    public event EventHandler<NewSoundCommandResult?> CloseRequested = null!;
}

public record NewSoundCommandResult(string Name, string Category);