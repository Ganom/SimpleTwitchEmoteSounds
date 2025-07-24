﻿#region

using System;
using SimpleTwitchEmoteSounds.ViewModels;

#endregion

namespace SimpleTwitchEmoteSounds.Services;

public class PageNavigationService
{
    public Action<Type>? NavigationRequested { get; set; }

    public void RequestNavigation<T>()
        where T : ViewModelBase
    {
        NavigationRequested?.Invoke(typeof(T));
    }
}
