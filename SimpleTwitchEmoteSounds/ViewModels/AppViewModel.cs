using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using SimpleTwitchEmoteSounds.Services;

namespace SimpleTwitchEmoteSounds.ViewModels;

public partial class AppViewModel : ObservableObject
{
    [ObservableProperty] private ViewModelBase? _activePage;

    public AppViewModel(IEnumerable<ViewModelBase> appPages, PageNavigationService pageNavigationService)
    {
        AppPages = new AvaloniaList<ViewModelBase>(appPages);
        ActivePage = AppPages[0];
        pageNavigationService.NavigationRequested += pageType =>
        {
            var page = AppPages.FirstOrDefault(x => x.GetType() == pageType);
            if (page is null || ActivePage?.GetType() == pageType) return;
            ActivePage = page;
        };
    }

    public IAvaloniaReadOnlyList<ViewModelBase> AppPages { get; }
}