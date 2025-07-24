#region

using CommunityToolkit.Mvvm.ComponentModel;

#endregion

namespace SimpleTwitchEmoteSounds.Models;

public partial class SoundFile : ObservableObject
{
    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _percentage = "1";
}
