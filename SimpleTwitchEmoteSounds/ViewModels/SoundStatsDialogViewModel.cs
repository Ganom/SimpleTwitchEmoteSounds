using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SimpleTwitchEmoteSounds.Models;
using SimpleTwitchEmoteSounds.Services;
using SkiaSharp;
using LiveChartsCore.SkiaSharpView.Painting.ImageFilters;

namespace SimpleTwitchEmoteSounds.ViewModels;

public partial class SoundStatsDialogViewModel : ObservableObject
{
    private const int TopSoundsCount = 10;

    private static readonly SolidColorPaint Paint = new(SKColors.White)
    {
        ImageFilter = new DropShadow(2, 2, 2, 2, SKColors.Black),
        SKTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
    };

    private static readonly SKColor[] ChartColors =
    [
        SKColor.Parse("#CC7777"),
        SKColor.Parse("#77CC77"),
        SKColor.Parse("#7777CC"),
        SKColor.Parse("#CCCC77"),
        SKColor.Parse("#CC77CC"),
        SKColor.Parse("#77CCCC"),
        SKColor.Parse("#CC8844"),
        SKColor.Parse("#99CC66"),
        SKColor.Parse("#CC7799"),
        SKColor.Parse("#7799CC")
    ];

    private readonly Dictionary<string, int> _soundColorIndices = new();
    private readonly bool[] _colorInUse = new bool[ChartColors.Length];

    public ObservableCollection<ISeries> PieSeries { get; } = [];

    public IEnumerable<SoundCommand> SortedSoundCommands =>
        ConfigService.Settings.SoundCommands
            .OrderByDescending(x => x.TimesPlayed);

    public SoundStatsDialogViewModel()
    {
        UpdatePieChart();
        ConfigService.Settings.SoundCommandPropertyChanged += (_, _) => UpdatePieChart();
    }

    [RelayCommand]
    private void RefreshStats(SoundCommand soundCommand)
    {
        soundCommand.RefreshStats();
        OnPropertyChanged(nameof(SortedSoundCommands));
    }

    [RelayCommand]
    private void KeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseRequested?.Invoke(null);
        }
    }

    private IEnumerable<SoundCommand> GetTopSounds() =>
        ConfigService.Settings.SoundCommands
            .OrderByDescending(x => x.TimesPlayed)
            .Take(TopSoundsCount)
            .Where(x => x.TimesPlayed > 0);

    private int GetNextAvailableColorIndex()
    {
        for (var i = 0; i < _colorInUse.Length; i++)
        {
            if (!_colorInUse[i])
                return i;
        }

        return 0;
    }

    private SKColor GetColorForSound(SoundCommand soundCommand)
    {
        if (!_soundColorIndices.TryGetValue(soundCommand.DisplayName, out var colorIndex))
        {
            colorIndex = GetNextAvailableColorIndex();
            _soundColorIndices[soundCommand.DisplayName] = colorIndex;
        }

        _colorInUse[colorIndex] = true;
        return ChartColors[colorIndex];
    }

    private PieSeries<double> CreatePieSeries(SoundCommand soundCommand)
    {
        var color = GetColorForSound(soundCommand);
        return new PieSeries<double>
        {
            Values = [soundCommand.TimesPlayed],
            Name = soundCommand.DisplayName,
            Fill = new SolidColorPaint(color),
            Stroke = new SolidColorPaint(SKColors.Black, 3),
            DataLabelsPosition = PolarLabelsPosition.Middle,
            DataLabelsSize = 16,
            DataLabelsPaint = Paint,
            ToolTipLabelFormatter = _ => $"{soundCommand.TimesPlayed:N0} plays",
            DataLabelsFormatter = _ => $"{soundCommand.DisplayName}"
        };
    }

    private void UpdatePieSeries(PieSeries<double> series, SoundCommand soundCommand)
    {
        series.Values = [soundCommand.TimesPlayed];
        series.Name = soundCommand.DisplayName;
        series.Fill = new SolidColorPaint(GetColorForSound(soundCommand));
        series.ToolTipLabelFormatter = _ => $"{soundCommand.TimesPlayed:N0} plays";
        series.DataLabelsFormatter = _ => $"{soundCommand.DisplayName}";
    }

    private void UpdatePieChart()
    {
        var topSounds = GetTopSounds().ToList();

        // Reset color usage tracking
        Array.Fill(_colorInUse, false);

        for (var i = 0; i < Math.Max(PieSeries.Count, topSounds.Count); i++)
        {
            if (i >= topSounds.Count)
            {
                PieSeries.RemoveAt(PieSeries.Count - 1);
                continue;
            }

            var soundCommand = topSounds[i];
            if (i < PieSeries.Count)
            {
                UpdatePieSeries((PieSeries<double>)PieSeries[i], soundCommand);
            }
            else
            {
                PieSeries.Add(CreatePieSeries(soundCommand));
            }
        }

        // Clean up color assignments for sounds no longer in top sounds
        var soundsToRemove = _soundColorIndices.Keys
            .Where(name => topSounds.All(s => s.DisplayName != name))
            .ToList();

        foreach (var soundName in soundsToRemove)
        {
            _soundColorIndices.Remove(soundName);
        }
    }

    public event Action<object?>? CloseRequested;
}