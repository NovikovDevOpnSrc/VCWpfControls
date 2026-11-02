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

    private readonly List<TreeListNode> _roots = new();
    private readonly Dictionary<object, TreeListNode> _nodesByItem = new();
    private readonly Dictionary<object, bool> _expandState = new();
    private IEnumerable? _rootsSource;
    private INotifyCollectionChanged? _rootsNotifier;
    private readonly List<INotifyCollectionChanged> _childNotifiers = new();

    /// <summary>Creates the tree list view.</summary>
    public TreeListView()
    {
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
        => ((TreeListView)d).RebuildTree();

    private void OnRootsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildTree();

    /// <inheritdoc />
    protected override void RebuildDataView() => RebuildTree();

    /// <inheritdoc />
    public override void Refresh() => RebuildTree();

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
                ResolveChildren(node);
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
        view.Add(new GridDataRow(node.Item));
        if (node.IsExpanded != true || node.ChildrenCount == 0)
        {
            return;
        }

        SortSiblings(node.Children!);
        foreach (var child in node.Children!)
        {
            _nodesByItem[child.Item] = child;
            ResolveChildren(child);
            AppendNode(view, child);
        }
    }

    private void ResolveChildren(TreeListNode node)
    {
        if (node.Children is not null)
        {
            return;
        }

        node.Children = new List<TreeListNode>();
        var children = PropertyAccessor.GetOrCreate(node.Item.GetType(), ChildMemberPath).GetValue(node.Item)
            as IEnumerable;
        if (children is not null)
        {
            foreach (var child in children)
            {
                if (child is null)
                {
                    continue;
                }

                node.Children.Add(new TreeListNode(child, node.Level + 1)
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
        if (children is INotifyCollectionChanged notifier)
        {
            _childNotifiers.Add(notifier);
            notifier.CollectionChanged += OnChildrenChanged;
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
            row.TreeHasChildren = node.ChildrenCount > 0;
            row.TreeIsExpanded = node.IsExpanded == true;
            row.UpdateTreeToggle();
        }
        else
        {
            row.TreeLevel = 0;
            row.TreeHasChildren = false;
        }
    }

    /// <summary>One node of the flattened tree.</summary>
    private sealed class TreeListNode
    {
        public TreeListNode(object item, int level)
        {
            Item = item;
            Level = level;
        }

        public object Item { get; }

        public int Level { get; }

        public List<TreeListNode>? Children { get; set; }

        public bool? IsExpanded { get; set; }

        public int ChildrenCount => Children?.Count ?? 0;

    }
}
