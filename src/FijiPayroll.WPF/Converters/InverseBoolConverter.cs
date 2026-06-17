using System.Globalization;
using System.Windows.Data;

namespace FijiPayroll.WPF.Converters;

/// <summary>
/// WPF value converter that returns the logical inverse of a boolean value.
/// Used to disable UI elements for read-only system components.
/// </summary>
/// <remarks>
/// Implemented as a singleton (<see cref="Instance"/>) to be referenced
/// directly from XAML via <c>{x:Static conv:InverseBoolConverter.Instance}</c>
/// without a resource dictionary entry.
/// </remarks>
public sealed class InverseBoolConverter : IValueConverter
{
    /// <summary>Gets the shared singleton instance for XAML binding.</summary>
    public static readonly InverseBoolConverter Instance = new();

    private InverseBoolConverter() { }

    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;
}
