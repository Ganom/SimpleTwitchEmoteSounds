using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SimpleTwitchEmoteSounds.ViewModels;
using SimpleTwitchEmoteSounds.Models;

namespace SimpleTwitchEmoteSounds.Views;

public partial class EditSoundCommandDialog : Window
{
    public EditSoundCommandDialog(SoundCommand soundCommand)
    {
        InitializeComponent();
        DataContext = new EditSoundCommandDialogViewModel(soundCommand);
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