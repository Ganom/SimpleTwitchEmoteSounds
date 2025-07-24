using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using Material.Icons.Avalonia;
using Serilog;
using SharpHook.Data;
using SimpleTwitchEmoteSounds.Models;
using SimpleTwitchEmoteSounds.Services;
using SimpleTwitchEmoteSounds.Services.Core;
using SimpleTwitchEmoteSounds.Services.Database;
using SimpleTwitchEmoteSounds.Services.Migration;
using SimpleTwitchEmoteSounds.Views;
using SukiUI;
using SukiUI.Dialogs;
using SukiUI.Enums;
using SukiUI.Models;
using SukiUI.Toasts;
using Velopack;

namespace SimpleTwitchEmoteSounds.ViewModels;

public partial class AppViewModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    private ViewModelBase? _activePage;

    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private string _versionButtonText = "Loading...";

    [ObservableProperty]
    private string _currentVersion = string.Empty;

    [ObservableProperty]
    private string _targetVersion = string.Empty;

    [ObservableProperty]
    private string _releaseNotes = string.Empty;

    [ObservableProperty]
    private string _packageSize = string.Empty;

    public ISukiToastManager ToastManager { get; }
    public ISukiDialogManager DialogManager { get; }

    private readonly PageNavigationService _pageNavigationService;
    private readonly IUpdateService _updateService;
    private readonly JsonToDbMigrationService _migrationService;
    private readonly DatabaseConfigService _configService;

    private bool _disposed;
    private UpdateInfo? _pendingUpdate;

    public AppViewModel(
        IEnumerable<ViewModelBase> appPages,
        PageNavigationService pageNavigationService,
        ISukiDialogManager dialogManager,
        ISukiToastManager toastManager,
        IUpdateService updateService,
        IHotkeyService hotkeyService,
        JsonToDbMigrationService migrationService,
        DatabaseConfigService configService
    )
    {
        DialogManager = dialogManager;
        ToastManager = toastManager;
        _pageNavigationService = pageNavigationService;
        _updateService = updateService;
        _migrationService = migrationService;
        _configService = configService;

        var viewModelBases = appPages.ToList();
        AppPages = new AvaloniaList<ViewModelBase>(viewModelBases);
        ActivePage = viewModelBases[0];

        pageNavigationService.NavigationRequested += pageType =>
        {
            var page = AppPages.FirstOrDefault(x => x.GetType() == pageType);
            if (page is null || ActivePage?.GetType() == pageType)
                return;
            ActivePage = page;
        };

        hotkeyService.RegisterHotkey(
            new Hotkey([KeyCode.VcLeftControl, KeyCode.VcLeftShift, KeyCode.VcC]),
            () => _ = CopyLatestLogsToClipboard()
        );

        // Initialize theme
        SukiTheme
            .GetInstance()
            .ChangeColorTheme(
                new SukiColorTheme("Koko", Color.Parse("#B24DB0"), Color.Parse("#ED8E12"))
            );

        _updateService.UpdateAvailable += OnUpdateAvailable;
        CurrentVersion = _updateService.CurrentVersion?.ToString() ?? "Unknown";
        UpdateVersionButtonText();
    }

    public IAvaloniaReadOnlyList<ViewModelBase> AppPages { get; }

    [RelayCommand]
    private Task ViewSoundCommandStats()
    {
        var dialog = new SoundStatsDialogView(_configService)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        dialog.Show();
        return Task.CompletedTask;
    }

    #region Menu Commands

    [RelayCommand]
    private static void OpenLogsFolder()
    {
        try
        {
            var logsPath = AppDataPathService.GetLogsPath();
            Process.Start(new ProcessStartInfo
            {
                FileName = logsPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error opening logs folder");
        }
    }

    [RelayCommand]
    private static void OpenSettingsFolder()
    {
        try
        {
            var settingsPath = AppDataPathService.GetSettingsPath();
            Process.Start(new ProcessStartInfo
            {
                FileName = settingsPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error opening settings folder");
        }
    }

    [RelayCommand]
    private async Task CopyLatestLogsToClipboard()
    {
        try
        {
            var logsPath = AppDataPathService.GetLogsPath();
            var logFiles = Directory
                .GetFiles(logsPath, "chat-app-*.txt")
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .ToArray();

            if (logFiles.Length == 0)
            {
                ShowToast(
                    NotificationType.Warning,
                    "No Logs Found",
                    "No log files found in the logs directory"
                );
                return;
            }

            var latestLogFile = logFiles[0];
            string logContent;

            await using (
                var fileStream = new FileStream(
                    latestLogFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite
                )
            )
            using (var reader = new StreamReader(fileStream))
            {
                logContent = await reader.ReadToEndAsync();
            }

            var clipboard = TopLevel
                .GetTopLevel(
                    (
                        (IClassicDesktopStyleApplicationLifetime)
                        Application.Current!.ApplicationLifetime!
                    ).MainWindow
                )
                ?.Clipboard;

            if (clipboard != null)
            {
                await clipboard.SetTextAsync(logContent);
                var fileName = Path.GetFileName(latestLogFile);
                ShowToast(
                    NotificationType.Success,
                    "Logs Copied",
                    $"Latest log file ({fileName}) copied to clipboard"
                );
            }
            else
            {
                ShowToast(NotificationType.Error, "Clipboard Error", "Could not access clipboard");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error copying logs to clipboard");
            ShowToast(NotificationType.Error, "Error", "Failed to copy logs to clipboard");
        }
    }

    [RelayCommand]
    private async Task ImportSoundsJson()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(GetMainWindow());
            if (topLevel == null)
            {
                ShowToast(NotificationType.Error, "Error", "Could not access file dialog");
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select sounds.json file to import",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("JSON Files")
                    {
                        Patterns = ["*.json"]
                    },
                ]
            });

            if (files.Count == 0)
                return;

            var selectedFile = files[0];
            var filePath = selectedFile.Path.LocalPath;

            if (string.IsNullOrEmpty(filePath))
            {
                ShowToast(NotificationType.Error, "Error", "Could not access the selected file");
                return;
            }

            ShowToast(NotificationType.Information, "Migration", "Starting migration from sounds.json...");

            _migrationService.MigrateFromSpecificFile(filePath);
            _configService.ReloadSettingsAfterMigration();
            var dashboardViewModel = AppPages.OfType<DashboardViewModel>().FirstOrDefault();
            dashboardViewModel?.RefreshAfterMigration();

            ShowToast(NotificationType.Success, "Migration Complete", "Successfully imported sounds from sounds.json file");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error importing sounds.json file");
            ShowToast(NotificationType.Error, "Import Failed", $"Failed to import sounds.json: {ex.Message}");
        }
    }

    [RelayCommand]
    private static void GetSupport()
    {
        try
        {
            const string discordInvite = "https://discord.gg/EgKhdFXnwF";
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = discordInvite,
                    UseShellExecute = true
                }
            );
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error opening Discord support link");
        }
    }

    [RelayCommand]
    private void ExitApplication()
    {
        if (
            Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime lifetime
        )
        {
            lifetime.Shutdown();
        }
    }

    [RelayCommand]
    private void ShowAbout()
    {
        DialogManager
            .CreateDialog()
            .WithTitle("About STES")
            .WithContent(
                $"Version: {CurrentVersion}\n\nCreated by Ganom"
            )
            .Dismiss()
            .ByClickingBackground()
            .TryShow();
    }

    #endregion

    #region Update Management - EXACT copy from SplashViewModel

    [RelayCommand]
    private async Task ShowUpdateInfo()
    {
        if (IsUpdateAvailable && _pendingUpdate != null)
        {
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                DialogManager
                    .CreateDialog()
                    .WithTitle($"Update to {TargetVersion} available!")
                    .WithViewModel(dialog => new UpdateAvailableDialogViewModel(
                        dialog,
                        CurrentVersion,
                        TargetVersion,
                        ReleaseNotes,
                        PackageSize,
                        _pendingUpdate,
                        _updateService,
                        ToastManager
                    ))
                    .Dismiss()
                    .ByClickingBackground()
                    .TryShow();
            });
        }
        else
        {
            ShowToast(
                NotificationType.Information,
                "Checking for Updates",
                "Checking for available updates..."
            );

            try
            {
                await _updateService.CheckForUpdatesAsync();

                if (!IsUpdateAvailable)
                {
                    ShowToast(
                        NotificationType.Success,
                        "No Updates Available",
                        $"You're running the latest version: {CurrentVersion}"
                    );
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking for updates");
                ShowToast(
                    NotificationType.Error,
                    "Update Check Failed",
                    "Failed to check for updates. Please try again later."
                );
            }
        }
    }

    private void UpdateVersionButtonText()
    {
        VersionButtonText = IsUpdateAvailable ? "Update Available" : $"Version {CurrentVersion}";
    }

    private void OnUpdateAvailable(object? sender, UpdateAvailableEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _pendingUpdate = e.UpdateInfo;
            TargetVersion = e.UpdateInfo.TargetFullRelease.Version?.ToString() ?? "Unknown";
            ReleaseNotes = e.CombinedReleaseNotes ?? "No release notes available.";
            PackageSize = FormatBytes(e.UpdateInfo.TargetFullRelease.Size);
            IsUpdateAvailable = true;
            UpdateVersionButtonText();
            ToastManager
                .CreateToast()
                .OfType(NotificationType.Information)
                .WithTitle("Update Available")
                .WithContent($"Update version {TargetVersion} is available for download!")
                .Dismiss()
                .ByClicking()
                .WithActionButton(
                    new MaterialIcon
                    {
                        Kind = MaterialIconKind.Update
                    },
                    _ =>
                    {
                        Dispatcher.UIThread.InvokeAsync(ShowUpdateInfo);
                    },
                    true,
                    SukiButtonStyles.Flat | SukiButtonStyles.Accent | SukiButtonStyles.Icon
                )
                .WithActionButton(
                    new MaterialIcon
                    {
                        Kind = MaterialIconKind.Close
                    },
                    _ => { },
                    true,
                    SukiButtonStyles.Icon
                )
                .Queue();
        });
    }

    private static string FormatBytes(long bytes)
    {
        const int scale = 1024;
        string[] orders = ["bytes", "KB", "MB", "GB", "TB"];
        var max = (long)Math.Pow(scale, orders.Length - 1);

        foreach (var order in orders)
        {
            if (bytes > max)
                return $"{decimal.Divide(bytes, max):##.##} {order}";
            max /= scale;
        }
        return "0 bytes";
    }

    #endregion

    private void ShowToast(NotificationType type, string title, string content)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            ToastManager
                .CreateToast()
                .OfType(type)
                .WithTitle(title)
                .WithContent(content)
                .Dismiss()
                .ByClicking()
                .Dismiss()
                .After(TimeSpan.FromSeconds(3))
                .Queue();
        });
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _updateService.UpdateAvailable -= OnUpdateAvailable;
        }
        finally
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    private static Window GetMainWindow()
    {
        return ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!;
    }
}
