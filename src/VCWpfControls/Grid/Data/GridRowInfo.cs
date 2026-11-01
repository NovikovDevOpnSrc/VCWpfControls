namespace VCWpfControls.Grid.Data;

/// <summary>Kind of a row in the grid's flattened view.</summary>
public enum GridRowKind
{
    /// <summary>A data row bound to an item of <c>ItemsSource</c>.</summary>
    Data = 0,

    /// <summary>A group header row.</summary>
    Group = 1,
}

/// <summary>Base class of the rows exposed by the grid's internal view.</summary>
public abstract class GridRowInfo
{
    /// <summary>Kind of the row.</summary>
    public abstract GridRowKind Kind { get; }

    /// <summary>Index of the row in the flattened view (data and group rows).</summary>
    public int FlatIndex { get; internal set; }
}

/// <summary>A data row bound to an item of <c>ItemsSource</c>.</summary>
public sealed class GridDataRow : GridRowInfo
{
    /// <summary>Creates a data row.</summary>
    public GridDataRow(object item) => Item = item;

    /// <summary>Kind of the row.</summary>
    public override GridRowKind Kind => GridRowKind.Data;

    /// <summary>The bound data item.</summary>
    public object Item { get; }
}

/// <summary>A group header row in the flattened view.</summary>
public sealed class GridGroupRow : GridRowInfo
{
    /// <summary>Creates a group row.</summary>
    public GridGroupRow(GridGroup group) => Group = group;

    /// <summary>Kind of the row.</summary>
    public override GridRowKind Kind => GridRowKind.Group;

    /// <summary>The group this row represents.</summary>
    public GridGroup Group { get; }

    /// <summary>Nesting level of the group (0 for top-level).</summary>
    public int Level => Group.Level;
}
