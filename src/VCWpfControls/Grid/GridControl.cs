using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Data;
using VCWpfControls.Grid.Primitives;

namespace VCWpfControls.Grid;

/// <summary>Selection behavior of <see cref="GridControl"/>.</summary>
public enum GridSelectionMode
{
    /// <summary>Only one row can be selected at a time.</summary>
    SingleRow = 0,

    /// <summary>Multiple rows can be selected with Ctrl/Shift.</summary>
    Extended = 1,
}

/// <summary>Clipboard copy behavior of <see cref="GridControl"/>.</summary>
public enum GridClipboardCopyMode
{
    /// <summary>Copy is disabled.</summary>
    None = 0,

    /// <summary>Copy includes a header line.</summary>
    IncludeHeaders = 1,
}

/// <summary>When row details (see <c>GridControl.RowDetailsTemplate</c>) are displayed.</summary>
public enum GridRowDetailsVisibilityMode
{
    /// <summary>Details are never shown.</summary>
    Collapsed = 0,

    /// <summary>Details are shown only for selected rows.</summary>
    VisibleWhenSelected = 1,

    /// <summary>Details are shown for every data row.</summary>
    Visible = 2,
}

/// <summary>
/// A fast, virtualized, MVVM-friendly data grid. Bind <see cref="ItemsSource"/> to any
/// <see cref="IEnumerable"/> (live updates supported through <see cref="ObservableCollection{T}"/>),
/// define columns manually or let the grid generate them from the item type.
/// </summary>
[ContentProperty(nameof(Columns))]
public partial class GridControl : ItemsControl
{
    /// <summary>Template part: the vertical scroll host.</summary>
    protected const string PartScrollViewer = "PART_ScrollViewer";

    /// <summary>Template part: the horizontal scroll bar.</summary>
    protected const string PartHorizontalScrollBar = "PART_HorizontalScrollBar";

    /// <summary>Template part: the column headers presenter.</summary>
    protected const string PartColumnHeadersPresenter = "PART_ColumnHeadersPresenter";

    /// <summary>Template part: the group panel (drag-to-group drop target).</summary>
    protected const string PartGroupPanel = "PART_GroupPanel";

    /// <summary>Template part: the optional filter row presenter.</summary>
    protected const string PartFilterRowPresenter = "PART_FilterRowPresenter";

    /// <summary>Template part: the summary footer presenter.</summary>
    protected const string PartSummaryRowPresenter = "PART_SummaryRowPresenter";

    /// <summary>Template part: the centered message shown when there are no rows.</summary>
    protected const string PartEmptyState = "PART_EmptyState";

