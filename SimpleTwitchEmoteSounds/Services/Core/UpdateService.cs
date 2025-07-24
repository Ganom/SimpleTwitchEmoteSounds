namespace SimpleTwitchEmoteSounds.Services.Core;

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using NuGet.Versioning;
using Serilog;
using Velopack;
using Velopack.Locators;
using Velopack.Sources;

public interface IUpdateService
{
    event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;
    event EventHandler<UpdateProgressEventArgs>? UpdateProgress;
    event EventHandler<UpdateErrorEventArgs>? UpdateError;

    Task CheckForUpdatesAsync();
    Task<bool> DownloadUpdateAsync(UpdateInfo updateInfo);
    Task ApplyUpdateAndRestart(UpdateInfo updateInfo);
    bool IsUpdateAvailable { get; }
    UpdateInfo? CurrentUpdateInfo { get; }
    SemanticVersion? CurrentVersion { get; }
    ValueTask DisposeAsync();
}

public class UpdateAvailableEventArgs(UpdateInfo updateInfo, string? combinedReleaseNotes = null)
    : EventArgs
{
    public UpdateInfo UpdateInfo { get; } = updateInfo;
    public string? CombinedReleaseNotes { get; } = combinedReleaseNotes;
}

public class UpdateProgressEventArgs(int progress) : EventArgs
{
    public int Progress { get; } = progress;
}

public class UpdateErrorEventArgs(Exception exception, string message) : EventArgs
{
    public Exception Exception { get; } = exception;
    public string Message { get; } = message;
}

public class UpdateService : IUpdateService, IAsyncDisposable
{
    private readonly ILogger _logger = Log.ForContext<UpdateService>();
    private CombinedChangelogUpdateManager? _updateManager;
    private UpdateInfo? _currentUpdateInfo;
    private string? _combinedReleaseNotes;
    private bool _disposed;

    public event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;
    public event EventHandler<UpdateProgressEventArgs>? UpdateProgress;
    public event EventHandler<UpdateErrorEventArgs>? UpdateError;

    public bool IsUpdateAvailable => _currentUpdateInfo != null;
    public UpdateInfo? CurrentUpdateInfo => _currentUpdateInfo;
    public SemanticVersion? CurrentVersion => _updateManager?.CurrentVersion;

    public UpdateService()
    {
        try
        {
            const string repositoryUrl = "https://github.com/Ganom/SimpleTwitchEmoteSounds";
            var source = new GithubSource(repositoryUrl, "", false);
            var logger = new SerilogConsoleLogger(_logger);

#if DEBUG
            var testLocator = new TestVelopackLocator(
                appId: "SimpleTwitchEmoteSounds",
                version: "1.3.2",
                packagesDir: AppDomain.CurrentDomain.BaseDirectory
            );
            _updateManager = new CombinedChangelogUpdateManager(source, null, testLocator, logger);
#else
            _updateManager = new CombinedChangelogUpdateManager(source, null, null, logger);
#endif

            _logger.Information(
                "UpdateService initialized with repository: {Repository}",
                repositoryUrl
            );
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize UpdateService");
            _updateManager = null;
        }
    }

