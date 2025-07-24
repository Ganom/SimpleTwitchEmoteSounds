#region

using System;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SimpleTwitchEmoteSounds.Services.Core;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using Velopack;

#endregion

namespace SimpleTwitchEmoteSounds.ViewModels;

public partial class UpdateAvailableDialogViewModel(
    ISukiDialog dialog,
    string currentVersion,
    string targetVersion,
    string releaseNotes,
    string packageSize,
    UpdateInfo updateInfo,
    IUpdateService updateService,
    ISukiToastManager toastManager
) : ObservableObject
{
    [ObservableProperty]
    private string _currentVersion = currentVersion;

    [ObservableProperty]
    private string _targetVersion = targetVersion;

    [ObservableProperty]
    private string _releaseNotes = releaseNotes;

    [ObservableProperty]
    private string _packageSize = packageSize;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private bool _isIndeterminate = true;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private bool _isTextVisible;

    [ObservableProperty]
    private string _downloadStatusText = "Preparing download...";

    [RelayCommand]
    private async Task AcceptUpdate()
    {
        try
        {
            IsDownloading = true;
            IsIndeterminate = true;
            IsTextVisible = false;
            DownloadStatusText = "Preparing download...";

            updateService.UpdateProgress += OnUpdateProgress;
            updateService.UpdateError += OnUpdateError;

            Log.Information("User accepted update to version {Version}", TargetVersion);

            DownloadStatusText = "Downloading update...";
            var downloadSuccess = await updateService.DownloadUpdateAsync(updateInfo);

            if (downloadSuccess)
            {
                DownloadStatusText = "Applying update...";
                IsIndeterminate = true;
                IsTextVisible = false;

                Log.Information("Update downloaded successfully, applying update...");
                await updateService.ApplyUpdateAndRestart(updateInfo);
            }
            else
            {
                ShowToast(
                    NotificationType.Error,
                    "Download Error",
                    "Failed to download update. Please try again."
                );
                dialog.Dismiss();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error applying update");
            ShowToast(
                NotificationType.Error,
                "Update Error",
                "Failed to apply update. Please try again."
            );
            dialog.Dismiss();
        }
        finally
        {
            updateService.UpdateProgress -= OnUpdateProgress;
            updateService.UpdateError -= OnUpdateError;
        }
    }

    [RelayCommand]
    private void DeclineUpdate()
    {
        Log.Information("User declined update to version {Version}", TargetVersion);
        dialog.Dismiss();
    }

    private void OnUpdateProgress(object? sender, UpdateProgressEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsIndeterminate = false;
            IsTextVisible = true;
            ProgressValue = e.Progress;
            DownloadStatusText = $"Downloading update... {e.Progress}%";
        });
    }

    private void OnUpdateError(object? sender, UpdateErrorEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsDownloading = false;
            ShowToast(NotificationType.Error, "Update Error", e.Message);
            dialog.Dismiss();
        });
    }

    private void ShowToast(NotificationType type, string title, string content)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            toastManager
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
}
