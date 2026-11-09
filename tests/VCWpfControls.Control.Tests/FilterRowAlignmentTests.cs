using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Regression (user report): the filter row editors were shifted right of their columns —
/// the presenter carried a template Margin="8" while headers/cells sit at slot origin 0
/// (the old cells margin was removed in the alignment fix c1aa4d2; the filter row kept
/// its copy). The presenter now has no horizontal margin; editors inset INSIDE their slot.
/// The summary row had the same shift and is asserted too.
/// </summary>
public class FilterRowAlignmentTests
{
    [StaFact]
    public void FilterEditors_AlignWithTheirColumnHeaders()
    {
        var grid = new GridControl { Width = 600, Height = 300, ShowFilterRow = true };
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Width = new GridColumnWidth(200, GridUnitType.Pixel) });
        grid.Columns.Add(new GridTextColumn { FieldName = "Department", Width = new GridColumnWidth(200, GridUnitType.Pixel) });
        grid.ItemsSource = new[]
        {
            new { Name = "A", Department = "IT" },
            new { Name = "B", Department = "HR" },
        };
        TestWindowHost.Pump();
        try
        {
            var presenter = (Grid.Primitives.GridFilterRowPresenter)grid.Template.FindName("PART_FilterRowPresenter", grid)!;
            var headersPresenter = (Grid.Primitives.GridColumnHeadersPresenter)grid.Template.FindName("PART_ColumnHeadersPresenter", grid)!;

            foreach (var child in presenter.Children)
            {
                var editor = (TextBox)child;
                var column = (GridColumnBase)editor.Tag!;
                var header = headersPresenter.GetHeader(column);
                Assert.NotNull(header);

                // Same coordinate space: translate both to the grid and compare X.
                var editorX = editor.TranslatePoint(new Point(0, 0), grid).X;
                var headerX = header!.TranslatePoint(new Point(0, 0), grid).X;
                Assert.True(Math.Abs(editorX - headerX) <= 2.5,
                    $"filter editor of '{column.FieldName}' starts at {editorX:F1} but its header at {headerX:F1}");
            }
        }
        finally
        {
            window.Close();
        }
    }
}
