namespace VCWpfControls.Grid;

/// <summary>
/// The data context of a custom group header (<see cref="GridControl.GroupHeaderTemplate"/>):
/// everything the default caption shows, in bindable form.
/// </summary>
public sealed class GridGroupHeaderInfo
{
    /// <summary>Creates the info.</summary>
    public GridGroupHeaderInfo(object? key, int itemCount, int level, string columnName, string caption)
    {
        Key = key;
        ItemCount = itemCount;
        Level = level;
        ColumnName = columnName;
        Caption = caption;
    }

    /// <summary>The group key (the grouped cell value or its converted form).</summary>
    public object? Key { get; }

    /// <summary>How many data items the group contains (all descendants).</summary>
    public int ItemCount { get; }

    /// <summary>The nesting level of the group (0 = outermost).</summary>
    public int Level { get; }

    /// <summary>The grouped column's header text (or the field name).</summary>
    public string ColumnName { get; }

    /// <summary>The default localized caption (useful as a fallback inside a custom template).</summary>
    public string Caption { get; }
}
