using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace SimpleTwitchEmoteSounds.Views;

public partial class NewSoundCommandDialog : Window
{
    public NewSoundCommandDialog()
    {
        InitializeComponent();
        KeyDown += NewSoundCommandDialog_KeyDown;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void NewSoundCommandDialog_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OkButton_Click(null, null);
        }
    }

    private void OkButton_Click(object? sender, RoutedEventArgs? e)
    {
        var name = this.FindControl<TextBox>("NameTextBox")?.Text ?? string.Empty;
        var category = this.FindControl<TextBox>("CategoryTextBox")?.Text ?? string.Empty;

        Close(new NewSoundCommandResult(name, category));
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}

public record NewSoundCommandResult(string Name, string Category);