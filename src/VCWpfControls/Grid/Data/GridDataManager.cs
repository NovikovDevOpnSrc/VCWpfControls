using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Globalization;

namespace VCWpfControls.Grid.Data;

/// <summary>
/// The UI-free data engine of <c>GridControl</c>: takes the source collection and applies
/// filtering, sorting and grouping, producing a flattened observable collection of
/// <see cref="GridRowInfo"/> rows (data rows interleaved with group rows). Fully unit-testable
/// without WPF.
/// </summary>
/// <remarks>
/// The pipeline can run synchronously (<see cref="Refresh"/>), incrementally (single-item
/// collection changes without active grouping) or asynchronously (<see cref="RefreshAsync"/>,
/// computing on a background thread and applying the result on the caller's context).
/// A version counter drops results that were computed against a stale state.
/// </remarks>
public sealed class GridDataManager
{
    private readonly ConcurrentDictionary<KeyValuePair<Type, string>, PropertyAccessor> _accessorCache = new();
    private readonly ConcurrentDictionary<string, bool> _expandState = new();
    private readonly GridRowCollection _view = new();

    private IEnumerable? _source;
    private INotifyCollectionChanged? _notifier;
    private List<GridDataRow> _dataRows = new();
    private long _version;

    /// <summary>Raised after the view has been rebuilt from scratch; the control re-synchronizes its items.</summary>
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

    /// <summary>The flattened observable view: data rows and group rows in display order.</summary>
    public GridRowCollection View => _view;

    /// <summary>Data rows of the view in display order (without group rows).</summary>
    public IReadOnlyList<GridDataRow> DataRows => _dataRows;

    /// <summary>Monotonic version of the engine state; bumped on every (re)build or incremental change.</summary>
    public long Version => _version;

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

    /// <summary>Rebuilds the whole pipeline (filter → sort → group → flatten) synchronously.</summary>
    public void Refresh()
    {
        var snapshot = TakeSnapshot();
        var result = ComputePipeline(snapshot);
        ApplyPipelineResult(result);
    }

    /// <summary>
    /// Rebuilds the pipeline asynchronously: the snapshot is taken (and the result applied) on the
    /// caller's context, while filtering/sorting/grouping run on the thread pool. Results computed
    /// against a stale state are dropped; returns <see langword="false"/> in that case (the caller
    /// may retry).
    /// </summary>
    public async Task<bool> RefreshAsync()
    {
        var snapshot = TakeSnapshot();
        var result = await Task.Run(() => ComputePipeline(snapshot)).ConfigureAwait(true);
        return ApplyPipelineResult(result);
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

    /// <summary>
    /// Returns the distinct values of a field across the source (first-seen order), capped at
    /// <paramref name="maxValues"/>. Used by the column filter menu.
    /// </summary>
    public List<object?> GetDistinctValues(string fieldName, int maxValues = 500)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException("Field name must not be empty.", nameof(fieldName));
        }


        var result = new List<object?>();
        if (_source is null || maxValues <= 0)
        {
            return result;
        }

        var seen = new HashSet<object?>();
        foreach (var item in _source)
        {
            if (item is null)
            {
                continue;
            }

            var value = GetAccessor(item.GetType(), fieldName).GetValue(item);
            if (seen.Add(value))
            {
                result.Add(value);
                if (result.Count >= maxValues)
                {
                    break;
                }
            }
        }

