using System;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SimpleTwitchEmoteSounds.Models;

public partial class SoundCommand : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _category = string.Empty;
    [ObservableProperty] private ObservableCollection<SoundFile> _soundFiles = [];
    [ObservableProperty] private bool _enabled = true;
    [ObservableProperty] private bool _isExpanded = true;
    [ObservableProperty] private int _playChance = 100;
    private float _volume = 0.5f;
    [JsonIgnore] public int VolumePercentage => (int)(Volume * 100);
    [JsonIgnore] public string DisplayName => $"({Category}) {Name}";

    public float Volume
    {
        get => _volume;
        set
        {
            if (SetProperty(ref _volume, Math.Clamp(value, 0.0f, 1.0f)))
            {
                OnPropertyChanged(nameof(VolumePercentage));
            }
        }
    }

    partial void OnPlayChanceChanged(int value)
    {
        PlayChance = value switch
        {
            < 0 => 0,
            > 100 => 100,
            _ => PlayChance
        };
    }
}