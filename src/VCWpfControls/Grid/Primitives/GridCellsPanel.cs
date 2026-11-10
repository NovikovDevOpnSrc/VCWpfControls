using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using VCWpfControls.Grid.Columns;

namespace VCWpfControls.Grid.Primitives;

/// <summary>
/// The panel inside a <see cref="GridRow"/> that hosts one cell element per visible column.
/// Arranges cells according to the owning grid's layout controller (frozen columns stay in place,
/// scrollable columns are shifted by the grid's horizontal offset) and swaps display elements
/// for editing elements while a cell is being edited.
/// </summary>
public sealed class GridCellsPanel : Panel
{
    private static readonly DependencyProperty ColumnProperty = DependencyProperty.RegisterAttached(
        "Column", typeof(GridColumnBase), typeof(GridCellsPanel), new PropertyMetadata(null));

    private readonly Dictionary<GridColumnBase, FrameworkElement> _elementsByColumn = new();
    private readonly Border _focusVisual = new();

    private GridControl? _owner;
    private GridRow? _row;
    private GridColumnBase? _editingColumn;
    private FrameworkElement? _editingElement;
    private FrameworkElement? _hiddenDisplayElement;

    static GridCellsPanel()
    {
        ClipToBoundsProperty.OverrideMetadata(typeof(GridCellsPanel), new PropertyMetadata(true));
    }

    internal GridRow Row => _row!;

    /// <summary>Attaches the panel to its row and owning grid.</summary>
    internal void Attach(GridRow row, GridControl owner)
    {
        _row = row;
        _owner = owner;
    }

    /// <summary>Builds or reuses cell elements so that one exists per visible column.</summary>
    internal void EnsureCells()
    {
        if (_owner is null || _row?.DataItem is null)
        {
            return;
        }

        var columns = _owner.VisibleColumns;
        Children.Clear();

        foreach (var column in columns)
        {
            if (column == _editingColumn && _editingElement is not null)
            {
                Children.Add(_editingElement);
                continue;
            }

            if (!_elementsByColumn.TryGetValue(column, out var element))
            {
                element = column.GenerateElement();
                _elementsByColumn[column] = element;
                element.SetValue(ColumnProperty, column);
            }

            Children.Add(element);
        }

        _focusVisual.BorderThickness = new Thickness(1);
        _focusVisual.IsHitTestVisible = false;
        _focusVisual.BorderBrush = Brushes.Transparent;
        Children.Add(_focusVisual);
    }

    /// <summary>Rebinds all cell elements to the row's data item (also used on container recycling).</summary>
    internal void PrepareCells()
    {
        if (_owner is null || _row?.DataItem is null)
        {
            return;
        }

        var columns = _owner.VisibleColumns;
        foreach (var column in columns)
        {
            if (_elementsByColumn.TryGetValue(column, out var element))
            {
                if (column == _editingColumn && _editingElement is not null)
                {
                    column.PrepareEditingElement(_editingElement, _row.DataItem);
                }
                else
                {
                    column.PrepareElement(element, _row.DataItem);
                }
            }
        }

        UpdateFocusVisual();
    }

    /// <summary>Drops per-item state when the row container is cleared.</summary>
    internal void ClearCells()
    {
        if (_owner is null)
        {
            return;
        }

        foreach (var pair in _elementsByColumn)
        {
            pair.Key.ResetElement(pair.Value);
        }

        _elementsByColumn.Clear();
        Children.Clear();
        Children.Add(_focusVisual);
    }

    /// <summary>Applies (or clears, when null) the column's <see cref="GridColumnBase.ElementStyle"/> on its cell element.</summary>
    internal void ApplyElementStyle(GridColumnBase column)
    {
        if (_elementsByColumn.TryGetValue(column, out var element))
        {
            element.Style = column.ElementStyle;
        }
    }

    /// <summary>The generated cell element of the given column, if it has been created.</summary>
    internal FrameworkElement? GetCellElement(GridColumnBase column)
        => _elementsByColumn.TryGetValue(column, out var element) ? element : null;

