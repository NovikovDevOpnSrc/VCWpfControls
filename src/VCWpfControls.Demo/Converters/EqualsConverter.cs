using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VCWpfControls.Demo.Converters;

/// <summary>Converts value == parameter into IsChecked (two-way for radio groups).</summary>
public sealed class EqualsConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value as string, parameter as string, StringComparison.Ordinal);

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? parameter! : Binding.DoNothing;
}
