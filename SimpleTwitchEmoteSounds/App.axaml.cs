using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SimpleTwitchEmoteSounds.Services;
using SimpleTwitchEmoteSounds.ViewModels;

namespace SimpleTwitchEmoteSounds;

public class App : Application
{
    private IServiceProvider? _provider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _provider = ConfigureServices();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // If you use CommunityToolkit, line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewLocator = _provider?.GetRequiredService<IDataTemplate>();
            var appViewModel = _provider?.GetRequiredService<AppViewModel>();

            desktop.MainWindow = viewLocator?.Build(appViewModel) as Window;
            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider ConfigureServices()
    {
        var viewLocator = Current?.DataTemplates.First(x => x is ViewLocator);
        var services = new ServiceCollection();

        // Services
        if (viewLocator is not null)
            services.AddSingleton(viewLocator);
        services.AddSingleton<PageNavigationService>();
        services.AddSingleton<TwitchService>();
        services.AddSingleton<IHotkeyService, HotkeyService>();

        // ViewModels
        services.AddSingleton<AppViewModel>();
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => !p.IsAbstract && typeof(ViewModelBase).IsAssignableFrom(p));
        foreach (var type in types)
            services.AddSingleton(typeof(ViewModelBase), type);

        return services.BuildServiceProvider();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _provider?.GetRequiredService<IHotkeyService>().Dispose();
    }
}