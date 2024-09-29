using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SimpleTwitchEmoteSounds.Models;

public partial class SoundFile : ObservableObject
{
    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _percentage = "1";
}