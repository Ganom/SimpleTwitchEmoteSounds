using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MiniTwitch.Irc;
using MiniTwitch.Irc.Interfaces;
using MiniTwitch.Irc.Models;
using Serilog;

namespace SimpleTwitchEmoteSounds.Services;

public class TwitchService
{
    private readonly IrcClient _client;
    public event Action<TwitchStatus>? ConnectionStatus;
    public event Action<Privmsg>? MessageLogged;
    private bool _isFirstConnect = true;
    private bool _connected;

    public TwitchService()
    {
        var loggerFactory = new LoggerFactory().AddSerilog(Log.Logger);
        var microsoftLogger = loggerFactory.CreateLogger<IrcClient>();

        _client = new IrcClient(options =>
        {
            options.Anonymous = true;
            options.Logger = microsoftLogger;
            options.ReconnectionDelay = TimeSpan.FromSeconds(5);
        });
        _client.OnChannelJoin += ChannelJoinEvent;
        _client.OnChannelPart += ChannelPartEvent;
        _client.OnConnect += OnConnect;
        _client.OnDisconnect += OnDisconnect;
        _client.OnReconnect += OnReconnect;
        _client.OnMessage += MessageEvent;
    }

    private ValueTask OnConnect()
    {
        Log.Information("Connected.");
        _connected = true;
        ConnectionStatus?.Invoke(TwitchStatus.Connected);
        return ValueTask.CompletedTask;
    }

    private ValueTask OnDisconnect()
    {
        Log.Information("Disconnected.");
        if (!_connected)
        {
            ConnectionStatus?.Invoke(TwitchStatus.Reconnecting);
            return ValueTask.CompletedTask;
        }

        _connected = false;
        ConnectionStatus?.Invoke(TwitchStatus.Disconnected);
        return ValueTask.CompletedTask;
    }

    private ValueTask OnReconnect()
    {
        _connected = true;
        Log.Information("Reconnected.");
        ConnectionStatus?.Invoke(TwitchStatus.Connected);
        return ValueTask.CompletedTask;
    }

    public async Task ConnectAsync(string channel)
    {
        Log.Information("Connecting...");
        if (string.IsNullOrWhiteSpace(channel))
        {
            Log.Information("Channel is empty.");
            return;
        }

        if (!_isFirstConnect)
        {
            await _client.ReconnectAsync();
            await _client.JoinChannel(channel);
            return;
        }

        await _client.ConnectAsync();
        await _client.JoinChannel(channel);
        _isFirstConnect = false;
    }

    public async Task DisconnectAsync()
    {
        foreach (var channel in _client.JoinedChannels)
        {
            Log.Information($"Disconnecting from {channel}.");
            await _client.PartChannel(channel);
        }
    }

    private ValueTask ChannelJoinEvent(IrcChannel channel)
    {
        Log.Information($"Channel joined: {channel.Name}, we're in {_client.JoinedChannels.Count} channels");
        return ValueTask.CompletedTask;
    }

    private async ValueTask ChannelPartEvent(IPartedChannel channel)
    {
        Log.Information($"Channel parted: {channel.Name}");

        if (_client.JoinedChannels.Count == 0)
        {
            Log.Information("We are in 0 channels, disconnecting.");
            await _client.DisconnectAsync();
        }
    }

    private ValueTask MessageEvent(Privmsg message)
    {
        MessageLogged?.Invoke(message);
        return ValueTask.CompletedTask;
    }
}

public enum TwitchStatus
{
    Disconnected,
    Connected,
    Reconnecting
}