        return result;
    }

    private PipelineSnapshot TakeSnapshot()
    {
        // Called on the caller's thread: reading the source here avoids touching a (possibly
        // non-thread-safe) collection from the background thread.
        Interlocked.Increment(ref _version);
        return new PipelineSnapshot(
            MaterializeSource(),
            Sorts.ToArray(),
            Filters.Where(f => f.Operator != GridFilterOperator.None).ToArray(),
            Groups.Where(g => !string.IsNullOrEmpty(g.FieldName)).ToArray(),
            ItemType,
            Culture,
            Volatile.Read(ref _version));
    }

    private GridPipelineResult ComputePipeline(PipelineSnapshot snapshot)
    {
        var itemType = snapshot.ItemType ?? InferItemType(snapshot.Items);
        var filtered = ApplyFilters(snapshot, snapshot.Items, itemType);
        var sorted = ApplySort(snapshot, filtered, itemType);
        var view = ApplyGrouping(snapshot, sorted, itemType);
        AssignFlatIndices(view);

        return new GridPipelineResult(
            view,
            view.OfType<GridDataRow>().ToList(),
            itemType,
            snapshot.Version);
    }

    private bool ApplyPipelineResult(GridPipelineResult result)
    {
        if (result.Version != Volatile.Read(ref _version))
        {
            return false;
        }

        ResolvedItemType = result.ResolvedItemType;
        _dataRows = result.DataRows;
        _view.ReplaceAll(result.View);
        ViewReset?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private sealed record PipelineSnapshot(
        List<object> Items,
        GridSortDescription[] Sorts,
        GridFilterDescription[] Filters,
        GridGroupDescription[] Groups,
        Type? ItemType,
        CultureInfo Culture,
        long Version);

    private sealed record GridPipelineResult(
        List<GridRowInfo> View,
        List<GridDataRow> DataRows,
        Type? ResolvedItemType,
        long Version);

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

    private List<object> ApplyFilters(PipelineSnapshot snapshot, List<object> items, Type? itemType)
    {
        var active = snapshot.Filters;
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

    private List<object> ApplySort(PipelineSnapshot snapshot, List<object> items, Type? itemType)
    {
        var active = snapshot.Sorts
            .Where(s => s.Direction != GridSortDirection.None && !string.IsNullOrEmpty(s.FieldName))
            .ToArray();
        if (active.Length == 0)
        {
            return items;
        }

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

    private List<GridRowInfo> ApplyGrouping(PipelineSnapshot snapshot, List<object> sortedItems, Type? itemType)
    {
        var descriptions = snapshot.Groups;
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
        var path = GridGroupKeyPath.Of(group).Path;
        if (_expandState.TryGetValue(path, out var expanded))
        {
            group.IsExpanded = expanded;
        }
        else
        {
            _expandState[path] = group.IsExpanded;
        }
    }

    /// <summary>Stores the user expand/collapse choice so it survives refreshes.</summary>
    public void SetGroupExpanded(GridGroup group, bool expanded)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.IsExpanded = expanded;
        _expandState[GridGroupKeyPath.Of(group).Path] = expanded;
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

    private static void AssignFlatIndices(IReadOnlyList<GridRowInfo> view)
    {
        for (var i = 0; i < view.Count; i++)
        {
            view[i].FlatIndex = i;
        }
    }

    // ============ Incremental source updates ============

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Grouped views rebuild: inserting a row may require new group headers and totals.
        if (HasActiveGrouping || ResolvedItemType is null)
        {
            Refresh();
            return;
        }

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is { Count: 1 }:
                OnItemAdded(e.NewItems[0]!);
                break;

            case NotifyCollectionChangedAction.Remove when e.OldItems is { Count: 1 }:
                OnItemRemoved(e.OldItems[0]!);
                break;

            case NotifyCollectionChangedAction.Replace when e.NewItems is { Count: 1 } && e.OldItems is { Count: 1 }:
                OnItemRemoved(e.OldItems[0]!);
                OnItemAdded(e.NewItems[0]!);
                break;

            default:
                Refresh();
                break;
        }
    }

    private bool HasActiveGrouping => Groups.Any(g => !string.IsNullOrEmpty(g.FieldName));

    private void OnItemAdded(object item)
    {
        var itemType = ResolvedItemType ?? item.GetType();
        if (ResolvedItemType is null)
        {
            ResolvedItemType = itemType;
        }

        // Respect active filters: rows that do not match are simply absent from the view.
        var activeFilters = Filters.Where(f => f.Operator != GridFilterOperator.None).ToArray();
        foreach (var filter in activeFilters)
        {
            var accessor = GetAccessor(item.GetType(), filter.FieldName);
            if (!filter.Matches(item, accessor))
            {
                return;
            }
        }

        Interlocked.Increment(ref _version);
        var row = new GridDataRow(item);
        var index = FindInsertIndex(item, itemType);
        _view.Insert(index, row);
        _dataRows.Insert(Math.Min(index, _dataRows.Count), row);
        AssignFlatIndices(_view);
    }

    private void OnItemRemoved(object item)
    {
        GridDataRow? row = null;
        foreach (var candidate in _dataRows)
        {
            if (ReferenceEquals(candidate.Item, item))
            {
                row = candidate;
                break;
            }
        }

        if (row is null)
        {
            // The item was filtered out of the view; nothing to do.
            return;
        }

        Interlocked.Increment(ref _version);
        _view.Remove(row);
        _dataRows.Remove(row);
        AssignFlatIndices(_view);
    }

    /// <summary>Binary-searches the stable insertion index for an item under the active sorts.</summary>
    private int FindInsertIndex(object item, Type itemType)
    {
        var active = Sorts
            .Where(s => s.Direction != GridSortDirection.None && !string.IsNullOrEmpty(s.FieldName))
            .ToArray();
        if (active.Length == 0)
        {
            return _view.Count;
        }

        var accessors = active.Select(s => GetAccessor(itemType, s.FieldName)).ToArray();
        int CompareToRow(GridRowInfo row)
        {
            if (row is not GridDataRow data)
            {
                return -1;
            }

            for (var level = 0; level < active.Length; level++)
            {
                var newX = accessors[level].GetValue(item);
                var newY = accessors[level].GetValue(data.Item);
                var result = active[level].Comparer is { } custom
                    ? custom.Compare(newX, newY)
                    : GridValueCompare.Compare(newX, newY, active[level].Culture);
                if (result != 0)
                {
                    return active[level].Direction == GridSortDirection.Descending ? -result : result;
                }
            }

            // Equal keys: insert after existing rows to keep a stable order.
            return 1;
        }

        var low = 0;
        var high = _view.Count;
        while (low < high)
        {
            var mid = low + (high - low) / 2;
            if (CompareToRow(_view[mid]) < 0)
            {
                high = mid;
            }
            else
            {
                low = mid + 1;
            }
        }

        return low;
    }

    private readonly struct GridGroupKeyPath
    {
        private readonly string _path;

        private GridGroupKeyPath(string path) => _path = path;

        public string Path => _path;

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
