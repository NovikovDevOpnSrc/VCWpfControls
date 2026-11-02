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

        var content = new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD1, 0xD5, 0xDB)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(2),
            MinWidth = 180,
            Child = new StackPanel
            {
                Children =
                {
                    _searchBox,
                    selectAll,
                    new ScrollViewer { Content = _itemsHost, MaxHeight = 260 },
                    buttons,
                },
            },
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
    }

    private void Apply(bool accepted)
    {
        if (accepted)
        {
            var selected = new List<object?>();
            foreach (var item in _allItems)
            {
                if (item.IsChecked)
                {
                    selected.Add(item.Value);
                }
            }

            // Remove the column's previous filter first.
            var existing = GetActiveFilter();
            if (existing is not null)
            {
                _owner.FilterDescriptions.Remove(existing);
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
