using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using VCWpfControls.Grid;

namespace VCWpfControls.Editors;

/// <summary>
/// A text input with suggestion popup: filters <see cref="ItemsSource"/> by the typed
/// prefix (via <see cref="ItemFilter"/> or a culture-invariant contains over the item
/// text), shows up to <see cref="MaxSuggestions"/> items, accepts a suggestion on Enter/
/// click/Tab, closes on Esc. UI-free filtering logic in <see cref="AutoCompleteFilter"/>
/// (unit-testable); the control only hosts it.
/// </summary>
[TemplatePart(Name = PartTextBox, Type = typeof(TextBox))]
[TemplatePart(Name = PartPopup, Type = typeof(Popup))]
[TemplatePart(Name = PartList, Type = typeof(ListBox))]
public class AutoComplete : Control
{
    private const string PartTextBox = "PART_TextBox";
    private const string PartPopup = "PART_Popup";
    private const string PartList = "PART_List";

    /// <summary>Identifies the <see cref="Text"/> dependency property.</summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(AutoComplete),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

    /// <summary>Identifies the <see cref="ItemsSource"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(AutoComplete),
        new PropertyMetadata(null, OnSourceChanged));

    /// <summary>Identifies the <see cref="SelectedItem"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
        nameof(SelectedItem), typeof(object), typeof(AutoComplete),
        new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="MinimumPrefixLength"/> dependency property.</summary>
    public static readonly DependencyProperty MinimumPrefixLengthProperty = DependencyProperty.Register(
        nameof(MinimumPrefixLength), typeof(int), typeof(AutoComplete), new PropertyMetadata(1));

    /// <summary>Identifies the <see cref="MaxSuggestions"/> dependency property.</summary>
    public static readonly DependencyProperty MaxSuggestionsProperty = DependencyProperty.Register(
        nameof(MaxSuggestions), typeof(int), typeof(AutoComplete), new PropertyMetadata(8));

    /// <summary>Identifies the <see cref="ItemFilter"/> dependency property.</summary>
    public static readonly DependencyProperty ItemFilterProperty = DependencyProperty.Register(
        nameof(ItemFilter), typeof(Predicate<object>), typeof(AutoComplete), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="ItemTextPath"/> dependency property.</summary>
    public static readonly DependencyProperty ItemTextPathProperty = DependencyProperty.Register(
        nameof(ItemTextPath), typeof(string), typeof(AutoComplete), new PropertyMetadata(null));

    private TextBox? _textBox;
    private Popup? _popup;
    private ListBox? _list;
    private bool _suppressText;

    static AutoComplete()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AutoComplete),
            new FrameworkPropertyMetadata(typeof(AutoComplete)));
        KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(AutoComplete),
            new FrameworkPropertyMetadata(false));
        FocusableProperty.OverrideMetadata(typeof(AutoComplete),
            new FrameworkPropertyMetadata(false));
    }

    /// <summary>Raised after the user accepted a suggestion (or cleared the text).</summary>
    public event RoutedEventHandler? SuggestionAccepted
    {
        add => AddHandler(SuggestionAcceptedEvent, value);
        remove => RemoveHandler(SuggestionAcceptedEvent, value);
    }

    /// <summary>Identifies the <see cref="SuggestionAccepted"/> routed event.</summary>
    public static readonly RoutedEvent SuggestionAcceptedEvent = EventManager.RegisterRoutedEvent(
        nameof(SuggestionAccepted), RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(AutoComplete));

    /// <summary>The typed text (two-way bindable).</summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>The suggestion source (any IEnumerable; strings or objects with ItemTextPath).</summary>
    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>The accepted suggestion item, if any.</summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>Minimum typed characters before suggestions appear.</summary>
    public int MinimumPrefixLength
    {
        get => (int)GetValue(MinimumPrefixLengthProperty);
        set => SetValue(MinimumPrefixLengthProperty, value);
    }

    /// <summary>Caps the suggestion list length.</summary>
    public int MaxSuggestions
    {
        get => (int)GetValue(MaxSuggestionsProperty);
        set => SetValue(MaxSuggestionsProperty, value);
    }

    /// <summary>Custom matcher; null = culture-invariant contains over the item text.</summary>
    public Predicate<object>? ItemFilter
    {
        get => (Predicate<object>?)GetValue(ItemFilterProperty);
        set => SetValue(ItemFilterProperty, value);
    }

    /// <summary>Property path used to extract match text from non-string items.</summary>
    public string? ItemTextPath
    {
        get => (string?)GetValue(ItemTextPathProperty);
        set => SetValue(ItemTextPathProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (AutoComplete)d;
        if (control._suppressText || control._textBox is null)
        {
            return;
        }

        control._textBox.Text = (string)e.NewValue;
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // A new source invalidates the current suggestion state.
        ((AutoComplete)d).SetCurrentValue(SelectedItemProperty, null);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _textBox = GetTemplateChild(PartTextBox) as TextBox;
        _popup = GetTemplateChild(PartPopup) as Popup;
        _list = GetTemplateChild(PartList) as ListBox;

        if (_textBox is not null)
        {
            _textBox.TextChanged += OnInputChanged;
            _textBox.PreviewKeyDown += OnInputKeyDown;
            if (!_suppressText)
            {
                _textBox.Text = Text; // a Text set BEFORE the template applied never
                                      // reached the box (the callback found _textBox
                                      // null and dropped it silently).
            }
        }

        if (_list is not null)
        {
            _list.PreviewMouseLeftButtonUp += OnListMouseUp;
        }
    }

    /// <summary>Match text of an item: string as-is, else ItemTextPath value, else ToString.</summary>
    internal string ItemText(object item)
        => AutoCompleteFilter.ItemText(item, ItemTextPath);

    private void OnInputChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressText || _list is null || _popup is null)
        {
            return;
        }

        SetCurrentValue(TextProperty, _textBox!.Text);
        var suggestions = AutoCompleteFilter.Match(
            _textBox.Text,
            ItemsSource,
            MinimumPrefixLength,
            MaxSuggestions,
            ItemFilter is null ? null : item => ItemFilter(item),
            ItemTextPath);

        _list.ItemsSource = suggestions;
        if (suggestions.Count > 0)
        {
            _list.SelectedIndex = 0;
            _popup.IsOpen = true;
        }
        else
        {
            _popup.IsOpen = false;
        }

        SetCurrentValue(SelectedItemProperty, null);
    }

    private void OnInputKeyDown(object sender, KeyEventArgs e)
    {
        if (_popup is not { IsOpen: true } || _list is null)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Down:
                MoveSelection(1);
                e.Handled = true;
                break;
            case Key.Up:
                MoveSelection(-1);
                e.Handled = true;
                break;
            case Key.Enter:
            case Key.Tab:
                Accept(_list.SelectedItem);
                e.Handled = true;
                break;
            case Key.Escape:
                _popup.IsOpen = false;
                e.Handled = true;
                break;
        }
    }

    private void OnListMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_list?.SelectedItem is { } item)
        {
            Accept(item);
            e.Handled = true;
        }
    }

    private void MoveSelection(int delta)
    {
        if (_list is null || _list.Items.Count == 0)
        {
            return;
        }

        var next = Math.Clamp((_list.SelectedIndex < 0 ? 0 : _list.SelectedIndex) + delta, 0, _list.Items.Count - 1);
        _list.SelectedIndex = next;
    }

    /// <summary>Applies the chosen suggestion to Text/SelectedItem and closes the popup.</summary>
    private void Accept(object? item)
    {
        if (_popup is not null)
        {
            _popup.IsOpen = false;
        }

        if (item is null)
        {
            return;
        }

        SetCurrentValue(SelectedItemProperty, item);
        _suppressText = true;
        try
        {
            var accepted = ItemText(item);
            if (_textBox is not null)
            {
                _textBox.Text = accepted;
                _textBox.CaretIndex = accepted.Length;
            }

            SetCurrentValue(TextProperty, accepted);
        }
        finally
        {
            _suppressText = false;
        }

        RaiseEvent(new RoutedEventArgs(SuggestionAcceptedEvent, this));
    }
}

