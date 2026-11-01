using System.Windows;
using System.Windows.Controls;
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

    static GridRow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GridRow),
            new FrameworkPropertyMetadata(typeof(GridRow)));
        FocusableProperty.OverrideMetadata(typeof(GridRow), new PropertyMetadata(false));
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

    /// <summary>The row info this container presents.</summary>
    internal GridRowInfo? RowInfo { get; private set; }

    /// <summary>The bound data item (null for group rows).</summary>
    internal object? DataItem => RowInfo is GridDataRow data ? data.Item : null;

    /// <summary>The cells panel of the row (after the template has been applied).</summary>
    internal GridCellsPanel? CellsPanel { get; private set; }

    /// <summary>The owning grid.</summary>
    internal GridControl? Owner { get; private set; }

    /// <summary>Binds the container to a row info item of the grid's view.</summary>
    internal void Prepare(GridRowInfo rowInfo, GridControl owner)
    {
        RowInfo = rowInfo;
        Owner = owner;

        if (CellsPanel is not null)
        {
            CellsPanel.Attach(this, owner);
            CellsPanel.EnsureCells();
            CellsPanel.PrepareCells();
        }

        IsSelected = DataItem is not null && owner.IsItemSelected(DataItem);
        IsCurrent = DataItem is not null && ReferenceEquals(DataItem, owner.CurrentItem);
    }

    /// <summary>Re-selects cell elements after the template has been applied.</summary>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        CellsPanel = GetTemplateChild("PART_Cells") as GridCellsPanel;
        if (CellsPanel is not null && Owner is not null)
        {
            CellsPanel.Attach(this, Owner);
            CellsPanel.EnsureCells();
            CellsPanel.PrepareCells();
        }
    }

    /// <summary>Clears cell state when the container is discarded or recycled.</summary>
    internal void Clear()
    {
        CellsPanel?.ClearCells();
        IsSelected = false;
        IsCurrent = false;
        IsEditing = false;
        RowInfo = null;
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

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Visual state is driven by template triggers; nothing to do here.
    }
}
