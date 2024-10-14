using System.Collections.Generic;
using System.Linq;
using SharpHook.Native;

namespace SimpleTwitchEmoteSounds.Models;

public class Hotkey(HashSet<KeyCode> keys)
{
    private HashSet<KeyCode> Keys { get; } = [..keys];

    public override bool Equals(object? obj)
    {
        return obj is Hotkey combo &&
               Keys.SetEquals(combo.Keys);
    }

    public override int GetHashCode()
    {
        return Keys.Count;
    }

    public override string ToString()
    {
        return string.Join("+", Keys.Select(k => k.ToString().Replace("Vc", "")));
    }
}