/// <summary>UI-free suggestion matching of <see cref="AutoComplete"/> (unit-testable).</summary>
public static class AutoCompleteFilter
{
    /// <summary>Match text of an item: string as-is, else the path value, else ToString.</summary>
    public static string ItemText(object item, string? path)
    {
        if (item is string text)
        {
            return text;
        }

        if (!string.IsNullOrEmpty(path))
        {
            var value = Grid.Data.PropertyAccessor.GetOrCreate(item.GetType(), path).GetValue(item);
            if (value is not null)
            {
                return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            }
        }

        return Convert.ToString(item, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
    }

    /// <summary>
    /// Suggestions for the typed query: empty when shorter than the minimum prefix, capped
    /// at maxCount; custom matcher or culture-invariant case-insensitive contains.
    /// </summary>
    public static List<object> Match(
        string query,
        IEnumerable? source,
        int minimumPrefixLength,
        int maxCount,
        Func<object, bool>? customMatcher,
        string? itemTextPath)
    {
        var result = new List<object>();
        if (source is null || query.Length < Math.Max(0, minimumPrefixLength) || maxCount <= 0)
        {
            return result;
        }

        var comparer = StringComparison.InvariantCultureIgnoreCase;
        foreach (var item in source)
        {
            if (item is null)
            {
                continue;
            }

            var matches = customMatcher is not null
                ? customMatcher(item)
                : ItemText(item, itemTextPath).Contains(query, comparer);
            if (matches)
            {
                result.Add(item);
                if (result.Count >= maxCount)
                {
                    break;
                }
            }
        }

        return result;
    }
}
