using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;

namespace VCWpfControls.Grid.Data;

/// <summary>
/// The UI-free data engine of <c>GridControl</c>: takes the source collection and applies
/// filtering, sorting and grouping, producing a flattened list of <see cref="GridRowInfo"/> rows
/// (data rows interleaved with group rows). Fully unit-testable without WPF.
/// </summary>
public sealed class GridDataManager
{
    private readonly ConcurrentDictionary<KeyValuePair<Type, string>, PropertyAccessor> _accessorCache = new();
    private readonly Dictionary<GridGroupKeyPath, bool> _expandState = new();

    private IEnumerable? _source;
    private INotifyCollectionChanged? _notifier;
    private List<GridRowInfo> _view = new();
    private List<GridDataRow> _dataRows = new();

    /// <summary>Raised after the view has been rebuilt; the control re-synchronizes its items.</summary>
    public event EventHandler? ViewReset;

    /// <summary>The bound source collection, or <see langword="null"/>.</summary>
    public IEnumerable? Source => _source;

    /// <summary>Active sort levels, outermost first.</summary>
    public IReadOnlyList<GridSortDescription> Sorts { get; set; } = Array.Empty<GridSortDescription>();

    /// <summary>Active filters, combined with a logical AND.</summary>
    public IReadOnlyList<GridFilterDescription> Filters { get; set; } = Array.Empty<GridFilterDescription>();

    /// <summary>Active grouping levels, outermost first.</summary>
    public IReadOnlyList<GridGroupDescription> Groups { get; set; } = Array.Empty<GridGroupDescription>();

    /// <summary>Culture used for string comparisons; defaults to <see cref="CultureInfo.CurrentCulture"/>.</summary>
    public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;

    /// <summary>Explicit item type used to resolve property paths before data arrives; otherwise inferred.</summary>
    public Type? ItemType { get; set; }

    /// <summary>The inferred (or explicit) common item type, or <see langword="null"/>.</summary>
    public Type? ResolvedItemType { get; private set; }

    /// <summary>The flattened view: data rows and group rows in display order.</summary>
    public IReadOnlyList<GridRowInfo> View => _view;

    /// <summary>Data rows of the view in display order (without group rows).</summary>
    public IReadOnlyList<GridDataRow> DataRows => _dataRows;

    /// <summary>Connects a new source collection and rebuilds the view.</summary>
    public void SetSource(IEnumerable? value)
    {
        if (ReferenceEquals(_source, value))
        {
            return;
        }

        if (_notifier is not null)
        {
            _notifier.CollectionChanged -= OnCollectionChanged;
        }

        _source = value;
        _notifier = value as INotifyCollectionChanged;
        if (_notifier is not null)
        {
            _notifier.CollectionChanged += OnCollectionChanged;
        }

        Refresh();
    }