    /// <summary>The current-cell focus border (the last child of the panel).</summary>
    internal Border FocusVisual => _focusVisual;

    /// <summary>Replaces the display element of a column with its editing element.</summary>
    internal void BeginCellEdit(GridColumnBase column)
    {
        if (_owner is null || _row?.DataItem is null)
        {
            return;
        }

        EndCellEdit(commit: false);

        var editingElement = column.GenerateEditingElement();
        if (editingElement is null)
        {
            return;
        }

        if (_elementsByColumn.TryGetValue(column, out var displayElement))
        {
            _hiddenDisplayElement = displayElement;
            var index = Children.IndexOf(displayElement);
            Children.RemoveAt(index);
            Children.Insert(index, editingElement);
        }
        else
        {
            Children.Add(editingElement);
        }

        column.PrepareEditingElement(editingElement, _row.DataItem);
        column.RebindEditingElement(editingElement, _row.DataItem);

        _editingColumn = column;
        _editingElement = editingElement;
        editingElement.SetValue(ColumnProperty, column);
        editingElement.Loaded += OnEditingElementLoaded;
    }

    /// <summary>Restores the display element of the edited cell.</summary>
    internal void EndCellEdit(bool commit)
    {
        if (_editingColumn is null || _editingElement is null)
        {
            return;
        }

        var column = _editingColumn;
        var editingElement = _editingElement;
        editingElement.Loaded -= OnEditingElementLoaded;

        _editingColumn = null;
        _editingElement = null;

        var index = Children.IndexOf(editingElement);
        if (index >= 0)
        {
            Children.RemoveAt(index);
        }

        if (_hiddenDisplayElement is not null)
        {
            Children.Insert(Math.Min(index < 0 ? Children.Count : index, Children.Count), _hiddenDisplayElement);
            _hiddenDisplayElement = null;
        }

        if (column.GetEditingCommitProperty() is { } property)
        {
            // Detach the editing binding so recycled elements don't push stale values.
            BindingOperations.ClearBinding(editingElement, property);
        }

        InvalidateMeasure();
    }

    /// <summary>The currently edited column, if any.</summary>
    internal GridColumnBase? EditingColumn => _editingColumn;

    /// <summary>The currently visible editing element, if any.</summary>
    internal FrameworkElement? EditingElement => _editingElement;

    /// <summary>Returns the column whose cell is located at the given x position (content coordinates).</summary>
    internal GridColumnBase? GetColumnAt(double x)
    {
        var slot = _owner?.Layout.FindSlotAt(x);
        return slot?.Column;
    }

    /// <summary>Measures each cell with the slot width and reports auto-width hints.</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (_owner is null || _row?.DataItem is null)
        {
            return default;
        }

        double height = 0;
        var layout = _owner.Layout;
        var columns = _owner.VisibleColumns;

        for (var i = 0; i < columns.Count && i < Children.Count; i++)
        {
            var child = Children[i];
            var column = columns[i];
            var slot = layout.GetSlot(column);
            var width = slot?.Width ?? column.ActualWidth;

            child.Measure(new Size(width, availableSize.Height));
            height = Math.Max(height, child.DesiredSize.Height);

            if (column.Width.IsAuto)
            {
                layout.ReportAutoWidth(column, child.DesiredSize.Width);
            }
        }

        _focusVisual.Measure(new Size(availableSize.Width, height));

