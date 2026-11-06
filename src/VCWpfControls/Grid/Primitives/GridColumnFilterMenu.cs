using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Data;
using VCWpfControls.Localization;

namespace VCWpfControls.Grid.Primitives;

/// <summary>
/// The Excel-like column filter menu: a search box plus a check list of the column's distinct
/// values. "OK" applies an <c>In</c> filter with the checked values; "Clear" removes the
/// column's filter. Built in code so it needs no extra theme templates.
/// </summary>
public sealed class GridColumnFilterMenu
{
    private const int MaxValues = 500;

    private readonly GridControl _owner;
    private readonly GridColumnBase _column;
    private readonly Popup _popup;
    private readonly TextBox _searchBox;
    private readonly StackPanel _itemsHost;
    private readonly List<FilterValueItem> _allItems = new();
    private readonly TextBox? _fromBox;
    private readonly TextBox? _toBox;
    private readonly Type? _rangeType;

    /// <summary>Creates the menu for a column of the given grid.</summary>
    public GridColumnFilterMenu(GridControl owner, GridColumnBase column)
    {
        _owner = owner;
        _column = column;

        _searchBox = new TextBox { Margin = new Thickness(4) };

        _itemsHost = new StackPanel();
        _searchBox.TextChanged += OnSearchChanged;

        var selectAll = new CheckBox
        {
            Content = new TextBlock { Text = GridStrings.FilterMenuSelectAll, FontWeight = FontWeights.SemiBold },
            Margin = new Thickness(6, 6, 4, 2),
            IsThreeState = false,
        };
        selectAll.Checked += (_, _) => SetAllChecked(true);
        selectAll.Unchecked += (_, _) => SetAllChecked(false);
        SelectAllToggle = selectAll;

        var okButton = new Button { Content = GridStrings.FilterMenuOk, MinWidth = 72, Margin = new Thickness(4, 4, 2, 4) };
        okButton.Click += (_, _) => Apply(accepted: true);

        var clearButton = new Button { Content = GridStrings.FilterMenuClear, MinWidth = 72, Margin = new Thickness(2, 4, 4, 4) };
        clearButton.Click += (_, _) => Apply(accepted: false);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(okButton);
        buttons.Children.Add(clearButton);

        _rangeType = ResolveRangeType();
        UIElement? rangeRow = null;
        if (_rangeType is not null)
        {
            _fromBox = new TextBox { Margin = new Thickness(4, 2, 2, 4), MinWidth = 80 };
            _toBox = new TextBox { Margin = new Thickness(2, 2, 4, 4), MinWidth = 80 };
            rangeRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new TextBlock { Text = GridStrings.FilterFrom, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 2, 0) },
                    _fromBox,
                    new TextBlock { Text = GridStrings.FilterTo, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 2, 0) },
                    _toBox,
                },
            };
        }

        var content = new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD1, 0xD5, 0xDB)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(2),
            MinWidth = 180,
            Child = BuildContent(_searchBox, rangeRow, selectAll, _itemsHost, buttons),
        };

        _popup = new Popup
        {
            Child = content,
            PlacementTarget = owner.HeadersPresenter?.GetHeader(column) ?? (UIElement)owner,
            Placement = PlacementMode.Bottom,
            StaysOpen = false,
            AllowsTransparency = true,
        };
        _popup.Closed += (_, _) => Closed?.Invoke(this, EventArgs.Empty);

        LoadValues();
    }

    private static StackPanel BuildContent(
        UIElement searchBox, UIElement? rangeRow, UIElement selectAll, StackPanel itemsHost, UIElement buttons)
    {
        var panel = new StackPanel();
        panel.Children.Add(searchBox);
        if (rangeRow is not null)
        {
            panel.Children.Add(rangeRow);
        }

        panel.Children.Add(selectAll);
        panel.Children.Add(new ScrollViewer { Content = itemsHost, MaxHeight = 260 });
        panel.Children.Add(buttons);
        return panel;
    }

    /// <summary>Raised after the menu closed (applied or dismissed).</summary>
    public event EventHandler? Closed;

    private CheckBox SelectAllToggle { get; }

    /// <summary>Opens the popup.</summary>
    public void Open()
    {
        PreselectCurrentFilter();
        _popup.IsOpen = true;
        _searchBox.Focus();
    }

    /// <summary>Force-closes the popup.</summary>
    public void Close() => _popup.IsOpen = false;

    private void LoadValues()
    {
        var values = _owner.DataEngine.GetDistinctValues(_column.FieldName, MaxValues);
        var activeFilter = GetActiveFilter();

        foreach (var value in values)
        {
            var isChecked = true;
            if (activeFilter is { Operator: GridFilterOperator.In or GridFilterOperator.NotIn, Value: System.Collections.IEnumerable list }
                && list is not string)
            {
                isChecked = false;
                foreach (var candidate in list)
                {
                    if (GridValueCompare.Compare(candidate, value, null) == 0)
                    {
                        isChecked = true;
                        break;
                    }
                }
            }

            var item = new FilterValueItem(value, isChecked);
            _allItems.Add(item);
            _itemsHost.Children.Add(CreateItemVisual(item));
        }
    }

    private GridFilterDescription? GetActiveFilter()
    {
        foreach (var filter in _owner.FilterDescriptions)
        {
            if (string.Equals(filter.FieldName, _column.FieldName, StringComparison.OrdinalIgnoreCase))
            {
                return filter;
            }
        }

        return null;
    }

    private static CheckBox CreateItemVisual(FilterValueItem item)
    {
        var checkBox = new CheckBox
        {
            Content = DisplayText(item.Value),
            IsChecked = item.IsChecked,
            Margin = new Thickness(6, 2, 4, 2),
            Tag = item,
        };
        checkBox.Checked += (_, _) => item.IsChecked = true;
        checkBox.Unchecked += (_, _) => item.IsChecked = false;
        return checkBox;
    }

    private static string DisplayText(object? value)
        => value is null ? string.Empty : Convert.ToString(value, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        var filter = _searchBox.Text;
        _itemsHost.Children.Clear();
        foreach (var item in _allItems)
        {
            if (filter.Length == 0
                || DisplayText(item.Value).Contains(filter, StringComparison.CurrentCultureIgnoreCase))
            {
                _itemsHost.Children.Add(CreateItemVisual(item));
            }
        }
    }

    private void SetAllChecked(bool isChecked)
    {
        foreach (var child in _itemsHost.Children.OfType<CheckBox>())
        {
            child.IsChecked = isChecked;
        }
    }

    private void PreselectCurrentFilter()
    {
        var anyUnchecked = _allItems.Any(item => !item.IsChecked);
        SelectAllToggle.IsChecked = !anyUnchecked;

        if (_fromBox is null || _toBox is null)
        {
            return;
        }

        var active = GetActiveFilter();
        if (active is null)
        {
            return;
        }

        if (active.Operator is GridFilterOperator.Between or GridFilterOperator.NotBetween)
        {
            _fromBox.Text = DisplayText(active.Value);
            _toBox.Text = DisplayText(active.Value2);
        }
        else if (active.Operator is GridFilterOperator.GreaterThan or GridFilterOperator.GreaterThanOrEqual)
        {
            _fromBox.Text = DisplayText(active.Value);
        }
        else if (active.Operator is GridFilterOperator.LessThan or GridFilterOperator.LessThanOrEqual)
        {
            _toBox.Text = DisplayText(active.Value);
        }
    }

    private void Apply(bool accepted)
    {
        if (accepted)
        {
            // Remove the column's previous filter first.
            var existing = GetActiveFilter();
            if (existing is not null)
            {
                _owner.FilterDescriptions.Remove(existing);
            }

            // A parsed range (From and/or To) takes precedence over the value list:
            // both ends => Between, one end => >= / <=.
            var hasFrom = TryParseRangeValue(_fromBox?.Text, out var from);
            var hasTo = TryParseRangeValue(_toBox?.Text, out var to);
            if (hasFrom && hasTo)
            {
                _owner.FilterDescriptions.Add(new GridFilterDescription(_column.FieldName, GridFilterOperator.Between, from, to));
                Close();
                return;
            }

            if (hasFrom)
            {
                _owner.FilterDescriptions.Add(new GridFilterDescription(_column.FieldName, GridFilterOperator.GreaterThanOrEqual, from));
                Close();
                return;
            }

            if (hasTo)
            {
                _owner.FilterDescriptions.Add(new GridFilterDescription(_column.FieldName, GridFilterOperator.LessThanOrEqual, to));
                Close();
                return;
            }

            var selected = new List<object?>();
            foreach (var item in _allItems)
            {
                if (item.IsChecked)
                {
                    selected.Add(item.Value);
                }
            }

            // All or nothing selected means "no filter"; a partial selection applies an In filter.
            var allChecked = selected.Count == _allItems.Count;
            if (!allChecked && selected.Count > 0)
            {
                _owner.FilterDescriptions.Add(new GridFilterDescription(_column.FieldName, GridFilterOperator.In, selected));
            }
        }

        Close();
    }

    /// <summary>The value type the From/To boxes parse into (numeric or date columns only).</summary>
    private Type? ResolveRangeType()
    {
        var valueType = _owner.DataEngine.ResolvedItemType is { } itemType
            ? Data.PropertyAccessor.GetOrCreate(itemType, _column.FieldName).ValueType
            : typeof(object);
        var nonNullable = Nullable.GetUnderlyingType(valueType) ?? valueType;
        if (nonNullable == typeof(DateTime))
        {
            return typeof(DateTime);
        }

        return nonNullable != typeof(object) && nonNullable != typeof(bool) && nonNullable != typeof(string)
            && (nonNullable.IsPrimitive || nonNullable == typeof(decimal))
            ? nonNullable
            : null;
    }

    /// <summary>Culture-aware parse of one range end (empty/invalid text = no bound).</summary>
    private bool TryParseRangeValue(string? text, out object? value)
    {
        value = null;
        return text is { Length: > 0 } && _rangeType is not null
            && GridClipboard.TryConvert(text.Trim(), _rangeType, out value);
    }

    /// <summary>The From box text (test hook; null when the column has no range mode).</summary>
    internal string? FromTextForTest => _fromBox?.Text;

    /// <summary>The To box text (test hook; null when the column has no range mode).</summary>
    internal string? ToTextForTest => _toBox?.Text;

    /// <summary>Types into the range boxes and applies (test hook).</summary>
    internal void ApplyRangeForTest(string? from, string? to)
    {
        if (_fromBox is not null)
        {
            _fromBox.Text = from ?? string.Empty;
        }

        if (_toBox is not null)
        {
            _toBox.Text = to ?? string.Empty;
        }

        Apply(accepted: true);
    }

    private sealed class FilterValueItem
    {
        public FilterValueItem(object? value, bool isChecked)
        {
            Value = value;
            IsChecked = isChecked;
        }

        public object? Value { get; }

        public bool IsChecked { get; set; }
    }
}
