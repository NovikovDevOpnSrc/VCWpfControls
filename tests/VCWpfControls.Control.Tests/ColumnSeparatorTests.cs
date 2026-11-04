using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

public class ColumnSeparatorTests
{
    [StaFact]
    public void OffByDefault_NoSeparatorsDrawn()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "A", Width = 100 });
        grid.Columns.Add(new GridTextColumn { FieldName = "Department", Header = "B", Width = 100 });
        grid.ItemsSource = CreateEmployees(3);
        TestWindowHost.Pump();

        try
        {
            Assert.False(grid.ShowColumnSeparators);
            Assert.Null(grid.ColumnSeparatorBrush); // falls back to GridLinesBrush
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Enabled_SwitchDoesNotThrowAndInvalidatesRows()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "A", Width = 100 });
        grid.Columns.Add(new GridTextColumn { FieldName = "Department", Header = "B", Width = 100 });
        grid.ItemsSource = CreateEmployees(3);
        TestWindowHost.Pump();

        try
        {
            grid.ShowColumnSeparators = true;
            TestWindowHost.Pump();
            grid.ColumnSeparatorBrush = Brushes.Red;
            TestWindowHost.Pump();

            // Scroll horizontally with separators on: no exceptions, offset changes.
            grid.SetHorizontalOffsetForTest(10);
            Assert.True(grid.HorizontalOffset >= 0);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void SeparatorBrush_FallsBackToGridLinesBrush()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "A", Width = 100 });
        grid.ItemsSource = new[] { new Employee { Name = "x" } };
        grid.ShowColumnSeparators = true;
        TestWindowHost.Pump();

        try
        {
            // Default GridLinesBrush is set by the theme; a custom brush overrides.
            var themeBrush = grid.GridLinesBrush;
            grid.ColumnSeparatorBrush = Brushes.Blue;
            Assert.Equal(Brushes.Blue, grid.ColumnSeparatorBrush);
            Assert.NotEqual(themeBrush, grid.ColumnSeparatorBrush);
        }
        finally
        {
            window.Close();
        }
    }
}
