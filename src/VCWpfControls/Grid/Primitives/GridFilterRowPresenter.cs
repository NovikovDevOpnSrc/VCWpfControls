using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Grid.Columns;

namespace VCWpfControls.Grid.Primitives;

/// <summary>
/// The optional filter row shown under the column headers: one text box per visible column,
/// aligned exactly like the cells. Typing applies an appropriate filter operator
/// (contains for text, equals with type conversion otherwise) to the owning grid.
/// </summary>
public sealed class GridFilterRowPresenter : Panel
{
    private readonly Dictionary<GridColumnBase, TextBox> _editorsByColumn = new();
    private readonly System.Windows.Threading.DispatcherTimer _debounce;
    private bool _updating;

    private GridColumnBase? _pendingColumn;
    private string _pendingText = string.Empty;

    /// <summary>The owning grid, attached when the control template is applied.</summary>
    internal GridControl? Owner { get; set; }

    /// <summary>How long typing pauses before the filter is applied (avoids refreshing per keystroke).</summary>
    public static readonly DependencyProperty DebounceIntervalProperty = DependencyProperty.Register(
        nameof(DebounceInterval), typeof(TimeSpan), typeof(GridFilterRowPresenter),
        new PropertyMetadata(TimeSpan.FromMilliseconds(300)));

    /// <summary>Filter apply debounce; <see cref="TimeSpan.Zero"/> applies on every keystroke.</summary>
    public TimeSpan DebounceInterval
    {
        get => (TimeSpan)GetValue(DebounceIntervalProperty);
        set => SetValue(DebounceIntervalProperty, value);
    }

    /// <summary>Creates the presenter and its debounce timer.</summary>
    public GridFilterRowPresenter()
    {
        _debounce = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300),
        };
        _debounce.Tick += OnDebounceTick;
    }

    private void OnDebounceTick(object? sender, EventArgs e)
    {
        _debounce.Stop();
        if (_pendingColumn is not null && Owner is not null)
        {
            var column = _pendingColumn;
            var text = _pendingText;
            _pendingColumn = null;
            Owner.ApplyFilterText(column, text);
        }
    }

    /// <summary>Builds or reuses one text box per visible column.</summary>
    internal void EnsureEditors()
    {
        if (Owner is null)
        {
            return;
        }

        Children.Clear();
        foreach (var column in Owner.VisibleColumns)
        {
            if (!_editorsByColumn.TryGetValue(column, out var editor))
            {
                editor = new TextBox
                {
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2, 0, 2, 0), // inset INSIDE the column slot — keeps alignment
                    Tag = column,
                };
                editor.TextChanged += OnEditorTextChanged;
                _editorsByColumn[column] = editor;
            }

            Children.Add(editor);
        }

        InvalidateMeasure();
    }

    /// <summary>Measures editors with their column's slot width.</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Owner is null)
        {
            return default;
        }

        var columns = Owner.VisibleColumns;
        var height = 0d;
        for (var i = 0; i < columns.Count && i < Children.Count; i++)
        {
            var child = Children[i];
            var slot = Owner.Layout.GetSlot(columns[i]);
            child.Measure(new Size(slot?.Width ?? 0, availableSize.Height));
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(availableSize.Width, height);
    }

    /// <summary>Arranges editors exactly like header cells (frozen editors ignore the offset).</summary>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Owner is null)
        {
            return finalSize;
        }

        var columns = Owner.VisibleColumns;
        var offset = Owner.HorizontalOffset;
        for (var i = 0; i < columns.Count && i < Children.Count; i++)
        {
            var child = Children[i];
            var slot = Owner.Layout.GetSlot(columns[i]);
            if (slot is null)
            {
                child.Arrange(new Rect(0, 0, 0, 0));
                continue;
            }

            var x = slot.IsFrozen ? slot.X : slot.X - offset;
            child.Arrange(new Rect(x, 0, slot.Width, finalSize.Height));
        }

        return finalSize;
    }

    private void OnEditorTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || sender is not TextBox { Tag: GridColumnBase column } editor || Owner is null)
        {
            return;
        }

        _pendingColumn = column;
        _pendingText = editor.Text;
        if (DebounceInterval <= TimeSpan.Zero)
        {
            OnDebounceTick(this, EventArgs.Empty);
            return;
        }

        _debounce.Interval = DebounceInterval;
        _debounce.Stop();
        _debounce.Start();
    }

    /// <summary>Clears all filter editors without raising filter changes.</summary>
    internal void ClearEditors()
    {
        _updating = true;
        try
        {
            foreach (var editor in _editorsByColumn.Values)
            {
                editor.Text = string.Empty;
            }
        }
        finally
        {
            _updating = false;
        }
    }
}
