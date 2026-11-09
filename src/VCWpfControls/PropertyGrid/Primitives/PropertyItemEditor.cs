using System.Globalization;
using System.Windows;
using VCWpfControls.PropertyGrid;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace VCWpfControls.PropertyGrid.Primitives;

/// <summary>Event arguments of <see cref="PropertyItemEditor.ValueChangedEvent"/>.</summary>
public sealed class PropertyValueChangedEventArgs : RoutedEventArgs
{
    /// <summary>Creates the event args.</summary>
    public PropertyValueChangedEventArgs(PropertyItem item, object? oldValue, object? newValue)
    {
        Item = item;
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>The edited property item.</summary>
    public PropertyItem Item { get; }

    /// <summary>The value before the edit.</summary>
    public object? OldValue { get; }

    /// <summary>The value after the edit.</summary>
    public object? NewValue { get; }
}

/// <summary>
/// Hosts the typed editor control of one <see cref="PropertyItem"/> and pushes edits back
/// through reflection, raising <see cref="ValueChangedEvent"/>.
/// </summary>
public sealed class PropertyItemEditor : ContentControl
{
    /// <summary>Identifies the <see cref="ValueChanged"/> routed event.</summary>
    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged), RoutingStrategy.Bubble,
        typeof(EventHandler<PropertyValueChangedEventArgs>), typeof(PropertyItemEditor));

    /// <summary>Identifies the <see cref="Item"/> dependency property.</summary>
    public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(
        nameof(Item), typeof(PropertyItem), typeof(PropertyItemEditor), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="CommitMode"/> dependency property.</summary>
    public static readonly DependencyProperty CommitModeProperty = DependencyProperty.Register(
        nameof(CommitMode), typeof(PropertyGridCommitMode), typeof(PropertyItemEditor),
        new PropertyMetadata(PropertyGridCommitMode.OnCommit));

    private FrameworkElement? _editor;
    private bool _refreshing;

    static PropertyItemEditor()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyItemEditor),
            new FrameworkPropertyMetadata(typeof(PropertyItemEditor)));
    }

    /// <summary>Occurs after the user changed the value (already written to the object).</summary>
    public event EventHandler<PropertyValueChangedEventArgs> ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    /// <summary>The edited property item.</summary>
    public PropertyItem? Item
    {
        get => (PropertyItem?)GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    /// <summary>
    /// When edits are pushed: <see cref="PropertyGridCommitMode.OnCommit"/> (Enter, focus
    /// loss; the default) or <see cref="PropertyGridCommitMode.Immediate"/> (every keystroke).
    /// </summary>
    public PropertyGridCommitMode CommitMode
    {
        get => (PropertyGridCommitMode)GetValue(CommitModeProperty);
        set => SetValue(CommitModeProperty, value);
    }

    /// <inheritdoc />
    protected override void OnVisualParentChanged(DependencyObject oldParent)
    {
        base.OnVisualParentChanged(oldParent);
        EnsureEditor();
    }

    /// <summary>Builds the editor element for the item's kind.</summary>
    private void EnsureEditor()
    {
        if (_editor is not null || Item is not { } item)
        {
            return;
        }

        _editor = item.EditorKind switch
        {
            PropertyItemEditorKind.Boolean => CreateBooleanEditor(item),
            PropertyItemEditorKind.Enum => CreateEnumEditor(item),
            PropertyItemEditorKind.Date => CreateDateEditor(item),
            PropertyItemEditorKind.MultiLineText => CreateTextEditor(item, multiLine: true),
            PropertyItemEditorKind.Numeric => CreateTextEditor(item, multiLine: false, numeric: true),
            PropertyItemEditorKind.Text => CreateTextEditor(item, multiLine: false),
            _ => CreateReadOnlyEditor(item),
        };

        Content = _editor;
    }

    private CheckBox CreateBooleanEditor(PropertyItem item)
    {
        var checkBox = new CheckBox
        {
            IsChecked = item.GetValue() is bool flag && flag,
            IsEnabled = !item.IsReadOnly,
            VerticalAlignment = VerticalAlignment.Center,
        };
        checkBox.Checked += (_, _) => Commit(checkBox, checkBox.IsChecked);
        checkBox.Unchecked += (_, _) => Commit(checkBox, checkBox.IsChecked);
        return checkBox;
    }

    private ComboBox CreateEnumEditor(PropertyItem item)
    {
        var enumType = Nullable.GetUnderlyingType(item.PropertyType) ?? item.PropertyType;
        var combo = new ComboBox
        {
            ItemsSource = Enum.GetValues(enumType),
            IsEnabled = !item.IsReadOnly,
        };
        combo.SelectedItem = item.GetValue();
        combo.SelectionChanged += (_, _) => Commit(combo, combo.SelectedItem);
        return combo;
    }

    private DatePicker CreateDateEditor(PropertyItem item)
    {
        var picker = new DatePicker
        {
            SelectedDate = item.GetValue() as DateTime?,
            IsEnabled = !item.IsReadOnly,
        };
        picker.SelectedDateChanged += (_, _) => Commit(picker, picker.SelectedDate);
        return picker;
    }

    private TextBox CreateTextEditor(PropertyItem item, bool multiLine, bool numeric = false)
    {
        var textBox = new TextBox
        {
            Text = item.GetValueText(),
            IsReadOnly = item.IsReadOnly,
            VerticalAlignment = VerticalAlignment.Top,
        };
        if (numeric)
        {
            textBox.TextAlignment = TextAlignment.Right;
        }

        if (multiLine)
        {
            textBox.AcceptsReturn = true;
            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.MinHeight = 48;
            textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        }

        if (CommitMode == PropertyGridCommitMode.Immediate && !item.IsReadOnly)
        {
            textBox.TextChanged += (_, _) => Commit(textBox, textBox.Text);
        }

        textBox.LostFocus += (_, _) => Commit(textBox, textBox.Text);
        textBox.KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                // Cancel: restore the current value, discard the typed text.
                RefreshValue();
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Enter && !multiLine)
            {
                Commit(textBox, textBox.Text);
                e.Handled = true;
            }
        };
        return textBox;
    }

    private static TextBlock CreateReadOnlyEditor(PropertyItem item)
    {
        var block = new TextBlock
        {
            Text = item.GetValueText(),
            Opacity = 0.75,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        block.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, VCWpfControls.Grid.GridResources.TextBrushKey);
        return block;
    }

    /// <summary>Pushes a user edit through reflection and raises the routed event.</summary>
    private void Commit(FrameworkElement source, object? rawValue)
    {
        if (_refreshing || Item is not { } item)
        {
            return;
        }

        var targetType = Nullable.GetUnderlyingType(item.PropertyType) ?? item.PropertyType;
        object? converted;
        try
        {
            converted = ConvertValue(item, rawValue, targetType);
        }
        catch (Exception exception) when (
            exception is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            // Invalid input: revert the editor to the current value.
            RefreshValue();
            return;
        }

        var oldValue = item.GetValue();
        if (Equals(oldValue, converted))
        {
            return;
        }

        try
        {
            item.SetValue(converted);
        }
        catch (System.Reflection.TargetInvocationException)
        {
            RefreshValue();
            return;
        }

        RaiseEvent(new PropertyValueChangedEventArgs(item, oldValue, converted)
        {
            RoutedEvent = ValueChangedEvent,
            Source = source,
        });
    }

    private static object? ConvertValue(PropertyItem item, object? rawValue, Type targetType)
    {
        if (rawValue is null || targetType.IsInstanceOfType(rawValue))
        {
            return rawValue;
        }

        if (rawValue is string text)
        {
            if (text.Length == 0 && Nullable.GetUnderlyingType(targetType) is not null)
            {
                return null;
            }

            // A registered TypeConverter (Guid, custom structs...) parses the text.
            if (item.HasStringConverter)
            {
                return item.Converter.ConvertFromString(null, CultureInfo.CurrentCulture, text);
            }
        }

        return Convert.ChangeType(rawValue, targetType, CultureInfo.CurrentCulture);
    }

    /// <summary>Test conversion entry: parses text through the item's converter pipeline.</summary>
    internal static object? ConvertForTest(PropertyItem item, string text)
    {
        var targetType = Nullable.GetUnderlyingType(item.PropertyType) ?? item.PropertyType;
        return ConvertValue(item, text, targetType);
    }

    /// <summary>The built editor element (test hook).</summary>
    internal object? EditorContentForTest() => _editor;

    /// <summary>Reloads the editor content from the current object value (no commit).</summary>
    internal void RefreshValue()
    {
        if (Item is not { } item || _editor is null)
        {
            return;
        }

        _refreshing = true;
        try
        {
            switch (_editor)
            {
                case CheckBox checkBox:
                    checkBox.IsChecked = item.GetValue() is bool flag && flag;
                    break;
                case ComboBox combo:
                    combo.SelectedItem = item.GetValue();
                    break;
                case DatePicker picker:
                    picker.SelectedDate = item.GetValue() as DateTime?;
                    break;
                case TextBox textBox:
                    textBox.Text = item.GetValueText();
                    break;
                case TextBlock block:
                    block.Text = item.GetValueText();
                    break;
            }
        }
        finally
        {
            _refreshing = false;
        }
    }
}
