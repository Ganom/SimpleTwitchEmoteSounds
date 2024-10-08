using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SharpHook.Native;

namespace SimpleTwitchEmoteSounds.Models;

public partial class AppSettings : ObservableObject
{
    [ObservableProperty] private ObservableCollection<SoundCommand> _soundCommands = [];
    [ObservableProperty] private KeyCode _enableKey = KeyCode.VcF20;

    public void RefreshSubscriptions()
    {
        SoundCommands.CollectionChanged -= OnCollectionChanged;
        foreach (var se in SoundCommands)
        {
            se.PropertyChanged -= OnPropertyChanged;
            se.SoundFiles.CollectionChanged -= OnCollectionChanged;
            foreach (var file in se.SoundFiles)
            {
                file.PropertyChanged -= OnPropertyChanged;
            }
        }

        SoundCommands.CollectionChanged += OnCollectionChanged;
        foreach (var se in SoundCommands)
        {
            se.PropertyChanged += OnPropertyChanged;
            se.SoundFiles.CollectionChanged += OnCollectionChanged;
            foreach (var file in se.SoundFiles)
            {
                file.PropertyChanged += OnPropertyChanged;
            }
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Console.WriteLine($"Property changed: {e.PropertyName}");
        SoundCommandPropertyChanged?.Invoke(sender, e);
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? SoundCommandPropertyChanged;

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CollectionChanged?.Invoke(sender, e);
    }
}