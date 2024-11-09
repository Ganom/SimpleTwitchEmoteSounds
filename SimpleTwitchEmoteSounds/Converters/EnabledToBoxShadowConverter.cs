using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SimpleTwitchEmoteSounds.Converters;

public class EnabledToBoxShadowConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not [bool enabled, bool isMissingSoundFiles, bool isPointerOver])
            return new BoxShadows();
        
        if (isMissingSoundFiles)
        {
            return new BoxShadows(new BoxShadow
            {
                Color = Color.Parse("#ffd700"),
                Spread = isPointerOver ? 2 : 1,
                IsInset = false
            });
        }
        
        var color = enabled ? "#5dc264" : "#fc725a";

        return !enabled
            ? new BoxShadows(new BoxShadow
            {
                Color = Color.Parse(color),
                Spread = isPointerOver ? 3 : 2,
                IsInset = false
            })
            : new BoxShadows();
    }
}