    /// <summary>Rebuilds the whole pipeline (filter → sort → group → flatten).</summary>
    public void Refresh()
    {
        var all = MaterializeSource();

        ResolvedItemType = ItemType ?? InferItemType(all);

        var filtered = ApplyFilters(all, ResolvedItemType);
        var sorted = ApplySort(filtered);
        var view = ApplyGrouping(sorted, ResolvedItemType);

        AssignFlatIndices(view);

        _view = view;
        _dataRows = view.OfType<GridDataRow>().ToList();
        ViewReset?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Reads a cell value for the given item using a cached accessor.</summary>
    public object? GetCellValue(object item, string path)
    {
        ArgumentNullException.ThrowIfNull(item);
        return GetAccessor(item.GetType(), path).GetValue(item);
    }

    /// <summary>Gets a cached accessor for the given item type and path.</summary>
    public PropertyAccessor GetAccessor(Type itemType, string path)
        => _accessorCache.GetOrAdd(
            new KeyValuePair<Type, string>(itemType, path),
            key => PropertyAccessor.GetOrCreate(key.Key, key.Value));

    private List<object> MaterializeSource()
    {
        if (_source is null)
        {
            return new List<object>();
        }

        var result = new List<object>();
        foreach (var item in _source)
        {
            if (item is not null)
            {
                result.Add(item);
            }
        }

        return result;
    }

    private static Type? InferItemType(List<object> items)
        => items.Count > 0 ? items[0].GetType() : null;

    private List<object> ApplyFilters(List<object> items, Type? itemType)
    {
        var active = Filters.Where(f => f.Operator != GridFilterOperator.None).ToArray();
        if (active.Length == 0 || itemType is null)
        {
            return items;
        }

        var accessors = active
            .Select(f => GetAccessor(itemType, f.FieldName))
            .ToArray();

        return items.Where(item =>
        {
            for (var i = 0; i < active.Length; i++)
            {
                if (!active[i].Matches(item, accessors[i]))
                {
                    return false;
                }
            }

            return true;
        }).ToList();
    }

    private List<object> ApplySort(List<object> items)
    {
        var active = Sorts.Where(s => s.Direction != GridSortDirection.None && !string.IsNullOrEmpty(s.FieldName)).ToArray();
        if (active.Length == 0)
        {
            return items;
        }

        var itemType = ResolvedItemType;
        var accessors = active.Select(s => itemType is null ? null : GetAccessor(itemType, s.FieldName)).ToArray();

        IOrderedEnumerable<object> ordered = items
            .OrderBy(item => SortKey(item, 0), new LevelComparer(active[0]));

        for (var level = 1; level < active.Length; level++)
        {
            var captured = level;
            ordered = ordered.ThenBy(item => SortKey(item, captured), new LevelComparer(active[captured]));
        }

        return ordered.ToList();

        object? SortKey(object item, int level)
        {
            var accessor = accessors[level];
            return accessor is null ? null : accessor.GetValue(item);
        }
    }

    private List<GridRowInfo> ApplyGrouping(List<object> sortedItems, Type? itemType)
    {
        var descriptions = Groups.Where(g => !string.IsNullOrEmpty(g.FieldName)).ToArray();
        if (descriptions.Length == 0)
        {
            return sortedItems.Select(item => new GridDataRow(item)).ToList<GridRowInfo>();
        }

        if (itemType is null)
        {
            return new List<GridRowInfo>();
        }

        var accessors = descriptions.Select(d => GetAccessor(itemType, d.FieldName)).ToArray();

        // Build the group tree. Items arrive in sorted order, so groups keep that order
        // unless an explicit KeysComparer was provided (handled during lookup).
        var roots = new List<GridGroup>();
        foreach (var item in sortedItems)
        {
            List<GridGroup> siblings = roots;
            GridGroup? parent = null;
            for (var level = 0; level < descriptions.Length; level++)
            {
                var rawValue = accessors[level].GetValue(item);
                var key = descriptions[level].KeyConverter is { } converter ? converter(rawValue) : rawValue;

                var group = FindGroup(siblings, key, descriptions[level].KeysComparer);
                if (group is null)
                {
                    group = new GridGroup(key, level, parent);
                    siblings.Add(group);
                }

                group.TotalItems++;
                siblings = group.ChildGroupsInternal;
                parent = group;
            }

            if (parent is not null)
            {
                parent.ItemsInternal.Add(item);
            }
        }

        var result = new List<GridRowInfo>();
        AppendGroups(result, roots);
        return result;
    }

    private void AppendGroups(List<GridRowInfo> result, IReadOnlyList<GridGroup> groups)
    {
        foreach (var group in groups)
        {
            RestoreExpandState(group);
            result.Add(new GridGroupRow(group));
            if (group.IsExpanded)
            {
                if (group.ChildGroups.Count > 0)
                {
                    AppendGroups(result, group.ChildGroups);
                }
                else
                {
                    foreach (var item in group.Items)
                    {
                        result.Add(new GridDataRow(item));
                    }
                }
            }
        }
    }

    private void RestoreExpandState(GridGroup group)
    {
        if (_expandState.TryGetValue(GridGroupKeyPath.Of(group), out var expanded))
        {
            group.IsExpanded = expanded;
        }
        else
        {
            _expandState[GridGroupKeyPath.Of(group)] = group.IsExpanded;
        }
    }

    /// <summary>Stores the user expand/collapse choice so it survives refreshes.</summary>
    public void SetGroupExpanded(GridGroup group, bool expanded)
    {
        group.IsExpanded = expanded;
        _expandState[GridGroupKeyPath.Of(group)] = expanded;
        Refresh();
    }

    private static GridGroup? FindGroup(List<GridGroup> siblings, object? key, IComparer? comparer)
    {
        foreach (var candidate in siblings)
        {
            var result = comparer is not null
                ? comparer.Compare(candidate.Key, key)
                : GridValueCompare.Compare(candidate.Key, key);
            if (result == 0)
            {
                return candidate;
            }
        }

        return null;
    }

    private static void AssignFlatIndices(List<GridRowInfo> view)
    {
        for (var i = 0; i < view.Count; i++)
        {
            view[i].FlatIndex = i;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Stage 1 rebuilds the whole pipeline on any change; incremental updates arrive with async work (stage 1.2).
        Refresh();
    }

    private readonly struct GridGroupKeyPath
    {
        private readonly string _path;

        private GridGroupKeyPath(string path) => _path = path;

        public static GridGroupKeyPath Of(GridGroup group)
        {
            var parts = new List<string>();
            for (var current = group; current is not null; current = current.Parent)
            {
                parts.Add(Convert.ToString(current.Key, CultureInfo.InvariantCulture) ?? string.Empty);
            }

            parts.Reverse();
            return new GridGroupKeyPath(string.Join("\u001F", parts));
        }
    }

    private sealed class LevelComparer : IComparer<object?>
    {
        private readonly GridSortDescription _description;

        public LevelComparer(GridSortDescription description) => _description = description;

        public int Compare(object? x, object? y)
        {
            var result = _description.Comparer is { } custom
                ? custom.Compare(x, y)
                : GridValueCompare.Compare(x, y, _description.Culture);
            return _description.Direction == GridSortDirection.Descending ? -result : result;
        }
    }
}
