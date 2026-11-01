using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace VCWpfControls.Demo.Views;

/// <summary>Sample value converter: salary to 0..100 progress.</summary>
public class SalaryToProgressConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is decimal salary ? (double)salary : 0;

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>All column types sample.</summary>
public partial class ColumnTypesSampleView : UserControl
{
    /// <summary>Creates the view.</summary>
    public ColumnTypesSampleView()
    {
        Resources.Add("SalaryToProgress", new SalaryToProgressConverter());
        InitializeComponent();
        this.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "ColumnTypesGrid");
    }
}
