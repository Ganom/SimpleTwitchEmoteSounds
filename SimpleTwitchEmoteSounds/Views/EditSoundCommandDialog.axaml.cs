using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SimpleTwitchEmoteSounds.ViewModels;
using SimpleTwitchEmoteSounds.Models;
using SimpleTwitchEmoteSounds.Services;

namespace SimpleTwitchEmoteSounds.Views;

public partial class EditSoundCommandDialog : Window
{
    // ReSharper disable once UnusedMember.Global
    public EditSoundCommandDialog()
    {
        InitializeComponent();
    }

    public EditSoundCommandDialog(SoundCommand soundCommand, IAudioPlaybackService audioPlaybackService)
    {
        InitializeComponent();
        DataContext = new EditSoundCommandDialogViewModel(soundCommand, audioPlaybackService);
        ((EditSoundCommandDialogViewModel)DataContext).CloseRequested += ViewModel_CloseRequested;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ViewModel_CloseRequested(object? sender, SoundCommand? result)
    {
        Close(result);
    }
}