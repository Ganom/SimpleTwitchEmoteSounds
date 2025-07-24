using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Serilog;
using SimpleTwitchEmoteSounds.Services;

namespace SimpleTwitchEmoteSounds.Models;

public partial class SoundCommand : ObservableObject
{
    private string _name = string.Empty;
    [ObservableProperty]
    private string _category = string.Empty;
    [ObservableProperty]
    private ObservableCollection<SoundFile> _soundFiles = [];
    [ObservableProperty]
    private bool _enabled = true;
    [ObservableProperty]
    private bool _isExpanded = true;
    [ObservableProperty]
    private string _playChance = "1";
    [ObservableProperty]
    private MatchType _selectedMatchType = MatchType.StartsWith;
    [ObservableProperty]
    private string _volume = "1";
    [ObservableProperty]
    private int _timesPlayed;
    [ObservableProperty]
    private string _cooldownSeconds = "0";
    [JsonIgnore]
    [ObservableProperty]
    private DateTime _lastPlayedTime = DateTime.MinValue;
    [JsonIgnore]
    public string DisplayName => Category == string.Empty ? $"{Name}" : $"({Category}) {Name}";
    [JsonIgnore]
    public ObservableCollection<MatchType> MatchTypes => new(Enum.GetValues<MatchType>());
    [JsonIgnore]
    public string[] Names => Name.Split(',').Select(n => n.Trim()).ToArray();

    [JsonIgnore]
    public bool IsMissingSoundFiles => SoundFiles.Any(soundFile => !File.Exists(Path.Combine(AppDataPathService.GetAudioFilesPath(), soundFile.FileName)));

    [JsonIgnore]
    public bool IsOnCooldown
    {
        get
        {
            if (!double.TryParse(CooldownSeconds, out var cooldown) || cooldown <= 0)
                return false;
            return (DateTime.Now - LastPlayedTime).TotalSeconds < cooldown;
        }
    }

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

    public void IncrementTimesPlayed()
    {
        TimesPlayed++;
    }

    public void RefreshStats()
    {
        TimesPlayed = 0;
    }

    public void UpdateLastPlayedTime()
    {
        LastPlayedTime = DateTime.Now;
    }
}

public enum MatchType
{
    Equals,
    StartsWith,
    StartsWithWord,
    ContainsWord
}