        var finalHeight = double.IsNaN(_owner.RowHeight) ? height : _owner.RowHeight;
        return new Size(availableSize.Width, finalHeight);
    }

    /// <summary>Arranges cells at their slots; frozen cells ignore the horizontal offset.</summary>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_owner is null)
        {
            return finalSize;
        }

        var layout = _owner.Layout;
        var columns = _owner.VisibleColumns;
        var offset = _owner.HorizontalOffset;
        var frozenBoundary = GetFrozenBoundary(columns, layout);

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
            if (i == 0 && _row is { } indentRow && _owner is TreeList.TreeListView)
            {
                // TreeListView: indent the first column's content past the expander toggle
                // (level*16 + 16 + gap) and the node check box (+16 + gap when shown) —
                // the text used to render straight under the toggle/check box.
                var reserve = 16 + 4 + (indentRow.HasNodeCheckBox ? 16 + 4 : 0);
                var indent = (indentRow.TreeLevel * 16) + reserve;
                x += Math.Min(indent, Math.Max(0, slot.Width - 8));
            }

            child.Arrange(new Rect(x, 0, slot.Width, finalSize.Height));
            if (i == 0 && _row is { TreeLevel: > 0 })
            {
                // TreeListView: the indented first column deliberately overflows its slot
                // by the indent — keep the frozen-boundary-only clip for it.
                child.Clip = GetFrozenBoundaryClip(slot, x, frozenBoundary, finalSize.Height);
            }
            else
            {
                // Every cell is hard-clipped to its own slot: no cell content can ever
                // render into the neighboring column, whatever its template measures.
                var clipLeft = Math.Max(slot.IsFrozen ? 0 : frozenBoundary - x, 0);
                var clipWidth = slot.Width - clipLeft;
                child.Clip = clipWidth <= 0
                    ? new RectangleGeometry(new Rect(0, 0, 0, 0))
                    : new RectangleGeometry(new Rect(clipLeft, 0, clipWidth, finalSize.Height));
            }
        }

        UpdateFocusVisual();

        var height = double.IsNaN(_owner.RowHeight) ? finalSize.Height : _owner.RowHeight;
        return new Size(finalSize.Width, height);
    }

    /// <summary>
    /// The right edge of the last frozen slot in panel coordinates (0 when nothing is frozen):
    /// scrollable content must not render left of it or it would slide under the frozen cells.
    /// </summary>
    private static double GetFrozenBoundary(IReadOnlyList<GridColumnBase> columns, GridLayoutController layout)
    {
        var boundary = 0d;
        for (var i = 0; i < columns.Count; i++)
        {
            if (layout.GetSlot(columns[i]) is { IsFrozen: true } slot)
            {
                boundary = Math.Max(boundary, slot.X + slot.Width);
            }
        }

        return boundary;
    }

    /// <summary>
    /// Clips a scrollable cell to the part of its slot right of the frozen boundary;
    /// null when the whole slot is visible (nothing frozen to its left).
    /// </summary>
    private static RectangleGeometry? GetFrozenBoundaryClip(GridLayoutSlot slot, double x, double frozenBoundary, double height)
    {
        if (slot.IsFrozen)
        {
            return null;
        }

        var clipLeft = Math.Max(frozenBoundary - x, 0);
        var clipWidth = slot.Width - clipLeft;
        if (clipLeft <= 0 && clipWidth >= slot.Width)
        {
            return null; // fully visible, nothing frozen to the left
        }

        return clipWidth <= 0
            ? new RectangleGeometry(new Rect(0, 0, 0, 0))
            : new RectangleGeometry(new Rect(clipLeft, 0, clipWidth, height));
    }

    private void UpdateFocusVisual()
    {
        if (_owner is null || _row is null)
        {
            return;
        }

        var isCurrentRow = ReferenceEquals(_row.DataItem, _owner.CurrentItem);
        var currentColumn = isCurrentRow ? _owner.CurrentColumn : null;
        if (currentColumn is null)
        {
            _focusVisual.Arrange(new Rect(0, 0, 0, 0));
            _focusVisual.Visibility = Visibility.Collapsed;
            return;
        }

        var slot = _owner.Layout.GetSlot(currentColumn);
        if (slot is null)
        {
            _focusVisual.Visibility = Visibility.Collapsed;
            return;
        }

        _focusVisual.Visibility = Visibility.Visible;
        var offset = slot.IsFrozen ? 0 : -_owner.HorizontalOffset;
        _focusVisual.Arrange(new Rect(slot.X + offset, 0, slot.Width, RenderSize.Height == 0 ? DesiredSize.Height : RenderSize.Height));
        _focusVisual.BorderBrush = _owner.CurrentCellBorderBrush;
        _focusVisual.Clip = GetFrozenBoundaryClip(slot, slot.X + offset, GetFrozenBoundary(_owner.VisibleColumns, _owner.Layout), RenderSize.Height == 0 ? DesiredSize.Height : RenderSize.Height);
    }

    /// <summary>Draws the selected-cell background spans, tree summaries and the column separators.</summary>
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        if (_owner is not { } owner)
        {
            return;
        }

        // TreeListView node summaries: right-aligned at the row's right edge, above the
        // background but below the cell content (OnRender runs before children).
        if (owner is TreeList.TreeListView tree
            && tree.TreeSummaryItems.Count > 0
            && _row?.DataItem is { } nodeItem
            && tree.GetTreeSummaryText(nodeItem) is { Length: > 0 } summaryText)
        {
            var typeface = new Typeface(owner.FontFamily, FontStyles.Italic, FontWeights.Normal, FontStretches.Normal);
            var text = new FormattedText(
                summaryText,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                Math.Max(owner.FontSize * 0.92, 9),
                (owner.TryFindResource(VCWpfControls.Grid.GridResources.TextBrushKey) as Brush) ?? Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            var x = Math.Max(RenderSize.Width - text.Width - 6, 0);
            dc.DrawText(text, new System.Windows.Point(x, (RenderSize.Height - text.Height) / 2));
        }

        // Selected cells: painted before the children so text stays on top. View-row spans
        // map to panel x through the layout slots (frozen absolute, scrollable shifted).
        if (owner.SelectionUnit == GridSelectionUnit.CellOrRow
            && _row?.RowInfo is { FlatIndex: var flatIndex }
            && owner.TryFindResource(GridResources.SelectionBackgroundKey) is System.Windows.Media.Brush selectionBrush)
        {
            var layoutSel = owner.Layout;
            var columnsSel = owner.VisibleColumns;
            foreach (var (colStart, colEnd) in owner.GetSelectedCellSpans(flatIndex))
            {
                double x0 = 0, x1 = 0;
                var haveStart = false;
                var haveEnd = false;
                for (var i = colStart; i <= colEnd && i < columnsSel.Count; i++)
                {
                    if (layoutSel.GetSlot(columnsSel[i]) is not { } slot)
                    {
                        continue;
                    }

                    var sx = slot.IsFrozen ? slot.X : slot.X - owner.HorizontalOffset;
                    if (!haveStart || sx < x0)
                    {
                        x0 = sx;
                        haveStart = true;
                    }

                    var ex = sx + slot.Width;
                    if (!haveEnd || ex > x1)
                    {
                        x1 = ex;
                        haveEnd = true;
                    }
                }

                if (haveStart)
                {
                    dc.DrawRectangle(selectionBrush, null, new System.Windows.Rect(x0, 0, x1 - x0, RenderSize.Height));
                }
            }
        }

        if (!owner.ShowColumnSeparators
            || (owner.ColumnSeparatorBrush ?? TryFindResource(GridResources.SeparatorLineKey) as Brush ?? owner.GridLinesBrush) is not { } brush)
        {
            return;
        }

        var layout = owner.Layout;
        var columns = owner.VisibleColumns;
        var offset = owner.HorizontalOffset;
        var pen = new Pen(brush, 1);
        var width = RenderSize.Width;

        // The cells panel is inset within the row (template Margin) — shift the lines back
        // so they align with the header borders; the 0.5 offset centers the 1px pen in the
        // same pixel column the header's right border occupies (border is inside the slot).
        var inset = Margin.Left + Margin.Right > 0 ? Margin.Left : 0;
        for (var i = 0; i < columns.Count; i++)
        {
            var slot = layout.GetSlot(columns[i]);
            if (slot is null)
            {
                continue;
            }

            var boundary = (slot.IsFrozen ? slot.X : slot.X - offset) + slot.Width;
            var x = boundary - inset - 0.5;
            // Skip separators outside the viewport; the last column edge is clipped anyway.
            if (x <= 0 || x >= width)
            {
                continue;
            }

            dc.DrawLine(pen, new Point(x, 0), new Point(x, RenderSize.Height));
        }
    }

    private void OnEditingElementLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            element.Focus();
            if (element is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }
    }
}
