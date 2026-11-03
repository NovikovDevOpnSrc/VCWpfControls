using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using VCWpfControls.Grid.Data;

namespace VCWpfControls.Grid.Columns;

/// <summary>
/// Base class for grid columns. A column knows how to create the display element and the
/// editing element for a cell and how to bind them to the row's data item via
/// <see cref="FieldName"/> (supports dotted paths such as <c>Address.City</c>).
/// </summary>
public abstract class GridColumnBase : DependencyObject
{
    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header), typeof(object), typeof(GridColumnBase), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="FieldName"/> dependency property.</summary>
    public static readonly DependencyProperty FieldNameProperty = DependencyProperty.Register(
        nameof(FieldName), typeof(string), typeof(GridColumnBase),
        new PropertyMetadata(string.Empty, OnColumnPropertyChanged));

    /// <summary>Identifies the <see cref="Width"/> dependency property.</summary>
    public static readonly DependencyProperty WidthProperty = DependencyProperty.Register(
        nameof(Width), typeof(GridColumnWidth), typeof(GridColumnBase),
        new PropertyMetadata(new GridColumnWidth(1, GridUnitType.Star), OnColumnPropertyChanged));

    /// <summary>Identifies the <see cref="MinWidth"/> dependency property.</summary>
    public static readonly DependencyProperty MinWidthProperty = DependencyProperty.Register(
        nameof(MinWidth), typeof(double), typeof(GridColumnBase),
        new PropertyMetadata(0d, OnColumnPropertyChanged, static (d, baseValue) => Math.Max(0d, (double)baseValue)));

    /// <summary>Identifies the <see cref="MaxWidth"/> dependency property.</summary>
    public static readonly DependencyProperty MaxWidthProperty = DependencyProperty.Register(
        nameof(MaxWidth), typeof(double), typeof(GridColumnBase),
        new PropertyMetadata(double.PositiveInfinity, OnColumnPropertyChanged,
            static (d, baseValue) => double.IsNaN((double)baseValue) ? double.PositiveInfinity : (double)baseValue));

    /// <summary>Identifies the <see cref="Visible"/> dependency property.</summary>
    public static readonly DependencyProperty VisibleProperty = DependencyProperty.Register(
        nameof(Visible), typeof(bool), typeof(GridColumnBase),
        new PropertyMetadata(Boxed.BoolTrue, OnColumnPropertyChanged));

    /// <summary>Identifies the <see cref="ReadOnly"/> dependency property.</summary>
    public static readonly DependencyProperty ReadOnlyProperty = DependencyProperty.Register(
        nameof(ReadOnly), typeof(bool), typeof(GridColumnBase), new PropertyMetadata(Boxed.BoolFalse));

    /// <summary>Identifies the <see cref="CanUserSort"/> dependency property.</summary>
    public static readonly DependencyProperty CanUserSortProperty = DependencyProperty.Register(
        nameof(CanUserSort), typeof(bool), typeof(GridColumnBase), new PropertyMetadata(Boxed.BoolTrue));

    /// <summary>Identifies the <see cref="CanUserFilter"/> dependency property.</summary>
    public static readonly DependencyProperty CanUserFilterProperty = DependencyProperty.Register(
        nameof(CanUserFilter), typeof(bool), typeof(GridColumnBase), new PropertyMetadata(Boxed.BoolTrue));

    /// <summary>Identifies the <see cref="SortMemberPath"/> dependency property.</summary>
    public static readonly DependencyProperty SortMemberPathProperty = DependencyProperty.Register(
        nameof(SortMemberPath), typeof(string), typeof(GridColumnBase), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="StringFormat"/> dependency property.</summary>
    public static readonly DependencyProperty StringFormatProperty = DependencyProperty.Register(
        nameof(StringFormat), typeof(string), typeof(GridColumnBase), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="HorizontalContentAlignment"/> dependency property.</summary>
    public static readonly DependencyProperty HorizontalContentAlignmentProperty = DependencyProperty.Register(
        nameof(HorizontalContentAlignment), typeof(HorizontalAlignment), typeof(GridColumnBase),
        new PropertyMetadata(HorizontalAlignment.Left));

    /// <summary>Identifies the <see cref="ElementStyle"/> dependency property.</summary>
    public static readonly DependencyProperty ElementStyleProperty = DependencyProperty.Register(
        nameof(ElementStyle), typeof(Style), typeof(GridColumnBase),
        new PropertyMetadata(null, OnElementStyleChanged));

    /// <summary>Identifies the <see cref="CellTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty CellTemplateProperty = DependencyProperty.Register(
        nameof(CellTemplate), typeof(DataTemplate), typeof(GridColumnBase), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="CellEditingTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty CellEditingTemplateProperty = DependencyProperty.Register(
        nameof(CellEditingTemplate), typeof(DataTemplate), typeof(GridColumnBase), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="NullText"/> dependency property.</summary>
    public static readonly DependencyProperty NullTextProperty = DependencyProperty.Register(
        nameof(NullText), typeof(string), typeof(GridColumnBase), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="HeaderStyle"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderStyleProperty = DependencyProperty.Register(
        nameof(HeaderStyle), typeof(Style), typeof(GridColumnBase), new PropertyMetadata(null));

    /// <summary>The column header content (plain text or any object with a DataTemplate).</summary>
    public object Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Binding path of the cell value within the row's data item.</summary>
    public string FieldName
    {
        get => (string)GetValue(FieldNameProperty);
        set => SetValue(FieldNameProperty, value);
    }

    /// <summary>The width of the column: absolute, star or auto.</summary>
    public GridColumnWidth Width
    {
        get => (GridColumnWidth)GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }

    /// <summary>Minimum width in pixels.</summary>
    public double MinWidth
    {
        get => (double)GetValue(MinWidthProperty);
        set => SetValue(MinWidthProperty, value);
    }

    /// <summary>Maximum width in pixels.</summary>
    public double MaxWidth
    {
        get => (double)GetValue(MaxWidthProperty);
        set => SetValue(MaxWidthProperty, value);
    }

    /// <summary>Whether the column is displayed.</summary>
    public bool Visible
    {
        get => (bool)GetValue(VisibleProperty);
        set => SetValue(VisibleProperty, value);
    }

    /// <summary>Whether cells of the column are read-only.</summary>
    public bool ReadOnly
    {
        get => (bool)GetValue(ReadOnlyProperty);
        set => SetValue(ReadOnlyProperty, value);
    }

    /// <summary>Whether the user can sort by clicking the column header.</summary>
    public bool CanUserSort
    {
        get => (bool)GetValue(CanUserSortProperty);
        set => SetValue(CanUserSortProperty, value);
    }

    /// <summary>Whether the user can open the column filter menu from the header.</summary>
    public bool CanUserFilter
    {
        get => (bool)GetValue(CanUserFilterProperty);
        set => SetValue(CanUserFilterProperty, value);
    }

    /// <summary>Property path used for sorting; defaults to <see cref="FieldName"/>.</summary>
    public string? SortMemberPath
    {
        get => (string?)GetValue(SortMemberPathProperty);
        set => SetValue(SortMemberPathProperty, value);
    }

    /// <summary>Optional format string applied to text cells, e.g. <c>C</c> or <c>{0:N2}</c>.</summary>
    public string? StringFormat
    {
        get => (string?)GetValue(StringFormatProperty);
        set => SetValue(StringFormatProperty, value);
    }

    /// <summary>Horizontal alignment of cell content.</summary>
    public HorizontalAlignment HorizontalContentAlignment
    {
        get => (HorizontalAlignment)GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    /// <summary>
    /// Optional style applied to generated cell elements. Applies to display elements only —
    /// editing elements are not affected (a separate editing style is a future addition).
    /// </summary>
    public Style? ElementStyle
    {
        get => (Style?)GetValue(ElementStyleProperty);
        set => SetValue(ElementStyleProperty, value);
    }

    /// <summary>When set, overrides the generated display element with the template's content.</summary>
    public DataTemplate? CellTemplate
    {
        get => (DataTemplate?)GetValue(CellTemplateProperty);
        set => SetValue(CellTemplateProperty, value);
    }

    /// <summary>When set, overrides the generated editing element with the template's content.</summary>
    public DataTemplate? CellEditingTemplate
    {
        get => (DataTemplate?)GetValue(CellEditingTemplateProperty);
        set => SetValue(CellEditingTemplateProperty, value);
    }

    /// <summary>Text shown instead of null values (e.g. "—"); null renders as an empty cell.</summary>
    public string? NullText
    {
        get => (string?)GetValue(NullTextProperty);
        set => SetValue(NullTextProperty, value);
    }

    /// <summary>Optional style applied to the column's header.</summary>
    public Style? HeaderStyle
    {
        get => (Style?)GetValue(HeaderStyleProperty);
        set => SetValue(HeaderStyleProperty, value);
    }

    /// <summary>The owning grid; set when the column is added to <c>Columns</c>.</summary>
    internal Grid.GridControl? Owner { get; set; }

    /// <summary>Path used for sorting: <see cref="SortMemberPath"/> or <see cref="FieldName"/>.</summary>
    internal string EffectiveSortPath => string.IsNullOrEmpty(SortMemberPath) ? FieldName : SortMemberPath!;

    /// <summary>Final width computed by the layout controller; valid after a layout pass.</summary>
    internal double ActualWidth { get; set; }

    /// <summary>Sort direction currently applied to this column (kept in sync by the grid).</summary>
    internal Data.GridSortDirection SortDirection { get; set; }

    /// <summary>Reads the cell value of the given item using the column's field path.</summary>
    public object? GetCellValue(object item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return PropertyAccessor.GetOrCreate(item.GetType(), FieldName).GetValue(item);
    }

    /// <summary>
    /// The dependency property of the generated editing element that carries the edited value;
    /// used to commit the binding and to detach it when editing ends. <see langword="null"/> when
    /// the column has no typed editing element (templates handle their own bindings).
    /// </summary>
    internal virtual System.Windows.DependencyProperty? GetEditingCommitProperty() => null;

    /// <summary>
    /// Re-binds the editing element to the data item with an explicit source so that the binding
    /// activates immediately, before the element joins the visual tree (programmatic edit flows).
    /// </summary>
    internal virtual void RebindEditingElement(FrameworkElement element, object dataItem)
    {
    }

    /// <summary>A two-way cell binding with the given explicit source.</summary>
    protected Binding CreateEditingBinding(object dataItem)
    {
        var binding = CreateCellBinding(BindingMode.TwoWay);
        binding.Source = dataItem;
        return binding;
    }

    /// <summary>Creates the (unbound) display element for a cell.</summary>
    public FrameworkElement GenerateElement()
    {
        var element = CellTemplate is { } template
            ? new ContentPresenter { ContentTemplate = template }
            : GenerateElementCore();
        if (ElementStyle is { } style)
        {
            element.Style = style;
        }

        return element;
    }

    /// <summary>Creates the (unbound) editing element for a cell; <see langword="null"/> when not editable.</summary>
    public virtual FrameworkElement? GenerateEditingElement()
        => CellEditingTemplate is { } template
            ? new ContentPresenter { ContentTemplate = template }
            : GenerateEditingElementCore();

    /// <summary>Binds a display element to the row's data item.</summary>
    public virtual void PrepareElement(FrameworkElement element, object dataItem)
    {
        element.DataContext = dataItem;
        if (element is ContentPresenter presenter)
        {
            presenter.Content = dataItem;
        }
    }

    /// <summary>Binds an editing element to the row's data item.</summary>
    public virtual void PrepareEditingElement(FrameworkElement element, object dataItem)
    {
        element.DataContext = dataItem;
        if (element is ContentPresenter presenter)
        {
            presenter.Content = dataItem;
        }
    }

    /// <summary>Releases per-cell references when a row is recycled.</summary>
    public virtual void ResetElement(FrameworkElement element)
    {
        element.DataContext = null;
        if (element is ContentPresenter presenter)
        {
            presenter.Content = null;
        }
    }

    /// <summary>Column-specific display element.</summary>
    protected abstract FrameworkElement GenerateElementCore();

    /// <summary>Column-specific editing element; <see langword="null"/> by default (not editable).</summary>
    protected virtual FrameworkElement? GenerateEditingElementCore() => null;

    /// <summary>Creates a binding to <see cref="FieldName"/> with validation switched on.</summary>
    protected Binding CreateCellBinding(BindingMode mode)
    {
        var binding = new Binding(FieldName)
        {
            Mode = mode,
            ValidatesOnDataErrors = true,
            ValidatesOnNotifyDataErrors = true,
            ValidatesOnExceptions = true,
            UpdateSourceTrigger = mode == BindingMode.TwoWay ? UpdateSourceTrigger.Explicit : UpdateSourceTrigger.PropertyChanged,
        };
        if (mode == BindingMode.OneWay && !string.IsNullOrEmpty(StringFormat))
        {
            binding.StringFormat = StringFormat;
        }

        if (!string.IsNullOrEmpty(NullText))
        {
            binding.TargetNullValue = NullText;
        }

        return binding;
    }

    /// <summary>Notifies the owner grid that a layout-relevant property changed.</summary>
    private static void OnColumnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var column = (GridColumnBase)d;
        column.Owner?.NotifyColumnChanged(column);
    }

    /// <summary>Re-styles already generated cell elements when <see cref="ElementStyle"/> changes.</summary>
    /// <remarks>Setting the style invalidates the element's measure, which propagates to the row
    /// panel and the grid's layout pass on its own — no explicit layout invalidation is needed.</remarks>
    private static void OnElementStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var column = (GridColumnBase)d;
        column.Owner?.ApplyElementStyleToCells(column);
    }

    internal static class Boxed
    {
        public static readonly object BoolTrue = true;
        public static readonly object BoolFalse = false;
    }
}
