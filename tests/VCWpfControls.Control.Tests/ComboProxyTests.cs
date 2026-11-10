using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Regression (user report): the ColumnTypes page bound its combo/auto-complete columns'
/// ItemsSource with RelativeSource AncestorType=Window — but a column is a DependencyObject
/// OUTSIDE the visual tree, so the ancestor walk never resolves and the dropdown opened
/// EMPTY. The demo now uses the canonical BindingProxy; this test proves the proxy path
/// populates the editor and the old ancestor path does not.
/// </summary>
/// <summary>The demo's canonical proxy, inlined for the test (Freezable inherits DataContext).</summary>
public sealed class TestBindingProxy : Freezable
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(object), typeof(TestBindingProxy), new PropertyMetadata(null));

    public object? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    protected override Freezable CreateInstanceCore() => new TestBindingProxy();
}

public class ComboProxyTests
{
    private enum Department { It, Hr, Sales }

    private sealed class Row
    {
        public string Name { get; set; } = "A";
        public Department Department { get; set; } = Department.It;
    }

    private sealed class PageVm
    {
        public List<Row> Rows { get; } = new() { new Row(), new Row { Department = Department.Sales } };
        public Department[] Departments { get; } = System.Enum.GetValues<Department>();
    }

    private static (Window Window, GridControl Grid, GridComboBoxColumn Column) Create(bool useProxy)
    {
        var window = new Window
        {
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -32000,
            Top = -32000,
        };
        var grid = new GridControl { Width = 400, Height = 200 };
        var column = new GridComboBoxColumn { FieldName = "Department", Header = "Dept", Width = new GridColumnWidth(150, System.Windows.GridUnitType.Pixel) };
        if (useProxy)
        {
            var proxy = new TestBindingProxy { Data = window.DataContext };
            window.DataContext = new PageVm();
            proxy.Data = window.DataContext; // same as the demo's Data="{Binding}"
            System.Windows.Data.BindingOperations.SetBinding(column, GridComboBoxColumn.ItemsSourceProperty, new System.Windows.Data.Binding("Departments") { Source = proxy.Data });
        }
        else
        {
            window.DataContext = new PageVm();
            System.Windows.Data.BindingOperations.SetBinding(column, GridComboBoxColumn.ItemsSourceProperty,
                new System.Windows.Data.Binding("DataContext.Departments")
                {
                    RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(Window), 1),
                });
        }

        grid.Columns.Add(column);
        var vm = (PageVm)window.DataContext;
        grid.ItemsSource = vm.Rows; // direct: the window lacks INPC, "Rows" would never re-flow
        window.Content = grid;
        window.Show();
        TestWindowHost.Pump();
        return (window, grid, column);
    }

    [StaFact]
    public void AncestorBinding_OnColumn_DoesNotResolve()
    {
        var (window, grid, column) = Create(useProxy: false);
        try
        {
            Assert.Null(column.ItemsSource); // the documented failure mode
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ProxyBinding_PopulatesTheEditor()
    {
        var (window, grid, column) = Create(useProxy: true);
        try
        {
            Assert.NotNull(column.ItemsSource);

            grid.BeginEdit(((Grid.Data.GridDataRow)grid.DataEngine.View[0]).Item, column);
            TestWindowHost.Pump();
            var row = (Grid.Primitives.GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
            var panel = row.Template.FindName("PART_Cells", row) as Grid.Primitives.GridCellsPanel;
            var editor = Assert.IsType<ComboBox>(panel?.EditingElement);
            Assert.Equal(3, editor.Items.Count); // It, Hr, Sales — the dropdown is NOT empty
        }
        finally
        {
            window.Close();
        }
    }
}
