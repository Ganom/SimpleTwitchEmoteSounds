using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SharpHook.Native;
using SimpleTwitchEmoteSounds.Converters;

namespace SimpleTwitchEmoteSounds.Models;

[JsonConverter(typeof(HotkeyConverter))]
public class Hotkey(HashSet<KeyCode> keys)
{
    public HashSet<KeyCode> Keys { get; } = [..keys];

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
        return string.Join(" + ", Keys.Select(k => k.ToString().Replace("Vc", "")));
    }
}