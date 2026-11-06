using VCWpfControls.Grid.Columns;

namespace VCWpfControls.Grid;

/// <summary>The unit the user selects with the mouse and keyboard.</summary>
public enum GridSelectionUnit
{
    /// <summary>Whole rows (default).</summary>
    Row = 0,

    /// <summary>Cells and rectangular cell ranges (row selection stays available via the API).</summary>
    CellOrRow = 1,
}

/// <summary>A single cell address: the row's data item plus the column.</summary>
public readonly record struct GridCellInfo(object? Item, GridColumnBase? Column)
{
    /// <summary>Whether both parts of the address are present.</summary>
    public bool IsValid => Item is not null && Column is not null;
}

/// <summary>
/// A rectangular cell range between two cells (any corners). Stored item-based so it
/// survives sorting/filtering; resolved to view coordinates on demand.
/// </summary>
public sealed class GridCellRange
{
    /// <summary>Creates a range between two cells.</summary>
    public GridCellRange(GridCellInfo start, GridCellInfo end)
    {
        Start = start;
        End = end;
    }

    /// <summary>One corner of the range.</summary>
    public GridCellInfo Start { get; }

    /// <summary>The opposite corner.</summary>
    public GridCellInfo End { get; }
}
