#region

using CommunityToolkit.Mvvm.ComponentModel;

#endregion

namespace SimpleTwitchEmoteSounds.Models;

public partial class UserState : ObservableObject
{
    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private double _height = 1200;

    [ObservableProperty]
    private double _width = 900;

    [ObservableProperty]
    private int _posX;

    [ObservableProperty]
    private int _posY;
}
