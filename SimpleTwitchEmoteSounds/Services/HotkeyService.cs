using System;
using System.Collections.Generic;
using Avalonia.Threading;
using Serilog;
using SharpHook;
using SharpHook.Native;
using SharpHook.Reactive;
using SimpleTwitchEmoteSounds.Models;

namespace SimpleTwitchEmoteSounds.Services;

public class HotkeyService : IHotkeyService
{
    private readonly SimpleReactiveGlobalHook _globalHook = new();
    private readonly Dictionary<Hotkey, Action> _hotkeys = new();
    private readonly HashSet<KeyCode> _currentlyPressedKeys = [];
    private Action<Hotkey>? _nextKeyCallback;
    private bool _isListeningForHotkey;

    private static readonly HashSet<KeyCode> ModifierKeys =
    [
        KeyCode.VcLeftControl,
        KeyCode.VcRightControl,
        KeyCode.VcLeftShift,
        KeyCode.VcRightShift,
        KeyCode.VcLeftAlt,
        KeyCode.VcRightAlt,
        KeyCode.VcLeftMeta,
        KeyCode.VcRightMeta
    ];

    public HotkeyService()
    {
        _globalHook.KeyPressed.Subscribe(OnKeyPressed);
        _globalHook.KeyReleased.Subscribe(OnKeyReleased);
        _globalHook.RunAsync();
    }

    public void RegisterHotkey(Hotkey combo, Action action)
    {
        Log.Information($"Registering hotkey for {combo}");
        _hotkeys[combo] = action;
    }

    public void UnregisterHotkey(Hotkey combo)
    {
        Log.Information($"Unregistering hotkey {combo}");
        _hotkeys.Remove(combo);
    }

    public void StartListeningForNextKey(Action<Hotkey> onKeyPressed)
    {
        _nextKeyCallback = onKeyPressed;
        _isListeningForHotkey = true;
        _currentlyPressedKeys.Clear();
    }

    public void StopListeningForNextKey()
    {
        _nextKeyCallback = null;
        _isListeningForHotkey = false;
        _currentlyPressedKeys.Clear();
    }

    private void OnKeyPressed(KeyboardHookEventArgs e)
    {
        _currentlyPressedKeys.Add(e.Data.KeyCode);

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_isListeningForHotkey)
            {
                if (ModifierKeys.Contains(e.Data.KeyCode)) return;

                var combo = new Hotkey(_currentlyPressedKeys);
                _nextKeyCallback?.Invoke(combo);
                StopListeningForNextKey();
            }
            else
            {
                var combo = new Hotkey(_currentlyPressedKeys);
                if (_hotkeys.TryGetValue(combo, out var action))
                {
                    action();
                }
            }
        });
    }

    private void OnKeyReleased(KeyboardHookEventArgs e)
    {
        _currentlyPressedKeys.Remove(e.Data.KeyCode);
    }

    public void Dispose()
    {
        _globalHook.Dispose();
    }
}

public interface IHotkeyService : IDisposable
{
    void RegisterHotkey(Hotkey combo, Action action);
    void UnregisterHotkey(Hotkey combo);
    void StartListeningForNextKey(Action<Hotkey> onKeyPressed);
    void StopListeningForNextKey();
}