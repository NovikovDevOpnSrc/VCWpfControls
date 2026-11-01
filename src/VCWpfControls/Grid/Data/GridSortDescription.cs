using System.Collections;
using System.Globalization;

namespace VCWpfControls.Grid.Data;

/// <summary>Sort direction of a single sort level.</summary>
public enum GridSortDirection
{
    /// <summary>Not sorted by this level.</summary>
    None = 0,

    /// <summary>Ascending order.</summary>
    Ascending = 1,

    /// <summary>Descending order.</summary>
    Descending = 2,
}

/// <summary>
/// Describes one level of sorting: a property path, a direction and an optional custom comparer.
/// </summary>
public sealed class GridSortDescription
{
    /// <summary>Creates a sort description.</summary>
    public GridSortDescription(string fieldName, GridSortDirection direction = GridSortDirection.Ascending)
    {
        FieldName = fieldName;
        Direction = direction;
    }

    /// <summary>Property path to sort by, e.g. <c>"Name"</c> or <c>"Address.City"</c>.</summary>
    public string FieldName { get; set; }

    /// <summary>Sort direction.</summary>
    public GridSortDirection Direction { get; set; } = GridSortDirection.Ascending;

    /// <summary>
    /// Optional custom comparer for cell values of this level. When set, culture-aware default
    /// comparison is replaced (direction is still applied by the engine).
    /// </summary>
    public IComparer? Comparer { get; set; }

    /// <summary>Culture used for string comparison; defaults to <see cref="CultureInfo.CurrentCulture"/>.</summary>
    public CultureInfo? Culture { get; set; }
}