    public async Task CheckForUpdatesAsync()
    {
        if (_updateManager == null)
        {
            _logger.Warning("UpdateManager not initialized, skipping update check");
            return;
        }

        try
        {
            _logger.Information("Checking for updates...");
            var stopwatch = Stopwatch.StartNew();

            var updateInfo = await _updateManager.CheckForUpdatesAsync();
            stopwatch.Stop();

            _logger.Information(
                "Update check completed in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds
            );

            if (updateInfo != null)
            {
                _currentUpdateInfo = updateInfo;

                // Extract combined release notes if available
                if (updateInfo is ExtendedUpdateInfo extendedInfo)
                {
                    _combinedReleaseNotes = extendedInfo.CombinedReleaseNotes;
                    _logger.Information(
                        "Update available: {CurrentVersion} → {TargetVersion} (with {IntermediateCount} intermediate versions)",
                        CurrentVersion?.ToString() ?? "Unknown",
                        updateInfo.TargetFullRelease.Version?.ToString() ?? "Unknown",
                        extendedInfo.IntermediateReleases.Count
                    );
                }
                else
                {
                    _logger.Information(
                        "Update available: {CurrentVersion} → {TargetVersion}",
                        CurrentVersion?.ToString() ?? "Unknown",
                        updateInfo.TargetFullRelease.Version?.ToString() ?? "Unknown"
                    );
                }

                UpdateAvailable?.Invoke(
                    this,
                    new UpdateAvailableEventArgs(updateInfo, _combinedReleaseNotes)
                );
            }
            else
            {
                _logger.Information("No updates available");
                _currentUpdateInfo = null;
                _combinedReleaseNotes = null;
            }
        }
        catch (HttpRequestException ex)
        {
            const string errorMessage = "Network error during update check";
            _logger.Error(ex, errorMessage);
            UpdateError?.Invoke(this, new UpdateErrorEventArgs(ex, errorMessage));
        }
        catch (UnauthorizedAccessException ex)
        {
            const string errorMessage = "Authentication failed during update check";
            _logger.Error(ex, errorMessage);
            UpdateError?.Invoke(this, new UpdateErrorEventArgs(ex, errorMessage));
        }
        catch (Exception ex)
        {
            const string errorMessage = "Unexpected error during update check";
            _logger.Error(ex, errorMessage);
            UpdateError?.Invoke(this, new UpdateErrorEventArgs(ex, errorMessage));
        }
    }

    public async Task<bool> DownloadUpdateAsync(UpdateInfo updateInfo)
    {
        if (_updateManager == null)
        {
            _logger.Warning("UpdateManager not initialized, cannot download update");
            return false;
        }

        try
        {
            _logger.Information("Starting update download...");
            var stopwatch = Stopwatch.StartNew();

            await _updateManager.DownloadUpdatesAsync(
                updateInfo,
                progress =>
                {
                    _logger.Debug("Download progress: {Progress}%", progress);
                    UpdateProgress?.Invoke(this, new UpdateProgressEventArgs(progress));
                }
            );

            stopwatch.Stop();
            _logger.Information(
                "Update download completed in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds
            );
            return true;
        }
        catch (Exception ex)
        {
            var errorMessage = "Failed to download update";
            _logger.Error(ex, errorMessage);
            UpdateError?.Invoke(this, new UpdateErrorEventArgs(ex, errorMessage));
            return false;
        }
    }

    public async Task ApplyUpdateAndRestart(UpdateInfo updateInfo)
    {
        if (_updateManager == null)
        {
            _logger.Warning("UpdateManager not initialized, cannot apply update");
            return;
        }

        try
        {
            _logger.Information("Applying update and restarting application...");
            _logger.Warning("Application will now exit and restart with the new version");

            _updateManager.ApplyUpdatesAndRestart(updateInfo);
        }
        catch (Exception ex)
        {
            if (
                ex.Message.Contains("TestVelopackLocator")
                || ex.Message.Contains("UpdateExePath is not supported")
            )
            {
                _logger.Warning("Test environment detected, faking update completion");

                for (var i = 0; i <= 100; i += 2)
                {
                    UpdateProgress?.Invoke(this, new UpdateProgressEventArgs(i));
                    await Task.Delay(100);
                }

                _logger.Information("Fake update completed successfully");
                return;
            }

            var errorMessage = "Failed to apply update and restart";
            _logger.Error(ex, errorMessage);
            UpdateError?.Invoke(this, new UpdateErrorEventArgs(ex, errorMessage));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        try
        {
            _updateManager = null;
            _logger.Information("UpdateService disposed");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error disposing UpdateService");
        }

        await Task.CompletedTask;
    }
}
