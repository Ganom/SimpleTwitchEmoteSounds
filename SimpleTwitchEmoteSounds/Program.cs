#region

using System;
using System.Threading.Tasks;
using Avalonia;
using Serilog;
using SimpleTwitchEmoteSounds.Services;
using SimpleTwitchEmoteSounds.Services.Core;
using Velopack;

#endregion

namespace SimpleTwitchEmoteSounds;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task Main(string[] args)
    {
        VelopackApp.Build().Run();

        if (args.Length > 0 && args[0] == "--force-update")
        {
            await HandleForceUpdateAsync();
            return;
        }

        var logConfig = new LoggerConfiguration();

#if DEBUG
        logConfig.MinimumLevel.Debug();
#else
        logConfig.MinimumLevel.Information();
#endif

        Log.Logger = logConfig
            .WriteTo.Console()
            .WriteTo.File(
                AppDataPathService.GetLogFilePath("chat-app-.txt"),
                rollingInterval: RollingInterval.Day
            )
            .CreateLogger();

        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            Log.Fatal(e.ExceptionObject as Exception, "AppDomain unhandled exception");
            Log.CloseAndFlush();
        };

        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            Log.Fatal(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Startup exception");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static async Task HandleForceUpdateAsync()
    {
        var logConfig = new LoggerConfiguration();

#if DEBUG
        logConfig.MinimumLevel.Debug();
#else
        logConfig.MinimumLevel.Information();
#endif

        Log.Logger = logConfig
            .WriteTo.Console()
            .WriteTo.File(
                AppDataPathService.GetLogFilePath("chat-app-update-.txt"),
                rollingInterval: RollingInterval.Day
            )
            .CreateLogger();

        try
        {
            Log.Information("Force update requested via command line");

            var updateService = new UpdateService();

            Log.Information("Checking for updates...");
            await updateService.CheckForUpdatesAsync();

            if (updateService is { IsUpdateAvailable: true, CurrentUpdateInfo: not null })
            {
                Log.Information("Update available, downloading...");
                var downloadSuccess = await updateService.DownloadUpdateAsync(
                    updateService.CurrentUpdateInfo
                );

                if (downloadSuccess)
                {
                    Log.Information("Update downloaded successfully, applying and restarting...");
                    await updateService.ApplyUpdateAndRestart(updateService.CurrentUpdateInfo);
                }
                else
                {
                    Log.Error("Failed to download update");
                    Environment.Exit(1);
                }
            }
            else
            {
                Log.Information("No updates available");
                Environment.Exit(0);
            }

            await updateService.DisposeAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Force update failed");
            Environment.Exit(1);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var app = AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
        return app;
    }
}
