using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Regression (user report: Column types → Department looked EMPTY when editing began).
/// Root cause: the demo bound the column's ItemsSource through a BindingProxy with a path
/// missing the <c>Data.</c> prefix — the path resolved against the PROXY (which has no
/// Departments property), the column got null, and the editor rendered empty. The
/// grid-editing flow through a proxy resource had no coverage: the earlier proxy round
/// validated the mechanism with a correct path, and FlaUI cannot see the combo before
/// edit mode.
/// </summary>
public class ComboEditingProxyTests
{
    private enum Department { It, Hr, Sales, Marketing }

    private sealed class Row
    {
        public Department Department { get; set; } = Department.Hr;
    }

    private sealed class PageVm
    {
        public Department[] Departments { get; } = Enum.GetValues<Department>();
    }

    private sealed class BindingProxy : Freezable
    {
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
            nameof(Data), typeof(object), typeof(BindingProxy), new PropertyMetadata(null));

        public object? Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        protected override Freezable CreateInstanceCore() => new BindingProxy();
    }

    [StaFact]
    public void ComboEditor_ShowsCurrentValue_WhenItemsArriveThroughProxy()
    {
        var grid = new GridControl { Width = 500, Height = 200 };
        var column = new GridComboBoxColumn
        {
            FieldName = "Department",
            Width = new GridColumnWidth(160, GridUnitType.Pixel),
        };
        grid.Columns.Add(column);
        grid.ItemsSource = new[] { new Row(), new Row() };

        // The live page's shape: a proxy resource carries the page DataContext to the
        // column (outside the visual tree), and the column binds THROUGH the proxy's Data.
        var proxy = new BindingProxy { Data = new PageVm() };
        grid.Resources["PageProxy"] = proxy;
        var binding = new System.Windows.Data.Binding($"{nameof(BindingProxy.Data)}.{nameof(PageVm.Departments)}")
        {
            Source = proxy,
        };
        System.Windows.Data.BindingOperations.SetBinding(column, GridComboBoxColumn.ItemsSourceProperty, binding);

        var window = TestWindowHost.Show(grid);
        TestWindowHost.Pump();
        try
        {
            var item = ((Grid.Data.GridDataRow)grid.DataEngine.View[0]).Item;
            grid.SetCurrentCellForTest(item, column);
            TestWindowHost.Pump();
            Assert.True(grid.BeginEdit(item, column));
            TestWindowHost.Pump();

            ComboBox? editor = null;
            Walk(grid, c => editor ??= c);
            Assert.NotNull(editor);
            Assert.Equal(4, editor!.Items.Count); // the proxy delivered the list
            Assert.Equal(Department.Hr, editor.SelectedItem);

            var text = FindText(editor);
            Assert.Equal("Hr", text); // the editor SHOWS the current value when editing begins
        }
        finally
        {
            window.Close();
        }
    }

    private static string? FindText(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is TextBlock tb && !string.IsNullOrEmpty(tb.Text))
            {
                return tb.Text;
            }

            var deeper = FindText(child);
            if (deeper is not null)
            {
                return deeper;
            }
        }

        return null;
    }

    private static void Walk(DependencyObject root, Action<ComboBox> visit)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ComboBox combo)
            {
                visit(combo);
            }

            Walk(child, visit);
        }
    }
}
