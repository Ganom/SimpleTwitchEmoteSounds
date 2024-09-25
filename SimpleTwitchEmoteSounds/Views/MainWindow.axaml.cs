using System;
using Avalonia;
using Avalonia.Controls;
using SimpleTwitchEmoteSounds.Models;
using SimpleTwitchEmoteSounds.Services;

namespace SimpleTwitchEmoteSounds.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Width = Settings.Width;
        Height = Settings.Height;

        if (Settings.PosX != -1 && Settings.PosY != -1) Position = new PixelPoint(Settings.PosX, Settings.PosY);

        SizeChanged += OnSizeChanged;
        PositionChanged += OnPositionChanged;
    }

    private static UserState Settings => ConfigService.State;

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