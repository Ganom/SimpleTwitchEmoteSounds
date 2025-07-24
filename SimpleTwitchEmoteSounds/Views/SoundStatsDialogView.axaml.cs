#region

using Avalonia.Markup.Xaml;
using SimpleTwitchEmoteSounds.Services.Database;
using SimpleTwitchEmoteSounds.ViewModels;
using SukiUI.Controls;

#endregion

namespace SimpleTwitchEmoteSounds.Views;

public partial class SoundStatsDialogView : SukiWindow
{
    public SoundStatsDialogView()
    {
        InitializeComponent();
    }

    public SoundStatsDialogView(DatabaseConfigService configService)
        : this()
    {
        DataContext = new SoundStatsDialogViewModel(configService);
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
