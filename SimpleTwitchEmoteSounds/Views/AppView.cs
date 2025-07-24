using System;
using Avalonia;
using Avalonia.Controls;
using SimpleTwitchEmoteSounds.Models;
using SimpleTwitchEmoteSounds.Services;
using SimpleTwitchEmoteSounds.Services.Database;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleTwitchEmoteSounds.Views;

public partial class AppView : Window
{
    private readonly DatabaseConfigService _configService;
    
    public AppView()
    {
        _configService = ((App)Application.Current!).Services!.GetRequiredService<DatabaseConfigService>();
        
        InitializeComponent();
        Width = Settings.Width;
        Height = Settings.Height;

        if (Settings.PosX != -1 && Settings.PosY != -1) Position = new PixelPoint(Settings.PosX, Settings.PosY);

        SizeChanged += OnSizeChanged;
        PositionChanged += OnPositionChanged;
    }

    private UserState Settings => _configService.State;

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        Settings.Width = Width;
        Settings.Height = Height;
    }

    private void OnPositionChanged(object? sender, EventArgs e)
    {
        Settings.PosX = Position.X;
        Settings.PosY = Position.Y;
    }
}