using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Data;
using VCWpfControls.Grid.Primitives;

namespace VCWpfControls.TreeList;

/// <summary>
/// A tree with columns: builds on <see cref="GridControl"/> (columns, virtualization,
/// selection, editing, row details, themes) and renders a hierarchical source as indented
/// rows with expand/collapse toggles. <see cref="ItemsSource"/> holds the root items;
/// children are read from <see cref="ChildMemberPath"/> (any <see cref="IEnumerable"/>).
/// Sorting (header clicks or API) orders the children within each level.
/// </summary>
public class TreeListView : GridControl
{
    /// <summary>Identifies the <see cref="ItemsSource"/> dependency property (roots of the tree).</summary>
    public new static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(TreeListView),
        new PropertyMetadata(null, OnTreeItemsSourceChanged));

    /// <summary>Identifies the <see cref="ChildMemberPath"/> dependency property.</summary>
    public static readonly DependencyProperty ChildMemberPathProperty = DependencyProperty.Register(
        nameof(ChildMemberPath), typeof(string), typeof(TreeListView),
        new PropertyMetadata("Children", OnTreeStructureChanged));

    /// <summary>Identifies the <see cref="InitiallyExpanded"/> dependency property.</summary>
    public static readonly DependencyProperty InitiallyExpandedProperty = DependencyProperty.Register(
        nameof(InitiallyExpanded), typeof(bool), typeof(TreeListView),
        new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="FilterPredicate"/> dependency property.</summary>
    public static readonly DependencyProperty FilterPredicateProperty = DependencyProperty.Register(
        nameof(FilterPredicate), typeof(Predicate<object>), typeof(TreeListView),
        new PropertyMetadata(null, OnTreeFilterChanged));

    /// <summary>Identifies the <see cref="ChildrenProvider"/> dependency property.</summary>
    public static readonly DependencyProperty ChildrenProviderProperty = DependencyProperty.Register(
        nameof(ChildrenProvider), typeof(Func<object, IEnumerable?>), typeof(TreeListView),
        new PropertyMetadata(null, OnTreeStructureChanged));

    /// <summary>Identifies the <see cref="HasChildrenPredicate"/> dependency property.</summary>
    public static readonly DependencyProperty HasChildrenPredicateProperty = DependencyProperty.Register(
        nameof(HasChildrenPredicate), typeof(Func<object, bool>), typeof(TreeListView),
        new PropertyMetadata(null, OnTreeStructureChanged));

    private readonly List<TreeListNode> _roots = new();
    private readonly Dictionary<object, TreeListNode> _nodesByItem = new();
    private readonly Dictionary<object, bool> _expandState = new();
    private readonly Dictionary<object, bool> _checkState = new();

    /// <summary>Identifies the <see cref="ShowNodeCheckBoxes"/> dependency property.</summary>
    public static readonly DependencyProperty ShowNodeCheckBoxesProperty = DependencyProperty.Register(
        nameof(ShowNodeCheckBoxes), typeof(bool), typeof(TreeListView),
        new PropertyMetadata(false, OnTreeStructureChanged));

    /// <summary>Identifies the <see cref="NodeCheckChanged"/> routed event.</summary>
    public static readonly RoutedEvent NodeCheckChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(NodeCheckChanged), RoutingStrategy.Bubble,
        typeof(EventHandler<NodeCheckChangedEventArgs>), typeof(TreeListView));
    private readonly Dictionary<object, List<TreeListNode>> _loadedChildren = new();
    private readonly Dictionary<object, string> _treeSummaryCache = new();
    private IEnumerable? _rootsSource;
    private INotifyCollectionChanged? _rootsNotifier;
    private readonly List<INotifyCollectionChanged> _childNotifiers = new();

    /// <summary>Creates the tree list view.</summary>
    /// <summary>Aggregates shown next to tree nodes (see <see cref="GridTreeSummaryItem"/>).</summary>
    public System.Collections.ObjectModel.ObservableCollection<GridTreeSummaryItem> TreeSummaryItems { get; }
        = new();

    /// <summary>Creates the tree list view.</summary>
    public TreeListView()
    {
        TreeSummaryItems.CollectionChanged += (_, _) => InvalidateTreeSummaries();
    }

    /// <summary>Drops cached node summaries and repaints the realized rows.</summary>
    internal void InvalidateTreeSummaries()
    {
        _treeSummaryCache.Clear();
        foreach (var row in _rowRegistry)
        {
            row.CellsPanel?.InvalidateVisual();
        }
    }

    /// <summary>
    /// The summary text shown at the right of the node's row (null when no summary applies).
    /// Computed over the FULL tree structure — expansion state does not change the values.
    /// </summary>
    internal string? GetTreeSummaryText(object item)
    {
        if (TreeSummaryItems.Count == 0)
        {
            return null;
        }

        if (_treeSummaryCache.TryGetValue(item, out var cached))
        {
            return cached.Length == 0 ? null : cached;
        }

        string? result = null;
        if (_nodesByItem.TryGetValue(item, out var node))
        {
            result = BuildTreeSummaryText(node);
        }

        _treeSummaryCache[item] = result ?? string.Empty;
        return result;
    }

    private string? BuildTreeSummaryText(TreeListNode node)
    {
        ResolveChildren(node); // lazy children: a node may be collapsed/unresolved
        if (node.ChildrenCount == 0)
        {
            return null; // leaf: no summary
        }

        var parts = new List<string>();
        foreach (var summary in TreeSummaryItems)
        {
            if (summary.Level is { } level && node.Level != level)
            {
                continue;
            }

            var items = CollectSummaryItems(node, summary.Mode);
            var value = Grid.Data.GridSummaryCalculator.Calculate(
                summary.Function, summary.FieldName, items,
                (type, field) => Grid.Data.PropertyAccessor.GetOrCreate(type, field));
            var formatted = value is null
                ? string.Empty
                : string.IsNullOrEmpty(summary.StringFormat)
                    ? Convert.ToString(value, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty
                    : string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:" + summary.StringFormat + "}", value);
            parts.Add(VCWpfControls.Localization.GridStrings.Summary(summary.Function, summary.FieldName, formatted));
        }

        return parts.Count == 0 ? null : string.Join("   ", parts);
    }

    private List<object> CollectSummaryItems(TreeListNode node, GridTreeSummaryMode mode)
    {
        var items = new List<object>();
        if (mode == GridTreeSummaryMode.Children)
        {
            foreach (var child in node.Children ?? new List<TreeListNode>())
            {
                items.Add(child.Item);
            }

            return items;
        }

        void Walk(List<TreeListNode> children)
        {
            foreach (var child in children)
            {
                items.Add(child.Item);
                ResolveChildren(child); // lazy subtrees: resolve before descending
                if (child.Children is { } grand)
                {
                    Walk(grand);
                }
            }
        }

        Walk(node.Children ?? new List<TreeListNode>());
        return items;
    }

    /// <summary>The root items of the tree (bindable).</summary>
    public new IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Property path of the children collection within each item, e.g. <c>"Children"</c>.</summary>
    public string ChildMemberPath
    {
        get => (string)GetValue(ChildMemberPathProperty);
        set => SetValue(ChildMemberPathProperty, value);
    }

    /// <summary>Whether nodes are expanded initially (level 0).</summary>
    public bool InitiallyExpanded
    {
        get => (bool)GetValue(InitiallyExpandedProperty);
        set => SetValue(InitiallyExpandedProperty, value);
    }

    /// <summary>Whether every node shows a check box with tri-state cascade.</summary>
    public bool ShowNodeCheckBoxes
    {
        get => (bool)GetValue(ShowNodeCheckBoxesProperty);
        set => SetValue(ShowNodeCheckBoxesProperty, value);
    }

    /// <summary>Occurs after a node check state changed (after the cascade).</summary>
    public event EventHandler<NodeCheckChangedEventArgs> NodeCheckChanged
    {
        add => AddHandler(NodeCheckChangedEvent, value);
        remove => RemoveHandler(NodeCheckChangedEvent, value);
    }

    /// <summary>
    /// Node filter: a node is visible when it matches or any of its descendants matches.
    /// Ancestors of matches are shown and temporarily expanded while the filter is active;
    /// manual expansion state is restored when the filter is cleared.
    /// </summary>
    public Predicate<object>? FilterPredicate
    {
        get => (Predicate<object>?)GetValue(FilterPredicateProperty);
        set => SetValue(FilterPredicateProperty, value);
    }

    /// <summary>
    /// Lazy children source; when set, it takes precedence over <see cref="ChildMemberPath"/>
    /// and children are loaded on first expansion only.
    /// </summary>
    public Func<object, IEnumerable?>? ChildrenProvider
    {
        get => (Func<object, IEnumerable?>?)GetValue(ChildrenProviderProperty);
        set => SetValue(ChildrenProviderProperty, value);
    }

    /// <summary>
    /// Decides whether a node shows its expander without loading children
    /// (used with <see cref="ChildrenProvider"/>).
    /// </summary>
    public Func<object, bool>? HasChildrenPredicate
    {
        get => (Func<object, bool>?)GetValue(HasChildrenPredicateProperty);
        set => SetValue(HasChildrenPredicateProperty, value);
    }

    private static void OnTreeFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((TreeListView)d).RebuildTree();

    private bool PassesFilter(TreeListNode node)
    {
        if (FilterPredicate is not { } filter)
        {
            return true;
        }

        if (filter(node.Item))
        {
            return true;
        }

        ResolveChildren(node);
        foreach (var child in node.Children!)
        {
            if (PassesFilter(child))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasMatchingDescendant(TreeListNode node)
    {
        ResolveChildren(node);
        foreach (var child in node.Children!)
        {
            if (PassesFilter(child))
            {
                return true;
            }
        }

        return false;
    }

    private static void OnTreeItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var tree = (TreeListView)d;
        if (tree._rootsNotifier is not null)
        {
            tree._rootsNotifier.CollectionChanged -= tree.OnRootsChanged;
            tree._rootsNotifier = null;
        }

        tree._rootsSource = (IEnumerable?)e.NewValue;
        tree._rootsNotifier = tree._rootsSource as INotifyCollectionChanged;
        if (tree._rootsNotifier is not null)
        {
            tree._rootsNotifier.CollectionChanged += tree.OnRootsChanged;
        }

        tree.RebuildTree();
    }

    private static void OnTreeStructureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var tree = (TreeListView)d;
        tree._loadedChildren.Clear();
        tree.RebuildTree();
    }

    private void OnRootsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildTree();

    private void OnProviderChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Live provider collections invalidate the whole cache.
        _loadedChildren.Clear();
        RebuildTree();
    }

    /// <inheritdoc />
    protected override void RebuildDataView() => RebuildTree();

    /// <inheritdoc />
    /// <summary>Rebuilds the whole tree view (structure, expansion, summaries).</summary>
    public override void Refresh() => RebuildTree();

    /// <summary>Whether the node's item is checked.</summary>
    public bool IsNodeChecked(object item)
        => _checkState.TryGetValue(item, out var checkedValue) && checkedValue;

    /// <summary>Checks/unchecks a node with cascade: the node and all loaded descendants.</summary>
    public void SetNodeChecked(object? item, bool isChecked)
    {
        if (item is null || !_nodesByItem.TryGetValue(item, out var node))
        {
            return;
        }

        ApplyCheck(node, isChecked);

        // Ancestors derive their tri-state from children instead of keeping a stale
        // stored state from an earlier cascade.
        for (var ancestor = node.Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            _checkState.Remove(ancestor.Item);
        }

        RebuildTree();

        RaiseEvent(new NodeCheckChangedEventArgs(item, isChecked)
        {
            RoutedEvent = NodeCheckChangedEvent,
            Source = this,
        });
    }

    /// <summary>All checked items of the tree (regardless of expand/collapse state).</summary>
    public IReadOnlyList<object> CheckedItems
    {
        get
        {
            var result = new List<object>();
            foreach (var pair in _checkState)
            {
                if (pair.Value && _nodesByItem.ContainsKey(pair.Key))
                {
                    result.Add(pair.Key);
                }
            }

            return result;
        }
    }

    private void ApplyCheck(TreeListNode node, bool isChecked)
    {
        _checkState[node.Item] = isChecked;
        // In path mode resolving is a cheap reflection read, so cascade covers collapsed
        // subtrees; in provider mode checking a collapsed node records its own state only
        // (no forced loading of the whole lazy subtree).
        if (node.Children is null)
        {
            if (ChildrenProvider is not null)
            {
                return;
            }

            ResolveChildren(node);
            if (node.Children is null)
            {
                return;
            }
        }

        foreach (var child in node.Children)
        {
            ApplyCheck(child, isChecked);
        }
    }

    /// <summary>Effective check state of a node (indeterminate = mixed descendants).</summary>
    private bool? ComputeCheckState(TreeListNode node)
    {
        if (_checkState.TryGetValue(node.Item, out var own))
        {
            return own;
        }

        // Derived from already-loaded children only — never triggers lazy loading.
        if (node.Children is not { Count: > 0 })
        {
            return false;
        }

        var anyChecked = false;
        var anyUnchecked = false;
        foreach (var child in node.Children)
        {
            switch (ComputeCheckState(child))
            {
                case true:
                    anyChecked = true;
                    break;
                case false:
                    anyUnchecked = true;
                    break;
                case null:
                    anyChecked = true;
                    anyUnchecked = true;
                    break;
            }

            if (anyChecked && anyUnchecked)
            {
                return null;
            }
        }

        return anyChecked ? true : anyUnchecked ? false : false;
    }

    /// <summary>Expands or collapses the node of the given item and rebuilds the view.</summary>
    public void ToggleNode(object? item, bool expanded)
    {
        if (item is null)
        {
            return;
        }

        _expandState[item] = expanded;
        if (_nodesByItem.TryGetValue(item, out var node))
        {
            node.IsExpanded = expanded;
        }

        RebuildTree();
    }

    /// <summary>Expands all nodes (existing children collections are visited).</summary>
    public void ExpandAll()
    {
        foreach (var (item, _) in _nodesByItem)
        {
            _expandState[item] = true;
        }

        RebuildTree();
    }

    /// <summary>Collapses all nodes.</summary>
    public void CollapseAll()
    {
        _expandState.Clear();
        RebuildTree();
    }

    /// <summary>Whether the node of the given item is currently expanded.</summary>
    public bool IsExpanded(object item)
        => _expandState.TryGetValue(item, out var expanded) && expanded;

    /// <summary>Rebuilds the node tree and the flattened view.</summary>
    protected void RebuildTree()
    {
        foreach (var notifier in _childNotifiers)
        {
            notifier.CollectionChanged -= OnChildrenChanged;
            notifier.CollectionChanged -= OnProviderChildrenChanged;
        }

        _childNotifiers.Clear();
        _nodesByItem.Clear();
        _roots.Clear();

        var itemType = (Type?)null;
        if (_rootsSource is not null)
        {
            foreach (var root in _rootsSource)
            {
                if (root is null)
                {
                    continue;
                }

                itemType ??= root.GetType();
                var node = new TreeListNode(root, 0)
                {
                    IsExpanded = _expandState.TryGetValue(root, out var rootState) && rootState,
                };
                _roots.Add(node);
                _nodesByItem[root] = node;
                if (ChildrenProvider is null)
                {
                    // Path mode materializes eagerly; provider mode loads on first expansion.
                    ResolveChildren(node);
                }
            }
        }

        SortSiblings(_roots);
        var view = new List<GridRowInfo>();
        foreach (var root in _roots)
        {
            AppendNode(view, root);
        }

        DataEngine.ApplyExternalView(view, itemType);
    }

    private void AppendNode(List<GridRowInfo> view, TreeListNode node)
    {
        if (!PassesFilter(node))
        {
            return;
        }

        view.Add(new GridDataRow(node.Item));

        // While filtering, ancestors of matches are temporarily expanded so matches are visible.
        var expanded = FilterPredicate is not null && HasMatchingDescendant(node)
            ? true
            : node.IsExpanded == true;
        if (!expanded || !EnsureChildrenLoaded(node))
        {
            return;
        }

        SortSiblings(node.Children!);
        foreach (var child in node.Children!)
        {
            _nodesByItem[child.Item] = child;
            AppendNode(view, child);
        }
    }

    /// <summary>Whether the node may have children (expander visibility) without loading them.</summary>
    private bool HasChildrenHint(TreeListNode node)
    {
        if (ChildrenProvider is { } provider)
        {
            if (node.Children is not null)
            {
                return node.Children.Count > 0;
            }

            return HasChildrenPredicate is { } predicate && predicate(node.Item);
        }

        ResolveChildren(node);
        return node.ChildrenCount > 0;
    }

    /// <summary>Loads children if needed; returns whether any children exist.</summary>
    private bool EnsureChildrenLoaded(TreeListNode node)
    {
        if (ChildrenProvider is not null)
        {
            if (node.Children is null)
            {
                ResolveChildren(node);
            }

            return node.ChildrenCount > 0;
        }

        ResolveChildren(node);
        return node.ChildrenCount > 0;
    }

    private void ResolveChildren(TreeListNode node)
    {
        if (node.Children is not null)
        {
            return;
        }

        // Provider mode: reuse previously loaded children across rebuilds (cache by item).
        if (ChildrenProvider is not null && _loadedChildren.TryGetValue(node.Item, out var cached))
        {
            node.Children = cached;
            return;
        }

        node.Children = new List<TreeListNode>();
        IEnumerable? children = ChildrenProvider is { } provider
            ? provider(node.Item)
            : PropertyAccessor.GetOrCreate(node.Item.GetType(), ChildMemberPath).GetValue(node.Item) as IEnumerable;
        if (children is not null)
        {
            foreach (var child in children)
            {
                if (child is null)
                {
                    continue;
                }

                node.Children.Add(new TreeListNode(child, node.Level + 1, node)
                {
                    IsExpanded = _expandState.TryGetValue(child, out var childState) && childState,
                });
                if (node.Children[^1].Item is { } childItem)
                {
                    _nodesByItem[childItem] = node.Children[^1];
                }
            }
        }

        node.IsExpanded ??= false;
        if (ChildrenProvider is not null)
        {
            _loadedChildren[node.Item] = node.Children;
        }

        if (children is INotifyCollectionChanged notifier)
        {
            _childNotifiers.Add(notifier);
            notifier.CollectionChanged += ChildrenProvider is not null ? OnProviderChildrenChanged : OnChildrenChanged;
        }
    }

    private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildTree();

    private void SortSiblings(List<TreeListNode> siblings)
    {
        if (SortDescriptions.Count == 0)
        {
            return;
        }

        var active = SortDescriptions
            .Where(s => s.Direction != GridSortDirection.None && !string.IsNullOrEmpty(s.FieldName))
            .ToArray();
        if (active.Length == 0)
        {
            return;
        }

        siblings.Sort((x, y) =>
        {
            for (var level = 0; level < active.Length; level++)
            {
                var ax = PropertyAccessor.GetOrCreate(x.Item.GetType(), active[level].FieldName).GetValue(x.Item);
                var ay = PropertyAccessor.GetOrCreate(y.Item.GetType(), active[level].FieldName).GetValue(y.Item);
                var result = active[level].Comparer is { } custom
                    ? custom.Compare(ax, ay)
                    : GridValueCompare.Compare(ax, ay, active[level].Culture);
                if (result != 0)
                {
                    return active[level].Direction == GridSortDirection.Descending ? -result : result;
                }
            }

            return 0;
        });
    }

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);

        if (element is not GridRow row || row.DataItem is not { } dataItem)
        {
            return;
        }

        if (_nodesByItem.TryGetValue(dataItem, out var node))
        {
            row.TreeLevel = node.Level;
            row.TreeHasChildren = HasChildrenHint(node);
            row.TreeIsExpanded = node.IsExpanded == true;
            row.HasNodeCheckBox = ShowNodeCheckBoxes;
            row.NodeIsChecked = ComputeCheckState(node);
            row.UpdateTreeToggle();
            row.UpdateNodeCheckBox();
        }
        else
        {
            row.TreeLevel = 0;
            row.TreeHasChildren = false;
            row.HasNodeCheckBox = false;
        }
    }

    /// <summary>One node of the flattened tree.</summary>
    private sealed class TreeListNode
    {
        public TreeListNode(object item, int level, TreeListNode? parent = null)
        {
            Item = item;
            Level = level;
            Parent = parent;
        }

        public object Item { get; }

        public int Level { get; }

        public TreeListNode? Parent { get; }

        public List<TreeListNode>? Children { get; set; }

        public bool? IsExpanded { get; set; }

        public int ChildrenCount => Children?.Count ?? 0;

    }
}
