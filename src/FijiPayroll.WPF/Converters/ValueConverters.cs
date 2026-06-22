using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FijiPayroll.WPF.Views;

/// <summary>
/// Converts a boolean or null/empty state to Visibility.
/// Returns Visible if false or null/empty, Collapsed otherwise.
/// </summary>
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return Visibility.Visible;

        if (value is bool b)
            return b ? Visibility.Collapsed : Visibility.Visible;

        if (value is string s)
            return string.IsNullOrWhiteSpace(s) ? Visibility.Visible : Visibility.Collapsed;

        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts active status (boolean) to styled colors or string descriptors for WPF.
/// </summary>
public sealed class StatusToColorBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isActive = value is bool b && b;
        string param = parameter as string ?? "Text";

        switch (param)
        {
            case "Bg":
                return isActive 
                    ? new SolidColorBrush(Color.FromArgb(25, 72, 187, 120)) // 10% opacity green
                    : new SolidColorBrush(Color.FromArgb(25, 245, 101, 101)); // 10% opacity red
            
            case "Border":
                return isActive 
                    ? new SolidColorBrush(Color.FromArgb(60, 72, 187, 120)) 
                    : new SolidColorBrush(Color.FromArgb(60, 245, 101, 101));
            
            case "Brush":
                return isActive 
                    ? new SolidColorBrush(Color.FromRgb(72, 187, 120)) // green text
                    : new SolidColorBrush(Color.FromRgb(245, 101, 101)); // red text

            case "Text":
            default:
                return isActive ? "Active" : "Terminated";
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts leave request status string to styled colors for WPF.
/// </summary>
public sealed class StatusStringToColorBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string status = value as string ?? "Pending";
        string param = parameter as string ?? "Brush";

        // Determine base color based on status
        Color baseColor = status switch
        {
            "Approved" => Color.FromRgb(72, 187, 120), // Green
            "Rejected" => Color.FromRgb(245, 101, 101), // Red
            "Cancelled" => Color.FromRgb(160, 174, 192), // Gray
            "Pending" or _ => Color.FromRgb(237, 137, 54) // Orange
        };

        return param switch
        {
            "Bg" => new SolidColorBrush(Color.FromArgb(25, baseColor.R, baseColor.G, baseColor.B)),
            "Border" => new SolidColorBrush(Color.FromArgb(60, baseColor.R, baseColor.G, baseColor.B)),
            "Brush" or _ => new SolidColorBrush(baseColor)
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns Visibility.Visible if string value equals parameter, else Collapsed.
/// </summary>
public sealed class StringEqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string val = value as string ?? string.Empty;
        string param = parameter as string ?? string.Empty;

        return string.Equals(val, param, StringComparison.OrdinalIgnoreCase) 
            ? Visibility.Visible 
            : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