    /// <summary>Identifies the <see cref="ItemsSource"/> dependency property.</summary>
    public new static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(GridControl),
        new PropertyMetadata(null, OnItemsSourceChanged));

    /// <summary>Identifies the <see cref="AutoGenerateColumns"/> dependency property.</summary>
    public static readonly DependencyProperty AutoGenerateColumnsProperty = DependencyProperty.Register(
        nameof(AutoGenerateColumns), typeof(bool), typeof(GridControl),
        new PropertyMetadata(GridColumnBase.Boxed.BoolTrue, OnAutoGenerateColumnsChanged));

    /// <summary>Identifies the <see cref="CanUserSortColumns"/> dependency property.</summary>
    public static readonly DependencyProperty CanUserSortColumnsProperty = DependencyProperty.Register(
        nameof(CanUserSortColumns), typeof(bool), typeof(GridControl),
        new PropertyMetadata(GridColumnBase.Boxed.BoolTrue));

    /// <summary>Identifies the <see cref="CanUserResizeColumns"/> dependency property.</summary>
    public static readonly DependencyProperty CanUserResizeColumnsProperty = DependencyProperty.Register(
        nameof(CanUserResizeColumns), typeof(bool), typeof(GridControl),
        new PropertyMetadata(GridColumnBase.Boxed.BoolTrue));

    /// <summary>Identifies the <see cref="CanUserReorderColumns"/> dependency property.</summary>
    public static readonly DependencyProperty CanUserReorderColumnsProperty = DependencyProperty.Register(
        nameof(CanUserReorderColumns), typeof(bool), typeof(GridControl),
        new PropertyMetadata(GridColumnBase.Boxed.BoolTrue));

    /// <summary>Identifies the <see cref="FrozenColumnCount"/> dependency property.</summary>
    public static readonly DependencyProperty FrozenColumnCountProperty = DependencyProperty.Register(
        nameof(FrozenColumnCount), typeof(int), typeof(GridControl),
        new PropertyMetadata(0, OnFrozenColumnCountChanged), ValidateFrozenColumnCount);

    /// <summary>Identifies the <see cref="RowHeight"/> dependency property.</summary>
    public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(
        nameof(RowHeight), typeof(double), typeof(GridControl),
        new PropertyMetadata(double.NaN, OnRowMetricsChanged));

    /// <summary>Identifies the <see cref="HeaderHeight"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderHeightProperty = DependencyProperty.Register(
        nameof(HeaderHeight), typeof(double), typeof(GridControl),
        new PropertyMetadata(double.NaN, OnLayoutPropertyChanged));

    /// <summary>Identifies the <see cref="RowBackground"/> dependency property.</summary>
    public static readonly DependencyProperty RowBackgroundProperty = DependencyProperty.Register(
        nameof(RowBackground), typeof(Brush), typeof(GridControl), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="AlternatingRowBackground"/> dependency property.</summary>
    public static readonly DependencyProperty AlternatingRowBackgroundProperty = DependencyProperty.Register(
        nameof(AlternatingRowBackground), typeof(Brush), typeof(GridControl), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="GridLinesBrush"/> dependency property.</summary>
    public static readonly DependencyProperty GridLinesBrushProperty = DependencyProperty.Register(
        nameof(GridLinesBrush), typeof(Brush), typeof(GridControl), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="ShowColumnHeaders"/> dependency property.</summary>
    public static readonly DependencyProperty ShowColumnHeadersProperty = DependencyProperty.Register(
        nameof(ShowColumnHeaders), typeof(bool), typeof(GridControl),
        new PropertyMetadata(GridColumnBase.Boxed.BoolTrue, OnLayoutPropertyChanged));

    /// <summary>Identifies the <see cref="ShowColumnSeparators"/> dependency property.</summary>
    public static readonly DependencyProperty ShowColumnSeparatorsProperty = DependencyProperty.Register(
        nameof(ShowColumnSeparators), typeof(bool), typeof(GridControl),
        new PropertyMetadata(GridColumnBase.Boxed.BoolFalse, OnColumnSeparatorsChanged));

    /// <summary>Identifies the <see cref="ColumnSeparatorBrush"/> dependency property.</summary>
    public static readonly DependencyProperty ColumnSeparatorBrushProperty = DependencyProperty.Register(
        nameof(ColumnSeparatorBrush), typeof(Brush), typeof(GridControl),
        new PropertyMetadata(null, OnColumnSeparatorsChanged));

    /// <summary>Identifies the <see cref="ShowFilterRow"/> dependency property.</summary>
    public static readonly DependencyProperty ShowFilterRowProperty = DependencyProperty.Register(
        nameof(ShowFilterRow), typeof(bool), typeof(GridControl),
        new PropertyMetadata(GridColumnBase.Boxed.BoolFalse, OnLayoutPropertyChanged));

    /// <summary>Identifies the <see cref="UseAsyncRefresh"/> dependency property.</summary>
    public static readonly DependencyProperty UseAsyncRefreshProperty = DependencyProperty.Register(
        nameof(UseAsyncRefresh), typeof(bool), typeof(GridControl),
        new PropertyMetadata(GridColumnBase.Boxed.BoolFalse));

    /// <summary>Identifies the <see cref="ShowGroupPanel"/> dependency property.</summary>
    public static readonly DependencyProperty ShowGroupPanelProperty = DependencyProperty.Register(
        nameof(ShowGroupPanel), typeof(bool), typeof(GridControl),
        new PropertyMetadata(GridColumnBase.Boxed.BoolTrue, OnLayoutPropertyChanged));

    /// <summary>Identifies the <see cref="CanUserGroupColumns"/> dependency property.</summary>
    public static readonly DependencyProperty CanUserGroupColumnsProperty = DependencyProperty.Register(
        nameof(CanUserGroupColumns), typeof(bool), typeof(GridControl),
        new PropertyMetadata(GridColumnBase.Boxed.BoolTrue));

    /// <summary>Identifies the <see cref="RowDetailsTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty RowDetailsTemplateProperty = DependencyProperty.Register(
        nameof(RowDetailsTemplate), typeof(DataTemplate), typeof(GridControl),
        new PropertyMetadata(null, OnRowDetailsChanged));

    /// <summary>Identifies the <see cref="RowDetailsVisibilityMode"/> dependency property.</summary>
    public static readonly DependencyProperty RowDetailsVisibilityModeProperty = DependencyProperty.Register(
        nameof(RowDetailsVisibilityMode), typeof(GridRowDetailsVisibilityMode), typeof(GridControl),
        new PropertyMetadata(GridRowDetailsVisibilityMode.Collapsed, OnRowDetailsChanged));

    /// <summary>Identifies the <see cref="SelectionMode"/> dependency property.</summary>
    public static readonly DependencyProperty SelectionModeProperty = DependencyProperty.Register(
        nameof(SelectionMode), typeof(GridSelectionMode), typeof(GridControl),
        new PropertyMetadata(GridSelectionMode.Extended, OnSelectionModeChanged));

    /// <summary>Identifies the <see cref="SelectedItem"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
        nameof(SelectedItem), typeof(object), typeof(GridControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

    /// <summary>Identifies the <see cref="CurrentItem"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentItemProperty = DependencyProperty.Register(
        nameof(CurrentItem), typeof(object), typeof(GridControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCurrentItemChanged));

    /// <summary>Identifies the <see cref="IsReadOnly"/> dependency property.</summary>
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly), typeof(bool), typeof(GridControl),
        new PropertyMetadata(GridColumnBase.Boxed.BoolFalse));

    /// <summary>Identifies the <see cref="CanUserAddRows"/> dependency property.</summary>
    public static readonly DependencyProperty CanUserAddRowsProperty = DependencyProperty.Register(
        nameof(CanUserAddRows), typeof(bool), typeof(GridControl),
        new PropertyMetadata(GridColumnBase.Boxed.BoolFalse));

    /// <summary>Identifies the <see cref="CanUserDeleteRows"/> dependency property.</summary>
    public static readonly DependencyProperty CanUserDeleteRowsProperty = DependencyProperty.Register(
        nameof(CanUserDeleteRows), typeof(bool), typeof(GridControl),
        new PropertyMetadata(GridColumnBase.Boxed.BoolTrue));

    /// <summary>Identifies the <see cref="ClipboardCopyMode"/> dependency property.</summary>
    public static readonly DependencyProperty ClipboardCopyModeProperty = DependencyProperty.Register(
        nameof(ClipboardCopyMode), typeof(GridClipboardCopyMode), typeof(GridControl),
        new PropertyMetadata(GridClipboardCopyMode.IncludeHeaders));

    private static readonly DependencyPropertyKey HorizontalOffsetPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HorizontalOffset), typeof(double), typeof(GridControl), new PropertyMetadata(0d, OnHorizontalOffsetChanged));

    /// <summary>Identifies the read-only <see cref="HorizontalOffset"/> dependency property.</summary>
    public static readonly DependencyProperty HorizontalOffsetProperty = HorizontalOffsetPropertyKey.DependencyProperty;

    private readonly List<GridRow> _rowRegistry = new();
    private IReadOnlyList<GridColumnBase> _visibleColumns = Array.Empty<GridColumnBase>();
    private bool _syncingItems;
    private bool _autoColumnsGenerated;
    private double _lastConstraintWidth;
    private System.Windows.Controls.TextBlock? _emptyState;

    static GridControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GridControl),
            new FrameworkPropertyMetadata(typeof(GridControl)));
        FocusableProperty.OverrideMetadata(typeof(GridControl), new FrameworkPropertyMetadata(true));
    }

    /// <summary>Creates the grid.</summary>
    public GridControl()
    {
        Columns = new GridColumnCollection();
        Columns.StructureChanged += OnColumnsStructureChanged;

        SortDescriptions = new ObservableCollection<GridSortDescription>();
        SortDescriptions.CollectionChanged += OnSortDescriptionsChanged;

        FilterDescriptions = new ObservableCollection<GridFilterDescription>();
        FilterDescriptions.CollectionChanged += OnFilterDescriptionsChanged;

        GroupDescriptions = new ObservableCollection<GridGroupDescription>();
        GroupDescriptions.CollectionChanged += OnGroupDescriptionsChanged;

        SummaryItems = new ObservableCollection<GridSummaryItem>();
        SummaryItems.CollectionChanged += OnSummaryItemsChanged;

        DataEngine = new GridDataManager();
        DataEngine.ViewReset += OnEngineViewReset;
        DataEngine.View.CollectionChanged += OnViewCollectionChanged;

        Layout = new GridLayoutController();
        AlternationCount = 2;

        CommandBindings.Add(new CommandBinding(GridCommands.BeginEdit,
            (s, e) => e.Handled = BeginEdit(), (s, e) => e.CanExecute = CanBeginEdit()));
        CommandBindings.Add(new CommandBinding(GridCommands.CancelEdit,
            (s, e) => e.Handled = CancelEdit(), (s, e) => e.CanExecute = IsEditing));
        CommandBindings.Add(new CommandBinding(GridCommands.CommitEdit,
            (s, e) => e.Handled = CommitEdit(), (s, e) => e.CanExecute = IsEditing));
        CommandBindings.Add(new CommandBinding(GridCommands.Delete,
            (s, e) => { e.Handled = DeleteSelectedItems(); }, (s, e) => e.CanExecute = CanDeleteSelectedItems()));
        CommandBindings.Add(new CommandBinding(GridCommands.SelectAll,
            (s, e) => { SelectAll(); e.Handled = true; },
            (s, e) => e.CanExecute = SelectionMode == GridSelectionMode.Extended));
        CommandBindings.Add(new CommandBinding(GridCommands.Copy,
            (s, e) => { CopyToClipboard(); e.Handled = true; },
            (s, e) => e.CanExecute = ClipboardCopyMode != GridClipboardCopyMode.None));
    }

    /// <summary>Identifies the <see cref="AutoGeneratingColumn"/> routed event.</summary>
    public static readonly RoutedEvent AutoGeneratingColumnEvent = EventManager.RegisterRoutedEvent(
        nameof(AutoGeneratingColumn), RoutingStrategy.Direct,
        typeof(EventHandler<GridAutoGeneratingColumnEventArgs>), typeof(GridControl));

    /// <summary>Identifies the <see cref="Sorting"/> routed event.</summary>
    public static readonly RoutedEvent SortingEvent = EventManager.RegisterRoutedEvent(
        nameof(Sorting), RoutingStrategy.Direct,
        typeof(EventHandler<GridSortingEventArgs>), typeof(GridControl));

    /// <summary>Identifies the <see cref="Sorted"/> routed event.</summary>
    public static readonly RoutedEvent SortedEvent = EventManager.RegisterRoutedEvent(
        nameof(Sorted), RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(GridControl));

    /// <summary>Identifies the <see cref="SelectionChanged"/> routed event.</summary>
    public static readonly RoutedEvent SelectionChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(SelectionChanged), RoutingStrategy.Direct,
        typeof(EventHandler<GridSelectionChangedEventArgs>), typeof(GridControl));

    /// <summary>Identifies the <see cref="CellEditStarting"/> routed event.</summary>
    public static readonly RoutedEvent CellEditStartingEvent = EventManager.RegisterRoutedEvent(
        nameof(CellEditStarting), RoutingStrategy.Direct,
        typeof(EventHandler<GridCellEditEventArgs>), typeof(GridControl));

    /// <summary>Identifies the <see cref="CellEditEnding"/> routed event.</summary>
    public static readonly RoutedEvent CellEditEndingEvent = EventManager.RegisterRoutedEvent(
        nameof(CellEditEnding), RoutingStrategy.Direct,
        typeof(EventHandler<GridCellEditEventArgs>), typeof(GridControl));

    /// <summary>Identifies the <see cref="CellEditEnded"/> routed event.</summary>
    public static readonly RoutedEvent CellEditEndedEvent = EventManager.RegisterRoutedEvent(
        nameof(CellEditEnded), RoutingStrategy.Direct,
        typeof(EventHandler<GridCellEditEventArgs>), typeof(GridControl));

    /// <summary>Occurs before a column is auto-generated; allows replacing or cancelling it.</summary>
    public event EventHandler<GridAutoGeneratingColumnEventArgs> AutoGeneratingColumn
    {
        add => AddHandler(AutoGeneratingColumnEvent, value);
        remove => RemoveHandler(AutoGeneratingColumnEvent, value);
    }

    /// <summary>Occurs before sorting is applied; cancellable.</summary>
    public event EventHandler<GridSortingEventArgs> Sorting
    {
        add => AddHandler(SortingEvent, value);
        remove => RemoveHandler(SortingEvent, value);
    }

    /// <summary>Occurs after sorting has been applied.</summary>
    public event RoutedEventHandler Sorted
    {
        add => AddHandler(SortedEvent, value);
        remove => RemoveHandler(SortedEvent, value);
    }

    /// <summary>Occurs after the selection changed.</summary>
    public event EventHandler<GridSelectionChangedEventArgs> SelectionChanged
    {
        add => AddHandler(SelectionChangedEvent, value);
        remove => RemoveHandler(SelectionChangedEvent, value);
    }

    /// <summary>Occurs after the current cell moved.</summary>
    public event EventHandler? CurrentCellChanged;

    /// <summary>Occurs before a cell enters edit mode; cancellable.</summary>
    public event EventHandler<GridCellEditEventArgs> CellEditStarting
    {
        add => AddHandler(CellEditStartingEvent, value);
        remove => RemoveHandler(CellEditStartingEvent, value);
    }

    /// <summary>Occurs before an edit is committed or cancelled; cancellable.</summary>
    public event EventHandler<GridCellEditEventArgs> CellEditEnding
    {
        add => AddHandler(CellEditEndingEvent, value);
        remove => RemoveHandler(CellEditEndingEvent, value);
    }

    /// <summary>Occurs after an edit was committed or cancelled.</summary>
    public event EventHandler<GridCellEditEventArgs> CellEditEnded
    {
        add => AddHandler(CellEditEndedEvent, value);
        remove => RemoveHandler(CellEditEndedEvent, value);
    }

    /// <summary>The data source. Any <see cref="IEnumerable"/>; changes are tracked when it implements <see cref="INotifyCollectionChanged"/>.</summary>
    public new IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Manual column definitions. Auto-generated when left empty and <see cref="AutoGenerateColumns"/> is on.</summary>
    public GridColumnCollection Columns { get; }

    /// <summary>Whether columns are generated from the item type when <see cref="Columns"/> is empty.</summary>
    public bool AutoGenerateColumns
    {
        get => (bool)GetValue(AutoGenerateColumnsProperty);
        set => SetValue(AutoGenerateColumnsProperty, value);
    }

    /// <summary>Whether the user can sort by clicking column headers.</summary>
    public bool CanUserSortColumns
    {
        get => (bool)GetValue(CanUserSortColumnsProperty);
        set => SetValue(CanUserSortColumnsProperty, value);
    }

    /// <summary>Whether the user can resize columns by dragging header edges.</summary>
    public bool CanUserResizeColumns
    {
        get => (bool)GetValue(CanUserResizeColumnsProperty);
        set => SetValue(CanUserResizeColumnsProperty, value);
    }

    /// <summary>Whether the user can reorder columns by dragging headers.</summary>
    public bool CanUserReorderColumns
    {
        get => (bool)GetValue(CanUserReorderColumnsProperty);
        set => SetValue(CanUserReorderColumnsProperty, value);
    }

    /// <summary>Number of leading columns that stay visible during horizontal scrolling.</summary>
    public int FrozenColumnCount
    {
        get => (int)GetValue(FrozenColumnCountProperty);
        set => SetValue(FrozenColumnCountProperty, value);
    }

    /// <summary>Fixed row height; <see cref="double.NaN"/> sizes rows to content.</summary>
    public double RowHeight
    {
        get => (double)GetValue(RowHeightProperty);
        set => SetValue(RowHeightProperty, value);
    }

    /// <summary>Fixed header height; <see cref="double.NaN"/> sizes the header to content.</summary>
    public double HeaderHeight
    {
        get => (double)GetValue(HeaderHeightProperty);
        set => SetValue(HeaderHeightProperty, value);
    }

    /// <summary>Brush of even data rows.</summary>
    public Brush? RowBackground
    {
        get => (Brush?)GetValue(RowBackgroundProperty);
        set => SetValue(RowBackgroundProperty, value);
    }

    /// <summary>Brush of alternating (odd) data rows.</summary>
    public Brush? AlternatingRowBackground
    {
        get => (Brush?)GetValue(AlternatingRowBackgroundProperty);
        set => SetValue(AlternatingRowBackgroundProperty, value);
    }

    /// <summary>Brush of horizontal grid lines between rows.</summary>
    public Brush? GridLinesBrush
    {
        get => (Brush?)GetValue(GridLinesBrushProperty);
        set => SetValue(GridLinesBrushProperty, value);
    }

    /// <summary>Whether the column headers row is visible.</summary>
    public bool ShowColumnHeaders
    {
        get => (bool)GetValue(ShowColumnHeadersProperty);
        set => SetValue(ShowColumnHeadersProperty, value);
    }

    /// <summary>
    /// Whether vertical lines separating the columns are drawn inside the rows
    /// (in addition to the horizontal row separators). Default off.
    /// </summary>
    public bool ShowColumnSeparators
    {
        get => (bool)GetValue(ShowColumnSeparatorsProperty);
        set => SetValue(ShowColumnSeparatorsProperty, value);
    }

    /// <summary>Brush of the vertical column separators; defaults to <see cref="GridLinesBrush"/>.</summary>
    public Brush? ColumnSeparatorBrush
    {
        get => (Brush?)GetValue(ColumnSeparatorBrushProperty);
        set => SetValue(ColumnSeparatorBrushProperty, value);
    }

    private static void OnColumnSeparatorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        foreach (var row in ((GridControl)d)._rowRegistry)
        {
            row.CellsPanel?.InvalidateVisual();
        }
    }

    /// <summary>Whether the filter row under the headers is visible.</summary>
    public bool ShowFilterRow
    {
        get => (bool)GetValue(ShowFilterRowProperty);
        set => SetValue(ShowFilterRowProperty, value);
    }

    /// <summary>
    /// When <see langword="true"/>, sorting/filtering/grouping changes rebuild the view on a
    /// background thread and apply the result on the UI thread (keeps the UI responsive on
    /// large data sets). Incremental collection changes are processed without full rebuilds
    /// regardless of this setting.
    /// </summary>
    public bool UseAsyncRefresh
    {
        get => (bool)GetValue(UseAsyncRefreshProperty);
        set => SetValue(UseAsyncRefreshProperty, value);
    }

    /// <summary>Whether the group panel (drop target for column headers) is shown above the headers.</summary>
    public bool ShowGroupPanel
    {
        get => (bool)GetValue(ShowGroupPanelProperty);
        set => SetValue(ShowGroupPanelProperty, value);
    }

    /// <summary>Whether the user can group by dragging column headers into the group panel.</summary>
    public bool CanUserGroupColumns
    {
        get => (bool)GetValue(CanUserGroupColumnsProperty);
        set => SetValue(CanUserGroupColumnsProperty, value);
    }

    /// <summary>Template rendered under each data row's cells; data context is the row's item.</summary>
    public DataTemplate? RowDetailsTemplate
    {
        get => (DataTemplate?)GetValue(RowDetailsTemplateProperty);
        set => SetValue(RowDetailsTemplateProperty, value);
    }

    /// <summary>When the row details are displayed.</summary>
    public GridRowDetailsVisibilityMode RowDetailsVisibilityMode
    {
        get => (GridRowDetailsVisibilityMode)GetValue(RowDetailsVisibilityModeProperty);
        set => SetValue(RowDetailsVisibilityModeProperty, value);
    }

    /// <summary>Single or extended (multi) selection.</summary>
    public GridSelectionMode SelectionMode
    {
        get => (GridSelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    /// <summary>The selected data item (first of <see cref="SelectedItems"/>).</summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>The data item of the current cell.</summary>
    public object? CurrentItem
    {
        get => GetValue(CurrentItemProperty);
        set => SetValue(CurrentItemProperty, value);
    }

    /// <summary>Whether cells can be edited.</summary>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>Aggregate definitions shown in the footer row.</summary>
    public ObservableCollection<GridSummaryItem> SummaryItems { get; }

    /// <summary>Whether a new-row entry is offered (requires creatable item types).</summary>
    public bool CanUserAddRows
    {
        get => (bool)GetValue(CanUserAddRowsProperty);
        set => SetValue(CanUserAddRowsProperty, value);
    }

    /// <summary>Whether the user can delete rows with the Delete key.</summary>
    public bool CanUserDeleteRows
    {
        get => (bool)GetValue(CanUserDeleteRowsProperty);
        set => SetValue(CanUserDeleteRowsProperty, value);
    }

    /// <summary>Clipboard copy behavior.</summary>
    public GridClipboardCopyMode ClipboardCopyMode
    {
        get => (GridClipboardCopyMode)GetValue(ClipboardCopyModeProperty);
        set => SetValue(ClipboardCopyModeProperty, value);
    }

    /// <summary>Current horizontal scroll offset in pixels.</summary>
    public double HorizontalOffset => (double)GetValue(HorizontalOffsetProperty);

    /// <summary>Active sort levels, outermost first.</summary>
    public ObservableCollection<GridSortDescription> SortDescriptions { get; }

    /// <summary>Active filters (logical AND).</summary>
    public ObservableCollection<GridFilterDescription> FilterDescriptions { get; }

    /// <summary>Active grouping levels, outermost first.</summary>
    public ObservableCollection<GridGroupDescription> GroupDescriptions { get; }

    /// <summary>The UI-free data engine (exposed for advanced scenarios and export).</summary>
    public GridDataManager DataEngine { get; }

    /// <summary>Currently visible columns in display order.</summary>
    public IReadOnlyList<GridColumnBase> VisibleColumns => _visibleColumns;

    /// <summary>The layout controller (internal use).</summary>
    internal GridLayoutController Layout { get; }

    /// <summary>The vertical scroll host (template part).</summary>
    internal ScrollViewer? ScrollHost { get; private set; }

    /// <summary>The horizontal scroll bar (template part).</summary>
    internal ScrollBar? HorizontalScrollBar { get; private set; }

    /// <summary>The headers presenter (template part).</summary>
    internal GridColumnHeadersPresenter? HeadersPresenter { get; private set; }

    /// <summary>The group panel (template part).</summary>
    internal GridGroupPanel? GroupPanel { get; private set; }

    private Primitives.GridColumnFilterMenu? _openFilterMenu;

    /// <summary>The filter row presenter (template part).</summary>
    internal GridFilterRowPresenter? FilterRowPresenter { get; private set; }

    /// <summary>The summary footer presenter (template part).</summary>
    internal GridSummaryRowPresenter? SummaryRowPresenter { get; private set; }

    /// <summary>Brush used for the current cell border (theme).</summary>
    internal Brush? CurrentCellBorderBrush => TryFindResource(GridResources.CurrentCellBorderBrushKey) as Brush;

    /// <summary>The column of the current cell, or <see langword="null"/>.</summary>
    public GridColumnBase? CurrentColumn { get; internal set; }

    /// <summary>The available content width for columns.</summary>
    internal double ViewportWidth
    {
        get
        {
            if (ScrollHost is { } scrollHost)
            {
                var width = scrollHost.ViewportWidth;
                if (!double.IsNaN(width) && !double.IsInfinity(width) && width > 0)
                {
                    return width;
                }
            }

            if (!double.IsNaN(ActualWidth) && ActualWidth > 0)
            {
                return ActualWidth;
            }

            return _lastConstraintWidth;
        }
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (ScrollHost is not null)
        {
            ScrollHost.ScrollChanged -= OnScrollChanged;
        }

        if (HorizontalScrollBar is not null)
        {
            HorizontalScrollBar.Scroll -= OnHorizontalScrollBarScroll;
        }

        ScrollHost = GetTemplateChild(PartScrollViewer) as ScrollViewer;
        HorizontalScrollBar = GetTemplateChild(PartHorizontalScrollBar) as ScrollBar;
        HeadersPresenter = GetTemplateChild(PartColumnHeadersPresenter) as GridColumnHeadersPresenter;
        FilterRowPresenter = GetTemplateChild(PartFilterRowPresenter) as GridFilterRowPresenter;
        SummaryRowPresenter = GetTemplateChild(PartSummaryRowPresenter) as GridSummaryRowPresenter;
        GroupPanel = GetTemplateChild(PartGroupPanel) as GridGroupPanel;
        if (GroupPanel is not null)
        {
            GroupPanel.Owner = this;
        }

        if (ScrollHost is not null)
        {
            ScrollHost.ScrollChanged += OnScrollChanged;
        }

        if (HorizontalScrollBar is not null)
        {
            HorizontalScrollBar.Scroll += OnHorizontalScrollBarScroll;
        }

        if (HeadersPresenter is not null)
        {
            HeadersPresenter.Owner = this;
            HeadersPresenter.EnsureHeaders();
        }

        _emptyState = GetTemplateChild(PartEmptyState) as System.Windows.Controls.TextBlock;
        UpdateEmptyState();

        FilterRowPresenter?.EnsureEditors();
        if (FilterRowPresenter is not null)
        {
            FilterRowPresenter.Owner = this;
        }

        SummaryRowPresenter?.EnsureCells();
        if (SummaryRowPresenter is not null)
        {
            SummaryRowPresenter.Owner = this;
        }

        RefreshVisibleColumns(rebuildCells: true);
    }

    /// <inheritdoc />
    protected override System.Windows.Automation.Peers.AutomationPeer? OnCreateAutomationPeer()
        => new Automation.GridControlAutomationPeer(this);

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() => new GridRow();

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item) => false;

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);

        if (element is not GridRow row)
        {
            return;
        }

        if (!_rowRegistry.Contains(row))
        {
            _rowRegistry.Add(row);
        }

        if (item is GridRowInfo rowInfo)
        {
            row.Prepare(rowInfo, this);
        }

        row.Background = ItemsControl.GetAlternationIndex(row) % 2 == 1
            ? AlternatingRowBackground ?? RowBackground
            : RowBackground;
    }

    /// <inheritdoc />
    protected override void ClearContainerForItemOverride(DependencyObject element, object item)
    {
        base.ClearContainerForItemOverride(element, item);
        if (element is GridRow row)
        {
            _rowRegistry.Remove(row);
            row.Clear();
        }
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size constraint)
    {
        if (!double.IsInfinity(constraint.Width))
        {
            _lastConstraintWidth = constraint.Width;
        }

        var size = base.MeasureOverride(constraint);
        EnsureLayout();
        UpdateHorizontalScrollBar();
        return size;
    }

    /// <summary>Recomputes the visible column list; rebuilds headers and cells when columns change structurally.</summary>
    private void RefreshVisibleColumns(bool rebuildCells)
    {
        _visibleColumns = Columns.Where(c => c.Visible).ToArray();
        foreach (var column in Columns)
        {
            column.Owner = this;
        }

        HeadersPresenter?.PruneHiddenHeaders();
        HeadersPresenter?.EnsureHeaders();
        FilterRowPresenter?.EnsureEditors();
        SummaryRowPresenter?.EnsureCells();

        if (rebuildCells)
        {
            foreach (var row in _rowRegistry)
            {
                row.CellsPanel?.EnsureCells();
                row.CellsPanel?.PrepareCells();
            }
        }

        InvalidateLayout();
    }

    /// <summary>Recomputes layout slots and refreshes everything that depends on them.</summary>
    internal void InvalidateLayout()
    {
        EnsureLayout();
        HeadersPresenter?.InvalidateMeasure();
        foreach (var row in _rowRegistry)
        {
            // InvalidateVisual (not just Measure): the panel's RenderSize does not change on
            // reorder/resize, and OnRender-drawn separators would otherwise stay at the old
            // column boundaries.
            row.CellsPanel?.InvalidateVisual();
        }

        UpdateHorizontalScrollBar();
    }

    private void EnsureLayout()
    {
        Layout.ViewportWidth = ViewportWidth;
        Layout.FrozenColumnCount = FrozenColumnCount;
        Layout.Compute(_visibleColumns);
    }

    /// <summary>Called by cell/header panels when auto-width hints grew.</summary>
    internal void NotifyAutoWidthsChanged()
    {
        EnsureLayout();
        HeadersPresenter?.InvalidateMeasure();
        foreach (var row in _rowRegistry)
        {
            row.CellsPanel?.InvalidateArrange();
        }

        UpdateHorizontalScrollBar();
    }

    private void UpdateHorizontalScrollBar()
    {
        if (HorizontalScrollBar is not { } scrollBar)
        {
            return;
        }

        var viewport = ViewportWidth;
        var max = Math.Max(0, Layout.TotalWidth - viewport);
        scrollBar.ViewportSize = double.IsNaN(viewport) || double.IsInfinity(viewport) ? 0 : viewport;
        scrollBar.Maximum = max;
        scrollBar.LargeChange = Math.Max(1, viewport * 0.9);
        scrollBar.SmallChange = 16;
        scrollBar.Visibility = max > 0.5 ? Visibility.Visible : Visibility.Collapsed;
        if (scrollBar.Value > max)
        {
            SetHorizontalOffsetCore(max);
        }
    }

    /// <summary>Scrolls horizontally to the given offset (clamped).</summary>
    internal void SetHorizontalOffsetForTest(double value) => SetHorizontalOffsetCore(value);

    internal void SetHorizontalOffsetCore(double value)
    {
        var max = Math.Max(0, Layout.TotalWidth - ViewportWidth);
        value = Math.Clamp(value, 0, max);
        if (Math.Abs(value - HorizontalOffset) < 0.01)
        {
            return;
        }

        SetValue(HorizontalOffsetPropertyKey, value); // read-only DP: must go through the key
    }

    private static void OnHorizontalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (GridControl)d;
        grid.HeadersPresenter?.InvalidateArrange();
        foreach (var row in grid._rowRegistry)
        {
            // Arrange alone keeps the cached OnRender output: separators drawn by the panel
            // would lag behind the scrolled columns. Only pay the re-render when they are on.
            if (grid.ShowColumnSeparators)
            {
                row.CellsPanel?.InvalidateVisual();
            }
            else
            {
                row.CellsPanel?.InvalidateArrange();
            }
        }

        if (grid.HorizontalScrollBar is { } scrollBar && Math.Abs(scrollBar.Value - (double)e.NewValue) > 0.01)
        {
            scrollBar.Value = (double)e.NewValue;
        }
    }

    private void OnHorizontalScrollBarScroll(object sender, ScrollEventArgs e)
    {
        SetHorizontalOffsetCore(((ScrollBar)sender).Value);
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (Math.Abs(e.ViewportWidthChange) > 0.01)
        {
            InvalidateLayout();
        }
    }

    /// <summary>Scrolls so that the given data item becomes visible.</summary>
    public void ScrollIntoView(object item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var rowInfo = DataEngine.View.FirstOrDefault(row =>
            row is GridDataRow data && ReferenceEquals(data.Item, item));
        if (rowInfo is null)
        {
            return;
        }

        if (ItemContainerGenerator.ContainerFromItem(rowInfo) is GridRow row)
        {
            row.BringIntoView();
        }
    }

    /// <summary>Applies the current sort/filter/group settings and rebuilds the view synchronously.</summary>
    public virtual void Refresh()
    {
        PushEngineSettings();
        DataEngine.Refresh();
    }

    /// <summary>
    /// Rebuilds the data view, honoring <see cref="UseAsyncRefresh"/>. Derived controls that own
    /// their own view building (e.g. <c>TreeListView</c>) override this instead of the engine
    /// pipeline.
    /// </summary>
    protected virtual void RebuildDataView()
    {
        if (UseAsyncRefresh)
        {
            _settingsVersion++; // any running pipeline keeps going until it applies
                                // a snapshot taken at or after this version
            if (_asyncRefreshTask is not null && !_asyncRefreshTask.IsCompleted)
            {
                return;
            }

            _asyncRefreshTask = RunAsyncRefreshObserved(_settingsVersion);
            return;
        }

        DataEngine.Refresh();
    }

    /// <summary>Applies the settings and rebuilds the view (sync or async per <see cref="UseAsyncRefresh"/>).</summary>
    private void RefreshView()
    {
        PushEngineSettings();
        RebuildDataView();
    }

    private Task? _asyncRefreshTask;
    private int _settingsVersion;

    /// <summary>Last error of the background pipeline (surfaced to tests/diagnostics; otherwise swallowed by fire-and-forget).</summary>
    public Exception? LastAsyncRefreshError { get; private set; }

    private async Task RunAsyncRefreshObserved(int startVersion)
    {
        try
        {
            LastAsyncRefreshError = null;
            await RefreshViewAsyncCore(startVersion).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            // Fire-and-forget swallows everything; remember it so tests and diagnostics see the cause.
            LastAsyncRefreshError = exception;
            System.Diagnostics.Trace.TraceError($"GridControl async refresh failed: {exception}");
            throw;
        }
    }

    private async Task RefreshViewAsyncCore(int startVersion)
    {
        try
        {
            // Single in-flight pipeline (concurrent applies interleave view Resets with
            // container recycling and crash the virtualizing panel). The loop is
            // deterministic w.r.t. the final state: it keeps refreshing until a snapshot
            // taken at/after the LATEST requested settings version has been applied —
            // no reliance on flag timing on loaded machines.
            var attempt = 0;
            while (true)
            {
                var snapshotVersion = _settingsVersion;
                var result = await DataEngine.ComputeAsync().ConfigureAwait(false);

                // Marshal the apply onto the grid's dispatcher explicitly — never trust the
                // ambient SynchronizationContext (test hosts / runners may not install the
                // dispatcher context, and WPF CollectionView rejects cross-thread changes).
                var applied = await Dispatcher
                    .InvokeAsync(() => DataEngine.ApplyView(result), System.Windows.Threading.DispatcherPriority.Normal)
                    .Task
                    .ConfigureAwait(false);

                if (applied && snapshotVersion == _settingsVersion)
                {
                    return; // applied the latest requested state
                }

                if (applied)
                {
                    attempt = 0; // newer settings arrived while applying — recompute
                    continue;
                }

                if (++attempt >= 3)
                {
                    break;
                }
            }

            await Dispatcher
                .InvokeAsync(() => DataEngine.Refresh())
                .Task
                .ConfigureAwait(false); // sync fallback always reflects the latest state
        }
        finally
        {
            _asyncRefreshTask = null;
        }
    }

    private void PushEngineSettings()
    {
        DataEngine.Sorts = SortDescriptions;
        DataEngine.Filters = FilterDescriptions;
        DataEngine.Groups = GroupDescriptions;
    }

    private void OnViewCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Incremental view changes (single-item source Add/Remove): ItemsControl already
        // processed the collection change; keep selection, current item and summaries in sync.
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Remove when e.OldItems is not null:
                var removedItems = new List<object>();
                foreach (var rowInfo in e.OldItems)
                {
                    if (rowInfo is GridDataRow data && _selectedItems.Contains(data.Item))
                    {
                        removedItems.Add(data.Item);
                    }
                }

                foreach (var item in removedItems)
                {
                    _selectedItems.Remove(item);
                }

                if (removedItems.Count > 0)
                {
                    OnSelectionChanged(Array.Empty<object>(), removedItems);
                }

                RestoreCurrentItem();
                SummaryRowPresenter?.RefreshValues();
                UpdateEmptyState();
                break;

            case NotifyCollectionChangedAction.Add:
            case NotifyCollectionChangedAction.Remove:
                SummaryRowPresenter?.RefreshValues();
                UpdateEmptyState();
                break;
        }
    }

    private void SyncEngineView()
    {
        if (_syncingItems)
        {
            return;
        }

        _syncingItems = true;
        try
        {
            // Swap the whole view into the base ItemsSource: a single Reset instead of
            // per-item collection changes keeps large refreshes cheap.
            base.ItemsSource = DataEngine.View;
        }
        finally
        {
            _syncingItems = false;
        }
    }

    private void OnEngineViewReset(object? sender, EventArgs e)
    {
        if (!_autoColumnsGenerated
            && AutoGenerateColumns
            && Columns.Count == 0
            && DataEngine.ResolvedItemType is { } itemType)
        {
            GenerateAutoColumns(itemType);
        }

        SyncEngineView();
        PruneSelectionToSource();
        RestoreCurrentItem();
        InvalidateVisualChildren();
        SummaryRowPresenter?.RefreshValues();
        UpdateEmptyState();
    }

    /// <summary>Shows the localized empty-state message when the view has no rows.</summary>
    private void UpdateEmptyState()
    {
        if (_emptyState is not null)
        {
            _emptyState.Visibility = Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void InvalidateVisualChildren()
    {
        foreach (var row in _rowRegistry)
        {
            row.CellsPanel?.InvalidateMeasure();
        }
    }

    private void GenerateAutoColumns(Type itemType)
    {
        foreach (var column in ColumnAutoGenerator.GenerateColumns(itemType))
        {
            var args = new GridAutoGeneratingColumnEventArgs(column)
            {
                RoutedEvent = AutoGeneratingColumnEvent,
            };
            RaiseEvent(args);
            if (!args.Cancel)
            {
                if (!ReferenceEquals(args.Column, column))
                {
                    column.Owner = null;
                }

                Columns.Add(args.Column);
            }
        }

        _autoColumnsGenerated = Columns.Count > 0;
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (GridControl)d;
        grid._autoColumnsGenerated = false;

        if (grid.AutoGenerateColumns && grid.Columns.Count == 0 && e.NewValue is IEnumerable source)
        {
            if (TryGetItemType(source, out var itemType) && itemType is not null)
            {
                grid.GenerateAutoColumns(itemType);
            }
        }

        grid.DataEngine.SetSource((IEnumerable?)e.NewValue);
    }

    private static bool TryGetItemType(IEnumerable source, out Type? itemType)
    {
        itemType = null;
        if (source is IList { Count: > 0 } list)
        {
            itemType = list[0]?.GetType();
            return itemType is not null;
        }

        foreach (var item in source)
        {
            itemType = item?.GetType();
            return itemType is not null;
        }

        return false;
    }

    private static void OnAutoGenerateColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (GridControl)d;
        if ((bool)e.NewValue && grid.Columns.Count == 0 && grid.DataEngine.ResolvedItemType is { } itemType)
        {
            grid.GenerateAutoColumns(itemType);
        }
    }

    private static void OnFrozenColumnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (GridControl)d;
        grid.InvalidateLayout();
    }

    private static void OnRowDetailsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (GridControl)d;
        foreach (var row in grid._rowRegistry)
        {
            row.UpdateRowDetails();
        }
    }

    private static void OnRowMetricsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (GridControl)d;
        foreach (var row in grid._rowRegistry)
        {
            row.CellsPanel?.InvalidateMeasure();
            row.InvalidateMeasure();
        }
    }

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((GridControl)d).InvalidateLayout();
    }

    private void OnColumnsStructureChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            _autoColumnsGenerated = false;
        }

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is not null:
                foreach (GridColumnBase column in e.NewItems)
                {
                    column.Owner = this;
                }

                break;

            case NotifyCollectionChangedAction.Remove when e.OldItems is not null:
                foreach (GridColumnBase column in e.OldItems)
                {
                    column.Owner = null;
                }

                break;

            case NotifyCollectionChangedAction.Replace when e.OldItems is not null && e.NewItems is not null:
                foreach (GridColumnBase column in e.OldItems)
                {
                    column.Owner = null;
                }

                foreach (GridColumnBase column in e.NewItems)
                {
                    column.Owner = this;
                }

                break;

            case NotifyCollectionChangedAction.Reset:
            case NotifyCollectionChangedAction.Move:
                break;
        }

        RefreshVisibleColumns(rebuildCells: true);
    }

    /// <summary>Called by a column when one of its layout-relevant properties changed.</summary>
    internal void NotifyColumnChanged(GridColumnBase column)
    {
        if (!column.Visible)
        {
            RefreshVisibleColumns(rebuildCells: true);
            return;
        }

        var wasHidden = !_visibleColumns.Contains(column);
        if (wasHidden)
        {
            RefreshVisibleColumns(rebuildCells: true);
            return;
        }

        InvalidateLayout();
        foreach (var row in _rowRegistry)
        {
            row.CellsPanel?.InvalidateMeasure();
        }
    }

    /// <summary>Re-applies a column's <see cref="GridColumnBase.ElementStyle"/> to already generated cell elements.</summary>
    internal void ApplyElementStyleToCells(GridColumnBase column)
    {
        foreach (var row in _rowRegistry)
        {
            row.CellsPanel?.ApplyElementStyle(column);
        }
    }

    private void OnSortDescriptionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncSortStateToColumns();
        RefreshView();
    }

    private void OnFilterDescriptionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshView();
        SyncFilterIndicators();
    }

    private void OnGroupDescriptionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        GroupPanel?.EnsureChips();
        RefreshView();
    }

    private void SyncSortStateToColumns()
    {
        foreach (var column in Columns)
        {
            column.SortDirection = GridSortDirection.None;
        }

        foreach (var sort in SortDescriptions)
        {
            var column = Columns.FirstOrDefault(c => c.EffectiveSortPath == sort.FieldName);
            if (column is not null)
            {
                column.SortDirection = sort.Direction;
            }
        }

        HeadersPresenter?.EnsureHeaders();
    }

    private static bool ValidateFrozenColumnCount(object value) => value is int and >= 0;

    private void OnSummaryItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => SummaryRowPresenter?.RefreshValues();

    /// <summary>Expands or collapses a group; the flattened view is rebuilt.</summary>
    public void SetGroupExpanded(Data.GridGroup group, bool expanded)
    {
        ArgumentNullException.ThrowIfNull(group);
        DataEngine.SetGroupExpanded(group, expanded);
    }

    /// <summary>
    /// Applies a text filter for a column from the filter row: contains for text-like columns,
    /// equals (with conversion) otherwise. Empty text removes the filter.
    /// </summary>
    internal void ApplyFilterText(GridColumnBase column, string text)
    {
        var field = column.FieldName;
        var existing = FilterDescriptions.FirstOrDefault(f => f.FieldName == field);
        if (existing is not null)
        {
            FilterDescriptions.Remove(existing);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (column is GridTextColumn or GridTemplateColumn)
        {
            FilterDescriptions.Add(new GridFilterDescription(field, GridFilterOperator.Contains, text));
            return;
        }

        // Typed comparison: try converting to the column's value type.
        var itemType = DataEngine.ResolvedItemType;
        var targetType = itemType is null
            ? typeof(object)
            : PropertyAccessor.GetOrCreate(itemType, field).ValueType;
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        object? value = null;
        try
        {
            value = Convert.ChangeType(text, underlying, System.Globalization.CultureInfo.CurrentCulture);
        }
        catch (FormatException)
        {
            // Fall back to text contains.
        }
        catch (InvalidCastException)
        {
        }

        FilterDescriptions.Add(value is null
            ? new GridFilterDescription(field, GridFilterOperator.Contains, text)
            : new GridFilterDescription(field, GridFilterOperator.Equals, value));
    }

    /// <summary>Adds a grouping level for the column (the drag-to-group drop action).</summary>
    public void GroupByColumn(GridColumnBase column)
    {
        ArgumentNullException.ThrowIfNull(column);
        if (!CanUserGroupColumns)
        {
            return;
        }

        var field = column.EffectiveSortPath;
        if (GroupDescriptions.Any(g => string.Equals(g.FieldName, field, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        GroupDescriptions.Add(new GridGroupDescription(field));
    }

    /// <summary>Whether the given point (grid coordinates) lies over the group panel.</summary>
    internal bool IsPointOverGroupPanel(Point pointInGrid)
    {
        if (GroupPanel is not { } panel || !ShowGroupPanel || !CanUserGroupColumns)
        {
            return false;
        }

        var bounds = panel.TransformToAncestor(this).TransformBounds(new Rect(0, 0, panel.ActualWidth, panel.ActualHeight));
        return bounds.Contains(pointInGrid);
    }

    /// <summary>Refreshes the funnel highlight of headers for columns with an active filter.</summary>
    internal void SyncFilterIndicators()
    {
        if (HeadersPresenter is null)
        {
            return;
        }

        foreach (var column in VisibleColumns)
        {
            if (HeadersPresenter.GetHeader(column) is { } header)
            {
                header.IsFilterApplied = FilterDescriptions.Any(f =>
                    string.Equals(f.FieldName, column.FieldName, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    /// <summary>Opens the value-list filter menu anchored to the column's header.</summary>
    internal void OpenColumnFilterMenu(GridColumnBase column)
    {
        if (!column.CanUserFilter)
        {
            return;
        }

        _openFilterMenu?.Close();
        var menu = new Primitives.GridColumnFilterMenu(this, column);
        menu.Closed += (_, _) =>
        {
            if (ReferenceEquals(_openFilterMenu, menu))
            {
                _openFilterMenu = null;
            }
        };
        _openFilterMenu = menu;
        menu.Open();
    }
}
