using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SimpleTwitchEmoteSounds.Models;

public partial class SoundCommand : ObservableObject
{
    private string _name = string.Empty;
    [ObservableProperty] private string _category = string.Empty;
    [ObservableProperty] private ObservableCollection<SoundFile> _soundFiles = [];
    [ObservableProperty] private bool _enabled = true;
    [ObservableProperty] private bool _isExpanded = true;
    [ObservableProperty] private string _playChance = "1";
    [ObservableProperty] private MatchType _selectedMatchType = MatchType.StartsWith;
    [ObservableProperty] private string _volume = "1";
    [JsonIgnore] public string DisplayName => Category == string.Empty ? $"{Name}" : $"({Category}) {Name}";
    [JsonIgnore] public ObservableCollection<MatchType> MatchTypes => new(Enum.GetValues<MatchType>());
    [JsonIgnore] public string[] Names => Name.Split(',').Select(n => n.Trim()).ToArray();

    public string Name
    {
        get => _name;
        set
        {
            if (!SetProperty(ref _name, value)) return;
            OnPropertyChanged(nameof(Names));
            OnPropertyChanged(nameof(DisplayName));
        }
    }
}

public enum MatchType
{
    Equals,
    StartsWith,
    ContainsWord
}