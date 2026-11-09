using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Automation;
using VCWpfControls.Grid.Data;
using VCWpfControls.Grid.Primitives;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The group header template (a custom DataTemplate with a GridGroupHeaderInfo context
/// replacing the default caption) and the per-cell UIA polish (Value pattern read/write
/// through the paste conversion path, GridItem coordinates).
/// </summary>
public class GridGroupHeaderAndCellUiaTests
{
    private static ObservableCollection<Employee> CreateEmployees()
        => new()
        {
            new() { Id = 1, Name = "Alice", Department = "IT", Salary = 100m },
            new() { Id = 2, Name = "Bob", Department = "IT", Salary = 90m },
            new() { Id = 3, Name = "Cara", Department = "HR", Salary = 80m },
        };

    private static DataTemplate CaptionTemplate()
    {
        var factory = new FrameworkElementFactory(typeof(TextBlock));
        factory.SetBinding(TextBlock.TextProperty, new Binding("Caption"));
        return new DataTemplate { VisualTree = factory };
    }

    private static ContentPresenter? GroupContentOf(GridControl grid)
    {
        var row = grid.ItemContainerGenerator.ContainerFromIndex(0) as GridRow;
        return row?.Template.FindName("PART_GroupHeaderContent", row) as ContentPresenter;
    }

    private static GridRow DataRowAt(GridControl grid, int viewIndex)
        => (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(viewIndex);

    [StaFact]
    public void GroupHeaderTemplate_ReplacesDefaultCaptionWithContext()
    {
        var grid = new GridControl { ItemsSource = CreateEmployees() };
        var window = TestWindowHost.Show(grid);
        grid.GroupDescriptions.Add(new GridGroupDescription("Department"));
        TestWindowHost.Pump();
        try
        {
            grid.GroupHeaderTemplate = CaptionTemplate();
            TestWindowHost.Pump();

            // The first view row is the IT group; the template content took over.
            Assert.Equal("IT", grid.Items[0] is GridGroupRow g ? g.Group.Key : null);
            var content = GroupContentOf(grid);
            Assert.NotNull(content);
            Assert.Equal(Visibility.Visible, content!.Visibility);

            var info = Assert.IsType<GridGroupHeaderInfo>(content.Content);
            Assert.Equal("IT", info.Key);
            Assert.Equal(2, info.ItemCount); // Alice + Bob
            Assert.Equal(0, info.Level);
            Assert.Equal("Department", info.ColumnName);
            Assert.Contains("IT", info.Caption, StringComparison.Ordinal); // default caption fallback

            var row = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
            var text = (TextBlock)row.Template.FindName("PART_GroupText", row);
            Assert.Equal(Visibility.Collapsed, text.Visibility);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void GroupHeaderTemplate_NullAfterCustom_KeepsDefaultCaptionFresh()
    {
        var grid = new GridControl { ItemsSource = CreateEmployees() };
        var window = TestWindowHost.Show(grid);
        grid.GroupDescriptions.Add(new GridGroupDescription("Department"));
        grid.GroupHeaderTemplate = CaptionTemplate();
        TestWindowHost.Pump();
        try
        {
            grid.GroupHeaderTemplate = null; // runtime change must restore the default caption
            TestWindowHost.Pump();

            var content = GroupContentOf(grid);
            Assert.NotNull(content);
            Assert.Equal(Visibility.Collapsed, content!.Visibility);

            var row = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
            var text = (TextBlock)row.Template.FindName("PART_GroupText", row);
            Assert.Equal(Visibility.Visible, text.Visibility);
            Assert.Contains("IT", text.Text, StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void CellPeer_ValuePattern_ReadsFormattedValue()
    {
        var grid = new GridControl { ItemsSource = CreateEmployees() };
        var window = TestWindowHost.Show(grid);
        TestWindowHost.Pump();
        try
        {
            var rowPeer = new GridRowAutomationPeer(DataRowAt(grid, 0));
            var peers = rowPeer.GetChildren()?.OfType<GridCellAutomationPeer>().ToList();
            var nameCell = peers!.First(p => p.GetAutomationId()!.EndsWith("_Name", StringComparison.Ordinal));

            var value = (IValueProvider?)nameCell.GetPattern(PatternInterface.Value);
            Assert.NotNull(value);
            Assert.False(value!.IsReadOnly);
            Assert.Equal("Alice", value.Value);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void CellPeer_ValuePattern_WritesThroughAndRejectsReadOnly()
    {
        var grid = new GridControl { ItemsSource = CreateEmployees() };
        var window = TestWindowHost.Show(grid);
        TestWindowHost.Pump();
        try
        {
            var employees = (ObservableCollection<Employee>)grid.ItemsSource!;
            var nameColumn = grid.Columns.First(c => c.FieldName == "Name");
            var idColumn = grid.Columns.First(c => c.FieldName == "Id");
            idColumn.ReadOnly = true;

            var rowPeer = new GridRowAutomationPeer(DataRowAt(grid, 0));
            var peers = rowPeer.GetChildren()?.OfType<GridCellAutomationPeer>().ToList();
            Assert.NotNull(peers);

            var nameCell = peers!.First(p => p.GetAutomationId()!.EndsWith("_Name", StringComparison.Ordinal));
            var idCell = peers!.First(p => p.GetAutomationId()!.EndsWith("_Id", StringComparison.Ordinal));

            // Editable: the value writes through the paste conversion path.
            var editable = (IValueProvider)nameCell.GetPattern(PatternInterface.Value)!;
            editable.SetValue("Alicia");
            Assert.Equal("Alicia", employees[0].Name);

            // Read-only: rejected with the UIA contract exception.
            var readOnly = (IValueProvider)idCell.GetPattern(PatternInterface.Value)!;
            Assert.True(readOnly.IsReadOnly);
            Assert.Throws<InvalidOperationException>(() => readOnly.SetValue("99"));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void CellPeer_GridItem_CoordinatesAndContainingGrid()
    {
        var grid = new GridControl { ItemsSource = CreateEmployees() };
        var window = TestWindowHost.Show(grid);
        TestWindowHost.Pump();
        try
        {
            var rowPeer = new GridRowAutomationPeer(DataRowAt(grid, 0));
            var peers = rowPeer.GetChildren()?.OfType<GridCellAutomationPeer>().ToList();
            Assert.NotNull(peers);
            Assert.True(peers!.Count >= 4);

            var nameCell = peers!.First(p => p.GetAutomationId()!.EndsWith("_Name", StringComparison.Ordinal));
            var item = (IGridItemProvider)nameCell.GetPattern(PatternInterface.GridItem)!;

            Assert.Equal(0, item.Row); // the first data row
            var nameIndex = grid.VisibleColumns.Select((c, i) => (c, i)).First(t => t.c.FieldName == "Name").i;
            Assert.Equal(nameIndex, item.Column);
            Assert.Equal(1, item.RowSpan);
            Assert.Equal(1, item.ColumnSpan);

            // In-process (no live UIA session) ProviderFromPeer may legally be null —
            // assert the wiring resolves without throwing; e2e covers the live provider.
            Assert.Null(Record.Exception(() => _ = item.ContainingGrid));
        }
        finally
        {
            window.Close();
        }
    }

}

