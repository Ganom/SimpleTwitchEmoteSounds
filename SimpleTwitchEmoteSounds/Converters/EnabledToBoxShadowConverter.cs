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
        if (values is not [bool enabled, bool isPointerOver])
            return new BoxShadows();

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