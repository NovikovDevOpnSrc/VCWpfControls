using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using VCWpfControls.Grid.Columns;

namespace VCWpfControls.Grid.Primitives;

/// <summary>
/// The header cell of one column: click to sort, drag the right edge to resize,
/// drag the header to reorder the column.
/// </summary>
public class GridColumnHeader : Control
{
    /// <summary>Identifies the <see cref="Column"/> dependency property.</summary>
    public static readonly DependencyProperty ColumnProperty = DependencyProperty.Register(
        nameof(Column), typeof(GridColumnBase), typeof(GridColumnHeader),
        new PropertyMetadata(null, OnColumnChanged));

    /// <summary>Identifies the <see cref="SortDirection"/> dependency property.</summary>
    public static readonly DependencyProperty SortDirectionProperty = DependencyProperty.Register(
        nameof(SortDirection), typeof(Data.GridSortDirection), typeof(GridColumnHeader),
        new PropertyMetadata(Data.GridSortDirection.None));

    /// <summary>Identifies the <see cref="IsFrozen"/> dependency property.</summary>
    public static readonly DependencyProperty IsFrozenProperty = DependencyProperty.Register(
        nameof(IsFrozen), typeof(bool), typeof(GridColumnHeader), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsPressed"/> dependency property.</summary>
    public static readonly DependencyProperty IsPressedProperty = DependencyProperty.Register(
        nameof(IsPressed), typeof(bool), typeof(GridColumnHeader), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsDropTarget"/> dependency property.</summary>
    public static readonly DependencyProperty IsDropTargetProperty = DependencyProperty.Register(
        nameof(IsDropTarget), typeof(bool), typeof(GridColumnHeader), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsFilterApplied"/> dependency property.</summary>
    public static readonly DependencyProperty IsFilterAppliedProperty = DependencyProperty.Register(
        nameof(IsFilterApplied), typeof(bool), typeof(GridColumnHeader), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="ShowFilterButton"/> dependency property.</summary>
    public static readonly DependencyProperty ShowFilterButtonProperty = DependencyProperty.Register(
        nameof(ShowFilterButton), typeof(bool), typeof(GridColumnHeader), new PropertyMetadata(false));

    private const double DragThreshold = 4d;

    private Thumb? _resizeThumb;
    private Point _dragStartPoint;
    private bool _isDragging;
    private bool _mouseDownWithoutDrag;

    static GridColumnHeader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GridColumnHeader),
            new FrameworkPropertyMetadata(typeof(GridColumnHeader)));
    }

    /// <summary>The column presented by this header.</summary>
    public GridColumnBase? Column
    {
        get => (GridColumnBase?)GetValue(ColumnProperty);
        internal set => SetValue(ColumnProperty, value);
    }

    /// <summary>The sort direction currently applied to the column.</summary>
    public Data.GridSortDirection SortDirection
    {
        get => (Data.GridSortDirection)GetValue(SortDirectionProperty);
        internal set => SetValue(SortDirectionProperty, value);
    }

    /// <summary>Whether the column is frozen.</summary>
    public bool IsFrozen
    {
        get => (bool)GetValue(IsFrozenProperty);
        internal set => SetValue(IsFrozenProperty, value);
    }

    /// <summary>Whether the header is currently pressed.</summary>
    public bool IsPressed
    {
        get => (bool)GetValue(IsPressedProperty);
        private set => SetValue(IsPressedProperty, value);
    }

    /// <summary>Whether this header is the current drop target of a column reorder.</summary>
    public bool IsDropTarget
    {
        get => (bool)GetValue(IsDropTargetProperty);
        internal set => SetValue(IsDropTargetProperty, value);
    }

    /// <summary>Whether a filter is currently applied to the column (highlights the funnel).</summary>
    public bool IsFilterApplied
    {
        get => (bool)GetValue(IsFilterAppliedProperty);
        internal set => SetValue(IsFilterAppliedProperty, value);
    }

    /// <summary>Whether the filter (funnel) button is shown in this header.</summary>
    public bool ShowFilterButton
    {
        get => (bool)GetValue(ShowFilterButtonProperty);
        internal set => SetValue(ShowFilterButtonProperty, value);
    }

    /// <inheritdoc />
    protected override System.Windows.Automation.Peers.AutomationPeer? OnCreateAutomationPeer()
        => new Automation.GridColumnHeaderAutomationPeer(this);

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_resizeThumb is not null)
        {
            _resizeThumb.DragDelta -= OnResizeDelta;
            _resizeThumb.DragStarted -= OnResizeStarted;
            _resizeThumb.DragCompleted -= OnResizeCompleted;
        }

