using System;
using SimpleTwitchEmoteSounds.ViewModels;

namespace SimpleTwitchEmoteSounds.Services;

public class PageNavigationService
{
    public Action<Type>? NavigationRequested { get; set; }

    public void RequestNavigation<T>() where T : ViewModelBase
    {
        NavigationRequested?.Invoke(typeof(T));
    }
}