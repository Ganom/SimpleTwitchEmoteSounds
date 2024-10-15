using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SimpleTwitchEmoteSounds.ViewModels;

namespace SimpleTwitchEmoteSounds.Views;

public partial class NewSoundCommandDialog : Window
{
    public NewSoundCommandDialog()
    {
        InitializeComponent();
        DataContext = new NewSoundCommandDialogViewModel();
        ((NewSoundCommandDialogViewModel)DataContext).CloseRequested += ViewModel_CloseRequested;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ViewModel_CloseRequested(object? sender, NewSoundCommandResult? result)
    {
        Close(result);
    }
}