using CommunityToolkit.Mvvm.ComponentModel;

namespace SimpleTwitchEmoteSounds.Models;

public partial class SoundFile : ObservableObject
{
    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty] private string _filePath = string.Empty;
    private int _percentage = 100;

    public int Percentage
    {
        get => _percentage;
        set
        {
            if (SetProperty(ref _percentage, value))
            {
                OnPercentageChanged();
            }
        }
    }

    private void OnPercentageChanged()
    {
        if (Percentage < 0) Percentage = 0;
        if (Percentage > 100) Percentage = 100;
    }
}