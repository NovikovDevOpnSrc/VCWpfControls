using VCWpfControls.Grid.Columns;

namespace VCWpfControls.Grid.Primitives;

/// <summary>One column's computed layout: display position and final width.</summary>
internal sealed class GridLayoutSlot
{
    public GridLayoutSlot(GridColumnBase column, bool isFrozen)
    {
        Column = column;
        IsFrozen = isFrozen;
    }

    /// <summary>The column the slot belongs to.</summary>
    public GridColumnBase Column { get; }

    /// <summary>Whether the column is frozen (stays in place during horizontal scrolling).</summary>
    public bool IsFrozen { get; }

    /// <summary>X position relative to the start of the column layout
    /// (frozen zone first, then the scrollable zone).</summary>
    public double X { get; internal set; }

    /// <summary>Final width in pixels.</summary>
    public double Width { get; internal set; }
}

/// <summary>
/// Pure layout math shared by the header presenter and every row's cells panel:
/// resolves Auto/Absolute/Star widths into concrete slots. Contains no WPF types
/// beyond column definitions, so it is directly unit-testable.
/// </summary>
internal sealed class GridLayoutController
{
    private const double MinStarWidth = 10d;
    private const double AutoPadding = 4d;

    private readonly Dictionary<GridColumnBase, double> _autoWidths = new();
    private readonly Dictionary<GridColumnBase, GridLayoutSlot> _slotsByColumn = new();
    private readonly List<GridLayoutSlot> _slots = new();

    /// <summary>Available width (the grid's viewport) in pixels.</summary>
    public double ViewportWidth { get; set; }

    /// <summary>Number of leading (in display order) frozen columns.</summary>
    public int FrozenColumnCount { get; set; }

    /// <summary>Total width of all slots; the horizontal extent of the content.</summary>
    public double TotalWidth { get; private set; }

    /// <summary>Width occupied by frozen columns.</summary>
    public double FrozenWidth { get; private set; }

    /// <summary>All slots in display order (frozen first).</summary>
    public IReadOnlyList<GridLayoutSlot> Slots => _slots;

    /// <summary>Reports a measured Auto width (header content or cell content); grows only.</summary>
    public void ReportAutoWidth(GridColumnBase column, double desiredWidth)
    {
        var value = desiredWidth + AutoPadding;
        if (!_autoWidths.TryGetValue(column, out var current) || value > current)
        {
            _autoWidths[column] = value;
        }
    }

    /// <summary>Recomputes all slots. Returns <see langword="true"/> when any width changed.</summary>
    public bool Compute(IReadOnlyList<GridColumnBase> visibleColumns)
    {
        if (double.IsNaN(ViewportWidth) || double.IsInfinity(ViewportWidth) || ViewportWidth < 0)
        {
            ViewportWidth = 0;
        }

        var changed = false;
        _slots.Clear();
        _slotsByColumn.Clear();

        var frozenCount = Math.Max(0, Math.Min(FrozenColumnCount, visibleColumns.Count));
        var starWeight = 0d;
        var fixedWidth = 0d;

        // First pass: resolve absolute and auto widths, sum stars.
        var resolved = new List<(GridColumnBase Column, double Width, bool IsStar)>(visibleColumns.Count);
        foreach (var column in visibleColumns)
        {
            var width = column.Width;
            double resolvedWidth;
            var isStar = false;
            if (width.IsAbsolute)
            {
                resolvedWidth = width.Value;
            }
            else if (width.IsAuto)
            {
                resolvedWidth = _autoWidths.TryGetValue(column, out var auto) ? auto : 0d;
            }
            else
            {
                resolvedWidth = 0d;
                isStar = true;
                starWeight += width.Value;
            }

            resolvedWidth = ClampWidth(column, resolvedWidth);
            resolved.Add((column, resolvedWidth, isStar));
            if (!isStar)
            {
                fixedWidth += resolvedWidth;
            }
        }

        var remaining = Math.Max(0, ViewportWidth - fixedWidth);

        // Second pass: distribute the remaining width across stars.
        var starWidth = 0d;
        if (starWeight > 0)
        {
            starWidth = remaining / starWeight;
            if (starWidth < MinStarWidth)
            {
                starWidth = MinStarWidth;
            }
        }

        var x = 0d;
        var index = 0;
        foreach (var (column, width, isStar) in resolved)
        {
            // Stars clamp to the column's own MinWidth (not just the internal floor),
            // so narrowed viewports overflow into the horizontal scrollbar as expected.
            var finalWidth = isStar ? ClampWidth(column, starWidth * column.Width.Value) : width;
            var isFrozen = index < frozenCount;
            var slot = new GridLayoutSlot(column, isFrozen)
            {
                X = x,
                Width = finalWidth,
            };
            _slots.Add(slot);
            _slotsByColumn[column] = slot;

            if (Math.Abs(column.ActualWidth - finalWidth) > 0.01)
            {
                column.ActualWidth = finalWidth;
                changed = true;
            }

            x += finalWidth;
            index++;
        }

        TotalWidth = x;

        // Recompute frozen width from the final slots.
        var frozenWidth = 0d;
        for (var i = 0; i < Math.Min(frozenCount, _slots.Count); i++)
        {
            frozenWidth += _slots[i].Width;
        }

        FrozenWidth = frozenWidth;
        return changed;
    }

    /// <summary>Gets the slot of a column, if currently computed.</summary>
    public GridLayoutSlot? GetSlot(GridColumnBase column)
        => _slotsByColumn.TryGetValue(column, out var slot) ? slot : null;

    /// <summary>Finds the slot containing the given x position (already adjusted for the horizontal offset).</summary>
    public GridLayoutSlot? FindSlotAt(double x)
    {
        foreach (var slot in _slots)
        {
            if (x >= slot.X && x < slot.X + slot.Width)
            {
                return slot;
            }
        }

        return _slots.Count > 0 ? _slots[^1] : null;
    }

    private static double ClampWidth(GridColumnBase column, double width)
    {
        if (width < column.MinWidth)
        {
            width = column.MinWidth;
        }

        if (width > column.MaxWidth)
        {
            width = column.MaxWidth;
        }

        return width;
    }
}
