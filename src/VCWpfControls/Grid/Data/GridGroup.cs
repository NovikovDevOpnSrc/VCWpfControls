using System.Collections;

namespace VCWpfControls.Grid.Data;

/// <summary>Describes one level of grouping by a property path.</summary>
public sealed class GridGroupDescription
{
    private string? _headerStringFormat;

    /// <summary>Creates a group description.</summary>
    public GridGroupDescription(string fieldName)
    {
        FieldName = fieldName;
    }

    /// <summary>Property path to group by.</summary>
    public string FieldName { get; set; }

    /// <summary>
    /// Optional group header format: <c>{0}</c> = key, <c>{1}</c> = item count,
    /// <c>{2}</c> = column header (or the field name). Defaults to the localized
    /// "key: N item(s)".
    /// </summary>
    public string? HeaderStringFormat
    {
        get => _headerStringFormat;
        set => _headerStringFormat = string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>
    /// Optional converter producing the group key from the cell value
    /// (e.g. bucketing dates by month). When <see langword="null"/>, the raw cell value is the key.
    /// </summary>
    public Func<object?, object?>? KeyConverter { get; set; }

    /// <summary>Optional comparer for ordering group keys; <see langword="null"/> uses default value comparison.</summary>
    public IComparer? KeysComparer { get; set; }
}

/// <summary>A node of the group tree produced by the data engine.</summary>
public sealed class GridGroup
{
    private readonly List<GridGroup> _childGroups = new();
    private readonly List<object> _items = new();

    internal GridGroup(object? key, int level, GridGroup? parent)
    {
        Key = key;
        Level = level;
        Parent = parent;
    }

    /// <summary>Group key (the cell value, possibly transformed by <see cref="GridGroupDescription.KeyConverter"/>).</summary>
    public object? Key { get; }

    /// <summary>Nesting level; 0 for top-level groups.</summary>
    public int Level { get; }

    /// <summary>Parent group, or <see langword="null"/> for top-level groups.</summary>
    public GridGroup? Parent { get; }

    /// <summary>Child groups of the next grouping level.</summary>
    public IReadOnlyList<GridGroup> ChildGroups => _childGroups;

    /// <summary>Data items of this group that fall in this level (only filled for the deepest level).</summary>
    public IReadOnlyList<object> Items => _items;

    /// <summary>Total number of data items under this group (including sub-groups).</summary>
    public int TotalItems { get; internal set; }

    /// <summary>Whether the group is expanded in the UI. Persisted across data refreshes.</summary>
    public bool IsExpanded { get; set; } = true;

    internal List<GridGroup> ChildGroupsInternal => _childGroups;

    internal List<object> ItemsInternal => _items;
}
