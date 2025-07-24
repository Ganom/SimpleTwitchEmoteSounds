#region

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Serilog;
using SimpleTwitchEmoteSounds.Models;

#endregion

namespace SimpleTwitchEmoteSounds.Services;

public interface IAudioPlaybackService : IDisposable
{
    Task PlaySound(SoundCommand soundCommand);
    bool DoesSoundExist(SoundFile soundFile);
    string GetManagedAudioPath(string fileName);
    Task<string> CopyToManagedAudio(string sourceFilePath);
}

public class AudioPlaybackService : IAudioPlaybackService
{
    private const int MaxChannels = 12;

    private readonly ConcurrentQueue<AudioPlaybackRequest> _playbackQueue = new();
    private readonly AudioChannel[] _channels = new AudioChannel[MaxChannels];
    private readonly object _channelLock = new();
    private bool _disposed;

    public AudioPlaybackService()
    {
        for (var i = 0; i < MaxChannels; i++)
        {
            _channels[i] = new AudioChannel();
        }
    }

    public Task PlaySound(SoundCommand soundCommand)
    {
        if (soundCommand.SoundFiles.Count == 0)
        {
            return Task.CompletedTask;
        }

        var selectedFile = SelectRandomSoundFile(soundCommand);
        if (selectedFile == null || string.IsNullOrEmpty(selectedFile.FileName))
        {
            return Task.CompletedTask;
        }

        var filePath = GetManagedAudioPath(selectedFile.FileName);
        if (!File.Exists(filePath))
        {
            Log.Warning($"Managed audio file not found: {filePath}");
            return Task.CompletedTask;
        }

        var request = new AudioPlaybackRequest
        {
            FilePath = filePath,
            Volume = float.Parse(soundCommand.Volume),
            SoundCommand = soundCommand,
        };

        var channel = GetAvailableChannel();
        if (channel != null)
        {
            soundCommand.IncrementTimesPlayed();
            _ = PlayOnChannelAsync(channel, request);
        }
        else
        {
            _playbackQueue.Enqueue(request);
            Log.Debug(
                "All audio channels busy, sound request queued. Queue size: {QueueSize}",
                _playbackQueue.Count
            );
        }

        return Task.CompletedTask;
    }

    public bool DoesSoundExist(SoundFile soundFile)
    {
        if (string.IsNullOrEmpty(soundFile.FileName))
            return false;

        var managedPath = GetManagedAudioPath(soundFile.FileName);
        return File.Exists(managedPath);
    }

    public string GetManagedAudioPath(string fileName)
    {
        return AppDataPathService.GetAudioFilePath(fileName);
    }

    public async Task<string> CopyToManagedAudio(string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException($"Source file not found: {sourceFilePath}");

        var fileName = Path.GetFileName(sourceFilePath);
        var uniqueFileName = await GetUniqueFileName(fileName);
        var destinationPath = GetManagedAudioPath(uniqueFileName);

        await Task.Run(() => File.Copy(sourceFilePath, destinationPath, overwrite: false));
        Log.Information("Copied audio file to managed storage: {UniqueFileName}", uniqueFileName);

        return uniqueFileName;
    }

    private Task<string> GetUniqueFileName(string originalFileName)
    {
        var name = Path.GetFileNameWithoutExtension(originalFileName);
        var extension = Path.GetExtension(originalFileName);
        var counter = 1;
        var fileName = originalFileName;

        while (File.Exists(GetManagedAudioPath(fileName)))
        {
            fileName = $"{name}_{counter}{extension}";
            counter++;
        }

        return Task.FromResult(fileName);
    }

    private SoundFile? SelectRandomSoundFile(SoundCommand soundCommand)
    {
        var random = new Random();
        var totalProbability = soundCommand.SoundFiles.Sum(sf => float.Parse(sf.Percentage));
        var randomValue = (float)(random.NextDouble() * totalProbability);
        var cumulativeProbability = 0f;

        Log.Debug(
            "Sound selection: Total probability: {TotalProbability:F4}, Random value: {RandomValue:F4}",
            totalProbability,
            randomValue
        );

        foreach (var soundFile in soundCommand.SoundFiles)
        {
            var probability = float.Parse(soundFile.Percentage);
            cumulativeProbability += probability;

            Log.Debug(
                "Checking sound file: {SoundFileFileName}, Probability: {Probability:F4}, Cumulative: {CumulativeProbability:F4}",
                soundFile.FileName,
                probability,
                cumulativeProbability
            );

            if (!(randomValue <= cumulativeProbability))
                continue;
            Log.Debug("Selected sound file: {SoundFileFileName}", soundFile.FileName);
            return soundFile;
        }

        Log.Warning("No sound file selected");
        return null;
    }

    private AudioChannel? GetAvailableChannel()
    {
        lock (_channelLock)
        {
            return _channels.FirstOrDefault(c => !c.IsBusy);
        }
    }

    private async Task PlayOnChannelAsync(AudioChannel channel, AudioPlaybackRequest request)
    {
        try
        {
            await channel.PlayAsync(request);
        }
        finally
        {
            ProcessQueuedRequests();
        }
    }

    private void ProcessQueuedRequests()
    {
        while (_playbackQueue.TryDequeue(out var queuedRequest))
        {
            var availableChannel = GetAvailableChannel();
            if (availableChannel != null)
            {
                queuedRequest.SoundCommand.IncrementTimesPlayed();
                _ = PlayOnChannelAsync(availableChannel, queuedRequest);
            }
            else
            {
                _playbackQueue.Enqueue(queuedRequest);
            }
            break;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var channel in _channels)
        {
            channel?.Dispose();
        }

        _disposed = true;
    }

    private class AudioPlaybackRequest
    {
        public string FilePath { get; init; } = string.Empty;
        public float Volume { get; init; }
        public SoundCommand SoundCommand { get; set; } = null!;
    }

    private class AudioChannel : IDisposable
    {
        private WaveOutEvent? _outputDevice;
        private bool _disposed;

        public bool IsBusy { get; private set; }

        public async Task PlayAsync(AudioPlaybackRequest request)
        {
            if (IsBusy || _disposed)
                return;

            IsBusy = true;
            try
            {
                await using var audioFile = new AudioFileReader(request.FilePath);
                _outputDevice = new WaveOutEvent();
                var volumeProvider = new VolumeSampleProvider(audioFile)
                {
                    Volume = request.Volume,
                };

                _outputDevice.Init(volumeProvider);
                _outputDevice.Play();

                while (_outputDevice.PlaybackState == PlaybackState.Playing && !_disposed)
                {
                    await Task.Delay(50);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error playing audio file: {request.FilePath}");
            }
            finally
            {
                _outputDevice?.Dispose();
                _outputDevice = null;
                IsBusy = false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _outputDevice?.Dispose();
            _disposed = true;
        }
    }
}
