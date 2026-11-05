using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VCWpfControls.Grid.Columns;

namespace VCWpfControls.Grid.Primitives;

/// <summary>
/// Hosts one <see cref="GridColumnHeader"/> per visible column and arranges them exactly like
/// <see cref="GridCellsPanel"/> arranges cells, so headers stay aligned with the data columns
/// (including frozen columns and horizontal scrolling).
/// </summary>
public sealed class GridColumnHeadersPresenter : Panel
{
    private readonly Dictionary<GridColumnBase, GridColumnHeader> _headersByColumn = new();

    /// <summary>The owning grid, attached when the control template is applied.</summary>
    internal GridControl? Owner { get; set; }

    /// <summary>Rebuilds header children so that one exists per visible column.</summary>
    internal void EnsureHeaders()
    {
        if (Owner is null)
        {
            return;
        }

        Children.Clear();
        foreach (var column in Owner.VisibleColumns)
        {
            if (!_headersByColumn.TryGetValue(column, out var header))
            {
                header = new GridColumnHeader { Column = column };
                _headersByColumn[column] = header;
            }

            header.SortDirection = column.SortDirection;
            header.ShowFilterButton = column.CanUserFilter;
            Children.Add(header);
        }

        InvalidateMeasure();
    }

    /// <summary>Drops cached headers for columns that are no longer visible.</summary>
    internal void PruneHiddenHeaders()
    {
        if (Owner is null)
        {
            return;
        }

        foreach (var (column, header) in _headersByColumn.ToArray())
        {
            if (!column.Visible || column.Owner != Owner)
            {
                _headersByColumn.Remove(column);
                Children.Remove(header);
            }
        }
    }

    /// <summary>Measures headers with final column widths; feeds auto-width hints first.</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Owner is null)
        {
            return default;
        }

        var layout = Owner.Layout;
        var columns = Owner.VisibleColumns;

        // Auto columns: measure header content unconstrained to seed the auto width.
        foreach (var column in columns)
        {
            if (column.Width.IsAuto && _headersByColumn.TryGetValue(column, out var header))
            {
                header.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                layout.ReportAutoWidth(column, header.DesiredSize.Width);
            }
        }

        var height = 0d;
        for (var i = 0; i < columns.Count && i < Children.Count; i++)
        {
            var child = Children[i];
            var column = columns[i];
            var slot = layout.GetSlot(column);
            var width = slot?.Width ?? column.ActualWidth;
            child.Measure(new Size(width, availableSize.Height));
            height = Math.Max(height, child.DesiredSize.Height);

            if (child is GridColumnHeader header)
            {
                header.IsFrozen = slot?.IsFrozen ?? false;
            }
        }

        var finalHeight = double.IsNaN(Owner.HeaderHeight) ? height : Owner.HeaderHeight;
        return new Size(availableSize.Width, finalHeight);
    }

    /// <summary>Arranges headers at their slots; frozen headers ignore the horizontal offset.</summary>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Owner is null)
        {
            return finalSize;
        }

        var layout = Owner.Layout;
        var columns = Owner.VisibleColumns;
        var offset = Owner.HorizontalOffset;
        var frozenBoundary = 0d;
        for (var i = 0; i < columns.Count; i++)
        {
            if (layout.GetSlot(columns[i]) is { IsFrozen: true } slot)
            {
                frozenBoundary = Math.Max(frozenBoundary, slot.X + slot.Width);
            }
        }

        for (var i = 0; i < columns.Count && i < Children.Count; i++)
        {
            var child = Children[i];
            var column = columns[i];
            var slot = layout.GetSlot(column);
            if (slot is null)
            {
                child.Arrange(new Rect(0, 0, 0, 0));
                continue;
            }

            var x = slot.IsFrozen ? slot.X : slot.X - offset;
            child.Arrange(new Rect(x, 0, slot.Width, finalSize.Height));

            // Scrollable headers must not slide under the frozen headers, matching the
            // cells panel clipping (GridCellsPanel.GetScrollClip).
            if (!slot.IsFrozen && frozenBoundary > 0)
            {
                var clipLeft = Math.Max(frozenBoundary - x, 0);
                var clipWidth = slot.Width - clipLeft;
                child.Clip = clipWidth <= 0
                    ? new RectangleGeometry(new Rect(0, 0, 0, 0))
                    : new RectangleGeometry(new Rect(clipLeft, 0, clipWidth, finalSize.Height));
            }
            else
            {
                child.Clip = null;
            }
        }

        return finalSize;
    }

    /// <summary>Returns the header of the given column, if materialized.</summary>
    internal GridColumnHeader? GetHeader(GridColumnBase column)
        => _headersByColumn.TryGetValue(column, out var header) ? header : null;
}
