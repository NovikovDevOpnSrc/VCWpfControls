using System.Collections.ObjectModel;
using System.Windows;
using VCWpfControls.Grid.Data;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The add-row placeholder (<c>CanUserAddRows</c>): a sentinel row at the end of the view;
/// editing it creates a real item; Esc on the first edit removes it again.
/// </summary>
public class AddRowTests
{
    private static (GridControl Grid, ObservableCollection<Employee> Source, Window Window) Create(bool canAdd)
    {
        var source = new ObservableCollection<Employee>(CreateEmployees(3));
        var grid = new GridControl { Width = 400, RowHeight = 24, HeaderHeight = 26, CanUserAddRows = canAdd };
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "Name", Width = 150 });
        grid.Columns.Add(new GridTextColumn { FieldName = "Department", Header = "Dept", Width = 150 });
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = source;
        TestWindowHost.Pump();
        return (grid, source, window);
    }

    [StaFact]
    public void OffByDefault_NoPlaceholder()
    {
        var (grid, _, window) = Create(canAdd: false);
        try
        {
            Assert.Equal(3, grid.Items.Count);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Enabled_PlaceholderIsLastRow_AndRendersEmpty()
    {
        var (grid, _, window) = Create(canAdd: true);
        try
        {
            Assert.Equal(4, grid.Items.Count);
            Assert.IsType<GridDataRow>(grid.Items[^1]!);
            Assert.Same(GridControl.NewItemPlaceholder, ((GridDataRow)grid.Items[^1]!).Item);

            // The placeholder row realizes an (empty) container without exceptions.
            var row = grid.ItemContainerGenerator.ContainerFromIndex(3);
            Assert.NotNull(row);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void BeginEditOnPlaceholder_AddsItem_AndBeginsEditingIt()
    {
        var (grid, source, window) = Create(canAdd: true);
        try
        {
            object? supplied = null;
            grid.AddingNewItem += (_, e) => e.NewItem = supplied ??= new Employee { Name = "new", Department = "d" };

            Assert.True(grid.BeginEdit(GridControl.NewItemPlaceholder, grid.Columns[0]));
            TestWindowHost.Pump();

            Assert.Equal(4, source.Count); // added to the source
            Assert.Contains(source, e => e.Name == "new");
            Assert.True(grid.IsEditing);
            Assert.Same(supplied, grid.CurrentItem);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void CancelOnFirstEdit_RemovesTheNewItem()
    {
        var (grid, source, window) = Create(canAdd: true);
        try
        {
            grid.BeginEdit(GridControl.NewItemPlaceholder, grid.Columns[0]);
            TestWindowHost.Pump();
            Assert.Equal(4, source.Count);

            grid.CancelEdit();
            TestWindowHost.Pump();
            Assert.Equal(3, source.Count); // rolled back
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Commit_KeepsTheNewItem()
    {
        var (grid, source, window) = Create(canAdd: true);
        try
        {
            grid.BeginEdit(GridControl.NewItemPlaceholder, grid.Columns[0]);
            TestWindowHost.Pump();
            Assert.Equal(4, source.Count);

            Assert.True(grid.CommitEdit());
            TestWindowHost.Pump();
            Assert.Equal(4, source.Count); // stays after commit

            // A second cancel must NOT remove it anymore.
            grid.BeginEdit();
            grid.CancelEdit();
            TestWindowHost.Pump();
            Assert.Equal(4, source.Count);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Factory_IsUsedBeforeEvent()
    {
        var (grid, source, window) = Create(canAdd: true);
        grid.NewItemFactory = () => new Employee { Name = "from-factory" };
        try
        {
            Assert.True(grid.BeginEdit(GridControl.NewItemPlaceholder, grid.Columns[0]));
            TestWindowHost.Pump();
            Assert.Contains(source, e => e.Name == "from-factory");
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void AddingNewItem_Cancel_BlocksCreation()
    {
        var (grid, source, window) = Create(canAdd: true);
        try
        {
            grid.AddingNewItem += (_, e) => e.Cancel = true;
            Assert.False(grid.BeginEdit(GridControl.NewItemPlaceholder, grid.Columns[0]));
            TestWindowHost.Pump();
            Assert.Equal(3, source.Count);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ReadOnlyGrid_NoPlaceholder()
    {
        var source = CreateEmployees(2);
        var grid = new GridControl { Width = 400, RowHeight = 24, CanUserAddRows = true, IsReadOnly = true };
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "Name", Width = 150 });
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = source;
        TestWindowHost.Pump();
        try
        {
            Assert.Equal(2, grid.Items.Count);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void SourceAdd_KeepsPlaceholderLast()
    {
        var (grid, source, window) = Create(canAdd: true);
        try
        {
            source.Add(new Employee { Name = "late", Department = "x" });
            TestWindowHost.Pump();

            Assert.Equal(5, grid.Items.Count);
            Assert.Same(GridControl.NewItemPlaceholder, ((GridDataRow)grid.Items[^1]!).Item);
        }
        finally
        {
            window.Close();
        }
    }
}
