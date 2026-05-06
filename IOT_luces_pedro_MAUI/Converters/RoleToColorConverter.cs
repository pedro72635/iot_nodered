using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace IOT_luces_pedro_MAUI.Converters;

public class RoleToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string role = value as string ?? "";
        return role.ToLower() switch
        {
            "user" => Color.FromArgb("#2D2D30"),
            "system" => Color.FromArgb("#332200"),
            "assistant" => Color.FromArgb("#1E3C50"),
            _ => Color.FromArgb("#1E1E1E")
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
