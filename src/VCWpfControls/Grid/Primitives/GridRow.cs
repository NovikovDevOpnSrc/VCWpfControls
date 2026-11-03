using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using VCWpfControls.Grid.Data;

namespace VCWpfControls.Grid.Primitives;

/// <summary>
/// The container for one row of the <see cref="GridControl"/>. Hosts a <see cref="GridCellsPanel"/>
/// and exposes selection/current states to the template. Not focusable itself: keyboard input
/// is routed through the owning grid.
/// </summary>
public class GridRow : Control
{
    /// <summary>Identifies the <see cref="IsSelected"/> dependency property.</summary>
    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
        nameof(IsSelected), typeof(bool), typeof(GridRow),
        new PropertyMetadata(false, OnIsSelectedChanged));

    /// <summary>Identifies the <see cref="IsCurrent"/> dependency property.</summary>
    public static readonly DependencyProperty IsCurrentProperty = DependencyProperty.Register(
        nameof(IsCurrent), typeof(bool), typeof(GridRow), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsEditing"/> dependency property.</summary>
    public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register(
        nameof(IsEditing), typeof(bool), typeof(GridRow), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsGroupRow"/> dependency property.</summary>
    public static readonly DependencyProperty IsGroupRowProperty = DependencyProperty.Register(
        nameof(IsGroupRow), typeof(bool), typeof(GridRow), new PropertyMetadata(false));

    static GridRow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GridRow),
            new FrameworkPropertyMetadata(typeof(GridRow)));
        FocusableProperty.OverrideMetadata(typeof(GridRow), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Whether the row's data item is selected.</summary>
    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        internal set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>Whether the row hosts the current cell.</summary>
    public bool IsCurrent
    {
        get => (bool)GetValue(IsCurrentProperty);
        internal set => SetValue(IsCurrentProperty, value);
    }

    /// <summary>Whether one of the row's cells is in edit mode.</summary>
    public bool IsEditing
    {
        get => (bool)GetValue(IsEditingProperty);
        internal set => SetValue(IsEditingProperty, value);
    }

    /// <summary>Whether the row is a group header row (has no data item).</summary>
    public bool IsGroupRow
    {
        get => (bool)GetValue(IsGroupRowProperty);
        private set => SetValue(IsGroupRowProperty, value);
    }

    /// <summary>Identifies the <see cref="TreeHasChildren"/> dependency property.</summary>
    public static readonly DependencyProperty TreeHasChildrenProperty = DependencyProperty.Register(
        nameof(TreeHasChildren), typeof(bool), typeof(GridRow), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="TreeIsExpanded"/> dependency property.</summary>
    public static readonly DependencyProperty TreeIsExpandedProperty = DependencyProperty.Register(
        nameof(TreeIsExpanded), typeof(bool), typeof(GridRow), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="HasNodeCheckBox"/> dependency property.</summary>
    public static readonly DependencyProperty HasNodeCheckBoxProperty = DependencyProperty.Register(
        nameof(HasNodeCheckBox), typeof(bool), typeof(GridRow), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="NodeIsChecked"/> dependency property (null = indeterminate).</summary>
    public static readonly DependencyProperty NodeIsCheckedProperty = DependencyProperty.Register(
        nameof(NodeIsChecked), typeof(bool?), typeof(GridRow), new PropertyMetadata(false));

    /// <summary>Whether the row is a tree node with expandable children (TreeListView).</summary>
    public bool TreeHasChildren
    {
        get => (bool)GetValue(TreeHasChildrenProperty);
        internal set => SetValue(TreeHasChildrenProperty, value);
    }

    /// <summary>Whether the tree node is expanded (TreeListView).</summary>
    public bool TreeIsExpanded
    {
        get => (bool)GetValue(TreeIsExpandedProperty);
        internal set => SetValue(TreeIsExpandedProperty, value);
    }

    /// <summary>Whether the row shows a node check box (TreeListView with ShowNodeCheckBoxes).</summary>
    public bool HasNodeCheckBox
    {
        get => (bool)GetValue(HasNodeCheckBoxProperty);
        internal set => SetValue(HasNodeCheckBoxProperty, value);
    }

    /// <summary>Node check state: true/false/null (indeterminate).</summary>
    public bool? NodeIsChecked
    {
        get => (bool?)GetValue(NodeIsCheckedProperty);
        internal set => SetValue(NodeIsCheckedProperty, value);
    }

    /// <summary>Tree nesting level of the row (0 for roots); 0 for non-tree grids.</summary>
    internal int TreeLevel { get; set; }

    /// <summary>The row info this container presents.</summary>
    internal GridRowInfo? RowInfo { get; private set; }

    /// <summary>The bound data item (null for group rows).</summary>
    internal object? DataItem => RowInfo is GridDataRow data ? data.Item : null;

    /// <summary>The cells panel of the row (after the template has been applied).</summary>
    internal GridCellsPanel? CellsPanel { get; private set; }

    private GridGroupRow? _groupRow;
    private ToggleButton? _groupToggle;
    private TextBlock? _groupText;
    private Border? _groupHost;
    private ContentPresenter? _rowDetails;
    private ToggleButton? _treeToggle;
    private bool _syncingTreeToggle;
    private System.Windows.Controls.CheckBox? _nodeCheckBox;

    /// <summary>The owning grid.</summary>
    internal GridControl? Owner { get; private set; }

    /// <summary>Binds the container to a row info item of the grid's view.</summary>
    internal void Prepare(GridRowInfo rowInfo, GridControl owner)
    {
        RowInfo = rowInfo;
        Owner = owner;

        if (rowInfo is GridGroupRow groupRow)
        {
            PrepareGroupRow(groupRow, owner);
            return;
        }

        IsGroupRow = false;
        if (CellsPanel is not null)
        {
            CellsPanel.Visibility = Visibility.Visible;
            CellsPanel.Attach(this, owner);
            CellsPanel.EnsureCells();
            CellsPanel.PrepareCells();
        }

        if (_groupHost is not null)
        {
            _groupHost.Visibility = Visibility.Collapsed;
        }

        IsSelected = DataItem is not null && owner.IsItemSelected(DataItem);
        IsCurrent = DataItem is not null && ReferenceEquals(DataItem, owner.CurrentItem);
        UpdateRowDetails();
    }

    private void PrepareGroupRow(GridGroupRow groupRow, GridControl owner)
    {
        IsGroupRow = true;
        IsSelected = false;
        IsCurrent = false;
        _groupRow = groupRow;

        if (CellsPanel is not null)
        {
            CellsPanel.Visibility = Visibility.Collapsed;
        }

        if (_groupHost is null)
        {
            return;
        }

        _groupHost.Visibility = Visibility.Visible;
        _groupHost.Margin = new Thickness(8 + groupRow.Level * 16, 0, 0, 0);

        if (_groupText is not null)
        {
            _groupText.Text = Localization.GridStrings.GroupRowItemCount(
                groupRow.Group.Key,
                groupRow.Group.TotalItems);
        }

        if (_groupToggle is not null)
        {
            _groupToggle.Checked -= OnGroupToggleChanged;
            _groupToggle.Unchecked -= OnGroupToggleChanged;
            _groupToggle.IsChecked = groupRow.Group.IsExpanded;
            _groupToggle.Checked += OnGroupToggleChanged;
            _groupToggle.Unchecked += OnGroupToggleChanged;
        }
    }

    /// <summary>Syncs the tree expander toggle with the row's tree state (no events raised).</summary>
    internal void UpdateTreeToggle()
    {
        if (_treeToggle is null)
        {
            return;
        }

        _syncingTreeToggle = true;
        try
        {
            _treeToggle.Margin = new Thickness(TreeLevel * 16, 0, 0, 0);
            _treeToggle.IsChecked = TreeIsExpanded;
        }
        finally
        {
            _syncingTreeToggle = false;
        }
    }

    private void OnNodeCheckBoxClick(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.CheckBox { IsChecked: { } isChecked } && Owner is TreeList.TreeListView tree)
        {
            tree.SetNodeChecked(DataItem, isChecked);
        }
    }

    /// <summary>Syncs the node check box with the row's tree state (no event feedback).</summary>
    internal void UpdateNodeCheckBox()
    {
        if (_nodeCheckBox is null)
        {
            return;
        }

        _nodeCheckBox.Margin = new Thickness(TreeLevel * 16 + 20, 0, 0, 0);
        _nodeCheckBox.IsChecked = NodeIsChecked;
    }

    private void OnTreeToggleChanged(object sender, RoutedEventArgs e)
    {
        if (_syncingTreeToggle)
        {
            return;
        }

        if (_treeToggle is not null && Owner is TreeList.TreeListView treeView)
        {
            treeView.ToggleNode(DataItem, _treeToggle.IsChecked == true);
        }
    }

    private void OnGroupToggleChanged(object sender, RoutedEventArgs e)
    {
        if (_groupRow is not null
            && sender is ToggleButton toggle
            && Owner is not null)
        {
            Owner.SetGroupExpanded(_groupRow.Group, toggle.IsChecked == true);
        }
    }

    /// <summary>Re-selects cell elements after the template has been applied.</summary>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        CellsPanel = GetTemplateChild("PART_Cells") as GridCellsPanel;
        _groupHost = GetTemplateChild("PART_GroupHost") as Border;
        _groupToggle = GetTemplateChild("PART_GroupToggle") as ToggleButton;
        _groupText = GetTemplateChild("PART_GroupText") as TextBlock;
        _rowDetails = GetTemplateChild("PART_RowDetails") as ContentPresenter;
        _treeToggle = GetTemplateChild("PART_TreeToggle") as ToggleButton;
        if (GetTemplateChild("PART_NodeCheckBox") is System.Windows.Controls.CheckBox nodeCheckBox)
        {
            if (_nodeCheckBox is not null)
            {
                _nodeCheckBox.Click -= OnNodeCheckBoxClick;
            }

            _nodeCheckBox = nodeCheckBox;
            _nodeCheckBox.Click += OnNodeCheckBoxClick;
        }
        if (_treeToggle is not null)
        {
            _treeToggle.Checked -= OnTreeToggleChanged;
            _treeToggle.Unchecked -= OnTreeToggleChanged;
            _treeToggle.Checked += OnTreeToggleChanged;
            _treeToggle.Unchecked += OnTreeToggleChanged;
        }

        UpdateTreeToggle();
        UpdateNodeCheckBox();

        if (CellsPanel is not null && Owner is not null && !IsGroupRow)
        {
            CellsPanel.Attach(this, Owner);
            CellsPanel.EnsureCells();
            CellsPanel.PrepareCells();
        }

        UpdateRowDetails();

        if (IsGroupRow && _groupRow is not null && Owner is not null)
        {
            PrepareGroupRow(_groupRow, Owner);
        }
    }

    /// <summary>Clears cell state when the container is discarded or recycled.</summary>
    internal void Clear()
    {
        CellsPanel?.ClearCells();
        if (_groupToggle is not null)
        {
            _groupToggle.Checked -= OnGroupToggleChanged;
            _groupToggle.Unchecked -= OnGroupToggleChanged;
        }

        IsSelected = false;
        IsCurrent = false;
        IsEditing = false;
        IsGroupRow = false;
        _groupRow = null;
        RowInfo = null;
        if (_rowDetails is not null)
        {
            _rowDetails.Visibility = Visibility.Collapsed;
            _rowDetails.Content = null;
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Owner?.HandleRowMousePress(this, e);
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (Owner is not null)
        {
            GridControl.HandleRowMouseRelease(this, e);
        }
    }

    /// <inheritdoc />
    protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        Owner?.HandleRowDoubleClick(this, e);
    }

    /// <summary>Refreshes the row details presenter content and visibility.</summary>
    internal void UpdateRowDetails()
    {
        if (_rowDetails is null || Owner is null)
        {
            return;
        }

        var showDetails = DataItem is not null
            && Owner.RowDetailsTemplate is not null
            && Owner.RowDetailsVisibilityMode switch
            {
                GridRowDetailsVisibilityMode.Visible => true,
                GridRowDetailsVisibilityMode.VisibleWhenSelected => IsSelected,
                _ => false,
            };

        _rowDetails.Visibility = showDetails ? Visibility.Visible : Visibility.Collapsed;
        _rowDetails.Content = showDetails ? DataItem : null;
        if (showDetails)
        {
            _rowDetails.ContentTemplate = Owner.RowDetailsTemplate;
        }

        InvalidateMeasure();
    }

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GridRow row)
        {
            row.UpdateRowDetails();
        }
    }
}
