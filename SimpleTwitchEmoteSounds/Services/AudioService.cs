using SimpleTwitchEmoteSounds.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Serilog;

namespace SimpleTwitchEmoteSounds.Services;

public static class AudioService
{
    public static async Task PlaySound(SoundCommand soundCommand)
    {
        if (soundCommand.SoundFiles.Count == 0)
        {
            return;
        }

        var selectedFile = SelectRandomSoundFile(soundCommand);

        if (string.IsNullOrEmpty(selectedFile.FilePath) || !File.Exists(selectedFile.FilePath))
        {
            return;
        }

        try
        {
            await PlayAudioFile(selectedFile.FilePath, soundCommand.Volume);
        }
        catch (Exception ex)
        {
            Log.Error($"Error previewing sound: {ex.Message}");
        }
    }

    private static SoundFile SelectRandomSoundFile(SoundCommand soundCommand)
    {
        var random = new Random();
        var totalPercentage = soundCommand.SoundFiles.Sum(sf => sf.Percentage);
        var randomValue = random.Next(1, totalPercentage + 1);
        var cumulativePercentage = 0;

        foreach (var soundFile in soundCommand.SoundFiles)
        {
            cumulativePercentage += soundFile.Percentage;
            if (randomValue <= cumulativePercentage)
            {
                return soundFile;
            }
        }

        return null;
    }

    private static async Task PlayAudioFile(string filePath, float volume)
    {
        await using var audioFile = new AudioFileReader(filePath);
        using var outputDevice = new WaveOutEvent();
        var volumeProvider = new VolumeSampleProvider(audioFile)
        {
            Volume = volume
        };

        outputDevice.Init(volumeProvider);
        outputDevice.Play();

        while (outputDevice.PlaybackState == PlaybackState.Playing)
        {
            await Task.Delay(100);
        }
    }
}