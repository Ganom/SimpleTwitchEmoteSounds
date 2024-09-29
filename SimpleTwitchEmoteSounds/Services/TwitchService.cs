using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MiniTwitch.Irc;
using MiniTwitch.Irc.Models;
using Serilog;

namespace SimpleTwitchEmoteSounds.Services;

public class TwitchService
{
    private readonly IrcClient _client;
    public event Action<TwitchStatus>? ConnectionStatus;
    public event Action<Privmsg>? MessageLogged;
    private bool _connected;

    public TwitchService()
    {
        var loggerFactory = new LoggerFactory().AddSerilog(Log.Logger);
        var microsoftLogger = loggerFactory.CreateLogger<IrcClient>();

        _client = new IrcClient(options =>
        {
            options.Anonymous = true;
            options.Logger = microsoftLogger;
            options.ReconnectionDelay = TimeSpan.FromSeconds(10);
        });
        _client.OnChannelJoin += ChannelJoinEvent;
        _client.OnConnect += OnConnect;
        _client.OnDisconnect += OnDisconnect;
        _client.OnReconnect += OnReconnect;
        _client.OnMessage += MessageEvent;
    }

    private ValueTask OnConnect()
    {
        Log.Information($"Connected.");
        ConnectionStatus?.Invoke(TwitchStatus.Connected);
        return ValueTask.CompletedTask;
    }

    private ValueTask OnDisconnect()
    {
        Log.Information($"Disconnected.");
        ConnectionStatus?.Invoke(TwitchStatus.Disconnected);
        return ValueTask.CompletedTask;
    }

    private ValueTask OnReconnect()
    {
        Log.Information($"Reconnected.");
        ConnectionStatus?.Invoke(TwitchStatus.Reconnecting);
        return ValueTask.CompletedTask;
    }

    public async void ConnectAsync(string channel)
    {
        if (_connected)
        {
            await _client.ReconnectAsync();
            return;
        }

        await _client.ConnectAsync();
        await _client.JoinChannel(channel);
        _connected = true;
    }

    public async void DisconnectAsync()
    {
        foreach (var channel in _client.JoinedChannels)
        {
            await _client.PartChannel(channel);
        }
        
        await _client.DisconnectAsync();
    }

    private ValueTask ChannelJoinEvent(IrcChannel channel)
    {
        Log.Information($"Channel joined: {channel.Name}");
        return ValueTask.CompletedTask;
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