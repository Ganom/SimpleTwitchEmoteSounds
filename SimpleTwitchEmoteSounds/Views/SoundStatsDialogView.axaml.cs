using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SimpleTwitchEmoteSounds.ViewModels;

namespace SimpleTwitchEmoteSounds.Views;

public partial class SoundStatsDialogView : Window
{
    public SoundStatsDialogView()
    {
        InitializeComponent();
        DataContext = new SoundStatsDialogViewModel();
        ((SoundStatsDialogViewModel)DataContext).CloseRequested += ViewModel_CloseRequested;
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ViewModel_CloseRequested(object? sender)
    {
        Close();
    }
}