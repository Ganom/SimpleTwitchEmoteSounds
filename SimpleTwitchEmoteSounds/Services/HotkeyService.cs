using System;
using System.Collections.Generic;
using Avalonia.Threading;
using SharpHook;
using SharpHook.Native;
using SharpHook.Reactive;

namespace SimpleTwitchEmoteSounds.Services;

public class HotkeyService : IHotkeyService
{
    private readonly SimpleReactiveGlobalHook _globalHook = new();
    private readonly Dictionary<KeyCode, Action> _hotkeys = new();
    private Action<KeyCode>? _nextKeyCallback;

    public HotkeyService()
    {
        _globalHook.KeyPressed.Subscribe(OnKeyPressed);
        _globalHook.RunAsync();
    }

    public void RegisterHotkey(KeyCode key, Action action)
    {
        _hotkeys[key] = action;
    }

    public void UnregisterHotkey(KeyCode key)
    {
        _hotkeys.Remove(key);
    }

    public void StartListeningForNextKey(Action<KeyCode> onKeyPressed)
    {
        _nextKeyCallback = onKeyPressed;
    }

    public void StopListeningForNextKey()
    {
        _nextKeyCallback = null;
    }

    private void OnKeyPressed(KeyboardHookEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_nextKeyCallback != null)
            {
                _nextKeyCallback(e.Data.KeyCode);
                _nextKeyCallback = null;
            }
            else if (_hotkeys.TryGetValue(e.Data.KeyCode, out var action))
            {
                action();
            }
        });
    }

    public void Dispose()
    {
        _globalHook.Dispose();
    }
}

public interface IHotkeyService : IDisposable
{
    void RegisterHotkey(KeyCode key, Action action);
    void UnregisterHotkey(KeyCode key);
    void StartListeningForNextKey(Action<KeyCode> onKeyPressed);
    void StopListeningForNextKey();
}