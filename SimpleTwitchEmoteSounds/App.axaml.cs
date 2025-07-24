using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SimpleTwitchEmoteSounds.Common;
using SimpleTwitchEmoteSounds.Data;
using SimpleTwitchEmoteSounds.Services;
using SimpleTwitchEmoteSounds.Services.Core;
using SimpleTwitchEmoteSounds.Services.Database;
using SimpleTwitchEmoteSounds.Services.Migration;
using SimpleTwitchEmoteSounds.ViewModels;
using SimpleTwitchEmoteSounds.Views;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace SimpleTwitchEmoteSounds;

public class App : Application
{
    private DatabaseConfigService? _configService;

    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void RunMigration(JsonToDbMigrationService migrationService)
    {
        try
        {
            if (!migrationService.ShouldMigrate())
                return;
            migrationService.Migrate();
            _configService?.ReloadSettingsAfterMigration();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Migration failed, continuing with defaults");
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // If you use CommunityToolkit, line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection();

            services.AddSingleton(desktop);

            var views = ConfigureViews(services);
            Services = ConfigureServices(services);

            DataTemplates.Add(new ViewLocator(views));

            _configService = Services.GetRequiredService<DatabaseConfigService>();
            var migrationService = Services.GetRequiredService<JsonToDbMigrationService>();
            RunMigration(migrationService);

            desktop.MainWindow = views.CreateView<AppViewModel>(Services) as Window;
            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static AppViews ConfigureViews(ServiceCollection services)
    {
        return new AppViews()
            //main view
            .AddView<AppView, AppViewModel>(services)
            //other views
            .AddView<DashboardView, DashboardViewModel>(services)
            .AddView<EditSoundCommandDialog, EditSoundCommandDialogViewModel>(services)
            .AddView<NewSoundCommandDialog, NewSoundCommandDialogViewModel>(services)
            .AddView<SoundStatsDialogView, SoundStatsDialogViewModel>(services);
    }

    private static ServiceProvider ConfigureServices(ServiceCollection services)
    {
        var dbPath = AppDataPathService.GetSettingsFilePath("app.db");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        services.AddSingleton<ISukiToastManager, SukiToastManager>();
        services.AddSingleton<ISukiDialogManager, SukiDialogManager>();
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddSingleton<DatabaseConfigService>();
        services.AddSingleton<JsonToDbMigrationService>();
        services.AddSingleton<PageNavigationService>();
        services.AddSingleton<TwitchService>();
        services.AddSingleton<IHotkeyService, HotkeyService>();
        services.AddSingleton<IAudioPlaybackService, AudioPlaybackService>();

        return services.BuildServiceProvider();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _configService?.SaveAndShutdown().Wait();
        Services?.GetRequiredService<IHotkeyService>().Dispose();
        Services?.GetRequiredService<IAudioPlaybackService>().Dispose();
    }
}
