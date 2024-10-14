using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SharpHook.Native;
using SimpleTwitchEmoteSounds.Models;

namespace SimpleTwitchEmoteSounds.Converters;

public class HotkeyConverter : JsonConverter<Hotkey>
{
    public override void WriteJson(JsonWriter writer, Hotkey? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartArray();
        foreach (var key in value.Keys)
        {
            writer.WriteValue((int)key);
        }

        writer.WriteEndArray();
    }

    public override Hotkey? ReadJson(JsonReader reader, Type objectType, Hotkey? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonToken.StartArray)
        {
            throw new JsonSerializationException("Expected start of array.");
        }

        var keys = new HashSet<KeyCode>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray)
            {
                break;
            }

            if (reader is { TokenType: JsonToken.Integer, Value: long intValue })
            {
                keys.Add((KeyCode)intValue);
            }
        }

        return new Hotkey(keys);
    }
}