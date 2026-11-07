using System.Windows.Controls;
using VCWpfControls.Localization;
using VCWpfControls.Pivot;

namespace VCWpfControls.Demo.Views;

/// <summary>Pivot sample: region x product sales with totals.</summary>
public partial class PivotSampleView : UserControl
{
    /// <summary>Creates the view.</summary>
    public PivotSampleView()
    {
        InitializeComponent();
        this.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "PivotSample");

        Pivot.RowFields.Add("Region");
        Pivot.ColumnFields.Add("Product");
        Pivot.Measures.Add(new PivotMeasure("Amount", GridSummaryFunction.Sum));
    }
}
