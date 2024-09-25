using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SimpleTwitchEmoteSounds.Converters;

public class BoolToEnabledDisabledConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? "Enabled" : "Disabled";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}