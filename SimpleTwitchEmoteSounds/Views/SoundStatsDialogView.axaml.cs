using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SimpleTwitchEmoteSounds.Services.Database;
using SimpleTwitchEmoteSounds.ViewModels;
using SukiUI.Controls;

namespace SimpleTwitchEmoteSounds.Views;

public partial class SoundStatsDialogView : SukiWindow
{
    public SoundStatsDialogView()
    {
        InitializeComponent();
        var configService = ((App)Application.Current!).Services!.GetRequiredService<DatabaseConfigService>();
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
