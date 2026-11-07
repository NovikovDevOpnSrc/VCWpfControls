using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using VCWpfControls.Localization;
using VCWpfControls.PropertyGrid.Primitives;

namespace VCWpfControls.PropertyGrid;

/// <summary>
/// A property grid: shows the public properties of <see cref="SelectedObject"/> grouped by
/// category with typed editors (text, multi-line text, check box, enum combo, numeric, date),
/// a search box, expandable nested objects and live updates through
/// <see cref="INotifyPropertyChanged"/>.
/// </summary>
public class PropertyGrid : Control
{
    /// <summary>Identifies the <see cref="SelectedObject"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedObjectProperty = DependencyProperty.Register(
        nameof(SelectedObject), typeof(object), typeof(PropertyGrid),
        new PropertyMetadata(null, OnSelectedObjectChanged));

    /// <summary>Identifies the <see cref="ShowSearchBox"/> dependency property.</summary>
    public static readonly DependencyProperty ShowSearchBoxProperty = DependencyProperty.Register(
        nameof(ShowSearchBox), typeof(bool), typeof(PropertyGrid), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="ShowCategories"/> dependency property.</summary>
    public static readonly DependencyProperty ShowCategoriesProperty = DependencyProperty.Register(
        nameof(ShowCategories), typeof(bool), typeof(PropertyGrid), new PropertyMetadata(true, OnDisplayOptionsChanged));

    /// <summary>Identifies the <see cref="LabelWidth"/> dependency property.</summary>
    /// <summary>Identifies the <see cref="CommitMode"/> dependency property.</summary>
    public static readonly DependencyProperty CommitModeProperty = DependencyProperty.Register(
        nameof(CommitMode), typeof(PropertyGridCommitMode), typeof(PropertyGrid),
        new PropertyMetadata(PropertyGridCommitMode.OnCommit));

    /// <summary>Identifies the <see cref="LabelWidth"/> dependency property.</summary>
    public static readonly DependencyProperty LabelWidthProperty = DependencyProperty.Register(
        nameof(LabelWidth), typeof(double), typeof(PropertyGrid), new PropertyMetadata(130d));

    private const string PartSearchBox = "PART_SearchBox";
    private const string PartItemsHost = "PART_ItemsHost";

    private readonly ObservableCollection<PropertyItem> _items = new();
    private INotifyPropertyChanged? _notifySource;
    private TextBox? _searchBox;
    private StackPanel? _itemsHost;
    private string _filter = string.Empty;

    static PropertyGrid()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyGrid),
            new FrameworkPropertyMetadata(typeof(PropertyGrid)));
    }

    /// <summary>Creates the property grid.</summary>
    public PropertyGrid()
    {
        AddHandler(PropertyItemEditor.ValueChangedEvent, new RoutedEventHandler(OnEditorValueChanged));
    }

    /// <summary>Occurs after a property value was edited.</summary>
    public event EventHandler<PropertyValueChangedEventArgs>? PropertyValueChanged;

    /// <summary>The object whose properties are displayed (bindable).</summary>
    public object? SelectedObject
    {
        get => GetValue(SelectedObjectProperty);
        set => SetValue(SelectedObjectProperty, value);
    }

    /// <summary>Whether the search box is visible.</summary>
    public bool ShowSearchBox
    {
        get => (bool)GetValue(ShowSearchBoxProperty);
        set => SetValue(ShowSearchBoxProperty, value);
    }

    /// <summary>Whether properties are grouped by category into expanders.</summary>
    public bool ShowCategories
    {
        get => (bool)GetValue(ShowCategoriesProperty);
        set => SetValue(ShowCategoriesProperty, value);
    }

    /// <summary>Width of the label column in pixels.</summary>
    /// <summary>When property edits are pushed to the object (Enter/focus loss by default).</summary>
    public PropertyGridCommitMode CommitMode
    {
        get => (PropertyGridCommitMode)GetValue(CommitModeProperty);
        set => SetValue(CommitModeProperty, value);
    }

    /// <summary>The width of the label column.</summary>
    public double LabelWidth
    {
        get => (double)GetValue(LabelWidthProperty);
        set => SetValue(LabelWidthProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_searchBox is not null)
        {
            _searchBox.TextChanged -= OnSearchTextChanged;
        }

        _searchBox = GetTemplateChild(PartSearchBox) as TextBox;
        _itemsHost = GetTemplateChild(PartItemsHost) as StackPanel;

        if (_searchBox is not null)
        {
            _searchBox.TextChanged += OnSearchTextChanged;
        }

        RebuildVisualTree();
    }

    private static void OnSelectedObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (PropertyGrid)d;
        grid.RebuildItems();
        grid.RebuildVisualTree();
    }

    private static void OnDisplayOptionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PropertyGrid)d).RebuildVisualTree();

    private void RebuildItems()
    {
        DetachNotifySource();
        _items.Clear();

        if (SelectedObject is not { } target)
        {
            return;
        }

        foreach (var item in PropertyItemBuilder.Build(target))
        {
            _items.Add(item);
        }

        _notifySource = target as INotifyPropertyChanged;
        if (_notifySource is not null)
        {
            _notifySource.PropertyChanged += OnSourcePropertyChanged;
        }
    }

    private void DetachNotifySource()
    {
        if (_notifySource is not null)
        {
            _notifySource.PropertyChanged -= OnSourcePropertyChanged;
            _notifySource = null;
        }
    }

    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        foreach (var item in _items)
        {
            if (string.Equals(item.Name, e.PropertyName, StringComparison.Ordinal))
            {
                RefreshItemEditor(item);
                return;
            }
        }
    }

    /// <summary>All flattened items including expandable children (test hook).</summary>
    internal System.Collections.Generic.List<PropertyItem> GetAllItemsForTest()
    {
        var all = new System.Collections.Generic.List<PropertyItem>();
        void Walk(System.Collections.Generic.IEnumerable<PropertyItem> items)
        {
            foreach (var item in items)
            {
                all.Add(item);
                if (item.Children is not null)
                {
                    Walk(item.Children);
                }
            }
        }

        Walk(_items);
        return all;
    }

    /// <summary>The editor host of the item as currently shown, if any (test hook).</summary>
    internal Primitives.PropertyItemEditor? GetEditorForTest(PropertyItem item)
    {
        foreach (var child in _itemsHost?.Children.OfType<object>() ?? Enumerable.Empty<object>())
        {
            var found = FindEditor(child, item);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static Primitives.PropertyItemEditor? FindEditor(object root, PropertyItem item)
    {
        if (root is Primitives.PropertyItemEditor editor && ReferenceEquals(editor.Item, item))
        {
            return editor;
        }

        if (root is System.Windows.Media.Visual or System.Windows.FrameworkElement)
        {
            foreach (var child in System.Windows.LogicalTreeHelper.GetChildren((System.Windows.DependencyObject)root).OfType<object>())
            {
                var found = FindEditor(child, item);
                if (found is not null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    /// <summary>Display names of the items currently visible after filtering (test hook).</summary>
    internal System.Collections.Generic.List<string> GetVisibleItemNamesForTest()
        => FilterItems().Select(i => i.DisplayName).ToList();

    /// <summary>The search box of the current template (test hook).</summary>
    internal System.Windows.Controls.TextBox? GetSearchBoxForTest() => _searchBox;

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _filter = _searchBox?.Text ?? string.Empty;
        RebuildVisualTree();
    }

    /// <summary>Rebuilds the visual tree (categories, filtered items, editors).</summary>
    private void RebuildVisualTree()
    {
        if (_itemsHost is null)
        {
            return;
        }

        _itemsHost.Children.Clear();

        if (SelectedObject is null)
        {
            return;
        }

        var items = FilterItems();

        if (ShowCategories)
        {
            foreach (var group in GroupByCategory(items))
            {
                var expander = new Expander
                {
                    Header = BuildCategoryHeader(group.Key, group.Count()),
                    IsExpanded = true,
                    Margin = new Thickness(0, 0, 0, 4),
                };
                var panel = new StackPanel();
                foreach (var item in group)
                {
                    panel.Children.Add(BuildItemRow(item, 0));
                }

                expander.Content = panel;
                _itemsHost.Children.Add(expander);
            }
        }
        else
        {
            foreach (var item in items)
            {
                _itemsHost.Children.Add(BuildItemRow(item, 0));
            }
        }
    }

    private List<PropertyItem> FilterItems()
    {
        if (_filter.Length == 0)
        {
            return _items.ToList();
        }

        return _items
            .Where(item => item.DisplayName.Contains(_filter, StringComparison.CurrentCultureIgnoreCase)
                || item.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase))
            .ToList();
    }

    private static IEnumerable<IGrouping<string?, PropertyItem>> GroupByCategory(List<PropertyItem> items)
        => items.GroupBy(i => i.Category);

    private static object BuildCategoryHeader(string? category, int count)
        => new TextBlock
        {
            Text = $"{category ?? GridStrings.CategoryMisc} ({count})",
            FontWeight = FontWeights.SemiBold,
        };

    /// <summary>Builds one property row: label plus a typed editor.</summary>
    private Border BuildItemRow(PropertyItem item, int indentLevel)
    {
        var label = new TextBlock
        {
            Text = item.DisplayName,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 5, 8, 0),
            TextTrimming = TextTrimming.CharacterEllipsis,
            ToolTip = string.IsNullOrEmpty(item.Description)
                ? null
                : item.Description,
        };

        var editorHost = new PropertyItemEditor
        {
            Item = item,
            CommitMode = CommitMode,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 2, 0, 4),
        };

        var grid = new System.Windows.Controls.Grid();
        grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(LabelWidth, GridUnitType.Pixel) });
        grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        if (!item.IsExpandable)
        {
            System.Windows.Controls.Grid.SetColumn(label, 0);
            grid.Children.Add(label);
        }

        System.Windows.Controls.Grid.SetColumn(editorHost, 1);
        grid.Children.Add(editorHost);

        var border = new Border
        {
            // Do NOT parent the grid here for expandable items: BuildExpandableRow adds it
            // into its own container - double logical parenting throws.
            Child = item.IsExpandable ? null : grid,
            Padding = new Thickness(indentLevel * 16, 0, 0, 0),
            BorderBrush = TryFindResource(VCWpfControls.Grid.GridResources.GridLineKey) as Brush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Tag = item,
        };

        if (item.IsExpandable)
        {
            border.Child = BuildExpandableRow(item, grid, indentLevel);
        }

        return border;
    }

    private StackPanel BuildExpandableRow(PropertyItem item, System.Windows.Controls.Grid labelEditorGrid, int indentLevel)
    {
        var container = new StackPanel();

        var headerGrid = new System.Windows.Controls.Grid();
        headerGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(18) });
        headerGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(LabelWidth - 18, GridUnitType.Pixel) });
        headerGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var toggle = new ToggleButton
        {
            Content = "+",
            Width = 16,
            Height = 16,
            Focusable = false,
            Margin = new Thickness(0, 3, 2, 0),
        };

        var displayName = new TextBlock
        {
            Text = item.DisplayName,
            Margin = new Thickness(0, 5, 8, 0),
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        System.Windows.Controls.Grid.SetColumn(displayName, 1);

        var valueText = new TextBlock
        {
            Text = item.GetValueText(),
            Opacity = 0.7,
            Margin = new Thickness(0, 5, 0, 0),
            FontStyle = FontStyles.Italic,
        };
        System.Windows.Controls.Grid.SetColumn(valueText, 2);

        headerGrid.Children.Add(toggle);
        headerGrid.Children.Add(displayName);
        headerGrid.Children.Add(valueText);

        var childrenHost = new StackPanel { Margin = new Thickness(18, 0, 0, 0), Visibility = Visibility.Collapsed };
        toggle.Checked += (_, _) =>
        {
            if (childrenHost.Children.Count == 0)
            {
                item.Children ??= PropertyItemBuilder.BuildChildren(item);
                if (item.Children is not null)
                {
                    foreach (var child in item.Children)
                    {
                        childrenHost.Children.Add(BuildItemRow(child, indentLevel + 1));
                    }
                }
            }

            childrenHost.Visibility = Visibility.Visible;
        };
        toggle.Unchecked += (_, _) => childrenHost.Visibility = Visibility.Collapsed;

        container.Children.Add(headerGrid);
        container.Children.Add(labelEditorGrid);
        container.Children.Add(childrenHost);
        return container;
    }

    private void RefreshItemEditor(PropertyItem item)
    {
        if (_itemsHost is null)
        {
            return;
        }

        // LOGICAL walk: the rows are code-built; a visual walk misses editors that have
        // not rendered yet (or sit in collapsed expanders), silently dropping INPC updates.
        if (FindEditor(_itemsHost, item) is { } editor)
        {
            editor.RefreshValue();
        }
    }


    private void OnEditorValueChanged(object sender, RoutedEventArgs e)
    {
        if (e is PropertyValueChangedEventArgs args)
        {
            PropertyValueChanged?.Invoke(this, args);
        }
    }
}