        if (_resizeThumb is not null)
        {
            _resizeThumb.DragDelta -= OnResizeDelta;
            _resizeThumb.DragStarted -= OnResizeStarted;
            _resizeThumb.DragCompleted -= OnResizeCompleted;
            _resizeThumb.MouseLeftButtonDown -= OnResizeThumbDoubleClick;
        }

        _resizeThumb = GetTemplateChild("PART_ResizeThumb") as Thumb;
        if (_resizeThumb is not null)
        {
            _resizeThumb.DragDelta += OnResizeDelta;
            _resizeThumb.DragStarted += OnResizeStarted;
            _resizeThumb.DragCompleted += OnResizeCompleted;
            _resizeThumb.MouseLeftButtonDown += OnResizeThumbDoubleClick;
        }

        if (GetTemplateChild("PART_FilterButton") is ToggleButton filterButton)
        {
            // Both paths: Click for the mouse AND Checked for UIA/keyboard activation
            // (the Toggle pattern flips IsChecked without raising Click - without the
            // Checked hook, assistive tech cannot open the filter menu at all).
            filterButton.Click += OnFilterButtonClick;
            filterButton.Checked += OnFilterButtonClick;
        }
    }

    private void OnFilterButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton { IsChecked: true } button)
        {
            button.IsChecked = false; // momentary action, not a persistent toggle state
            Owner?.OpenColumnFilterMenu(Column!);
        }
    }

    /// <inheritdoc />
    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonUp(e);
        if (Column is not null && Owner is not null)
        {
            Owner.OpenColumnHeaderMenu(Column, this);
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (Column is null)
        {
            return;
        }

        _dragStartPoint = e.GetPosition(this);
        _mouseDownWithoutDrag = true;
        IsPressed = true;
        CaptureMouse();
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!IsMouseCaptured || Column is null || Owner is null)
        {
            return;
        }

        var position = e.GetPosition(this);
        if (!_isDragging)
        {
            if (Math.Abs(position.X - _dragStartPoint.X) > DragThreshold
                && Owner.CanUserReorderColumns
                && Owner.VisibleColumns.Count > 1)
            {
                _isDragging = true;
                _mouseDownWithoutDrag = false;
                Cursor = Cursors.ScrollAll;
                Owner.NotifyColumnDragStarted(Column);
            }
        }

        if (_isDragging)
        {
            var pointInGrid = e.GetPosition(Owner);
            Owner.NotifyColumnDragOver(Column, pointInGrid);
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (Column is null)
        {
            return;
        }

        if (_isDragging)
        {
            var pointInGrid = Owner is null ? default : e.GetPosition(Owner);
            _isDragging = false;
            Cursor = Cursors.Arrow;
            Owner?.NotifyColumnDragCompleted(Column, pointInGrid);
        }
        else if (_mouseDownWithoutDrag)
        {
            Owner?.NotifyHeaderClick(Column, Keyboard.Modifiers);
        }

        _mouseDownWithoutDrag = false;
        IsPressed = false;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    internal GridControl? Owner => Column?.Owner;

    private void OnResizeThumbDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2 && Owner is { } owner && Column is { } column)
        {
            // Excel-style: double-clicking the splitter best-fits the column.
            owner.BestFitColumn(column);
            e.Handled = true;
        }
    }

    private void OnResizeStarted(object sender, DragStartedEventArgs e)
    {
        IsPressed = true;
    }

    private void OnResizeDelta(object sender, DragDeltaEventArgs e)
    {
        if (Column is null || Owner is null || !Owner.CanUserResizeColumns)
        {
            return;
        }

        var owner = Owner;
        if (owner is null)
        {
            return;
        }

        var slot = owner.Layout.GetSlot(Column);
        var currentWidth = slot?.Width ?? Column.ActualWidth;
        GridControl.ResizeColumn(Column, currentWidth + e.HorizontalChange);
    }

    private void OnResizeCompleted(object sender, DragCompletedEventArgs e)
    {
        IsPressed = false;
    }

    private static void OnColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var header = (GridColumnHeader)d;
        if (e.NewValue is GridColumnBase column)
        {
            // Content is delivered through template bindings to Column.*; refresh the glyph.
            header.SortDirection = column.SortDirection;
        }
    }
}
