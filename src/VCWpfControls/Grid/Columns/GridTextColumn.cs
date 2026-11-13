using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace VCWpfControls.Grid.Columns;

/// <summary>Column displaying text with an optional format, edited via a text box.</summary>
public class GridTextColumn : GridColumnBase
{
    /// <summary>Creates the text display element: a <see cref="TextBlock"/> bound to the field.</summary>
    protected override FrameworkElement GenerateElementCore()
    {
        var textBlock = new TextBlock
        {
            Margin = new Thickness(8, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        textBlock.SetBinding(TextBlock.TextProperty, CreateCellBinding(BindingMode.OneWay));
        return textBlock;
    }

    /// <summary>Creates the text editing element: a <see cref="TextBox"/> with two-way binding.</summary>
    protected override FrameworkElement GenerateEditingElementCore()
    {
        var textBox = new TextBox
        {
            Margin = new Thickness(7, 0, 5, 0),
            VerticalAlignment = VerticalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        textBox.SetBinding(TextBox.TextProperty, CreateCellBinding(BindingMode.TwoWay));
        return textBox;
    }

    internal override System.Windows.DependencyProperty GetEditingCommitProperty() => TextBox.TextProperty;

    internal override void RebindEditingElement(FrameworkElement element, object dataItem)
    {
        if (element is TextBox textBox)
        {
            textBox.SetBinding(TextBox.TextProperty, CreateEditingBinding(dataItem));
        }
    }
}

/// <summary>Column for numeric values; right-aligned by default (header follows), numeric text box while editing.</summary>
public class GridNumericColumn : GridColumnBase
{
    static GridNumericColumn()
    {
        // Numbers read best right-aligned; the header follows the same alignment.
        HorizontalContentAlignmentProperty.OverrideMetadata(
            typeof(GridNumericColumn), new PropertyMetadata(HorizontalAlignment.Right));
    }

    /// <summary>Creates the numeric display element.</summary>
    protected override FrameworkElement GenerateElementCore()
    {
        var textBlock = new TextBlock
        {
            // Right-aligned numbers: padding on the right edge of the slot.
            Margin = new Thickness(6, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Right,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        textBlock.SetBinding(TextBlock.HorizontalAlignmentProperty,
            new System.Windows.Data.Binding(nameof(HorizontalContentAlignment)) { Source = this });
        textBlock.SetBinding(TextBlock.TextProperty, CreateCellBinding(BindingMode.OneWay));
        return textBlock;
    }

    /// <summary>Creates the numeric editing element.</summary>
    protected override FrameworkElement GenerateEditingElementCore()
    {
        var textBox = new TextBox
        {
            Margin = new Thickness(5, 0, 7, 0),
            VerticalAlignment = VerticalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Right,
        };
        textBox.SetBinding(TextBox.TextProperty, CreateCellBinding(BindingMode.TwoWay));
        return textBox;
    }

    internal override System.Windows.DependencyProperty GetEditingCommitProperty() => TextBox.TextProperty;

    internal override void RebindEditingElement(FrameworkElement element, object dataItem)
    {
        if (element is TextBox textBox)
        {
            textBox.SetBinding(TextBox.TextProperty, CreateEditingBinding(dataItem));
        }
    }
}

/// <summary>Column for boolean values displayed as a check box.</summary>
public class GridCheckBoxColumn : GridColumnBase
{
    /// <summary>Creates the check box element (interactive unless the column is read-only).</summary>
    protected override FrameworkElement GenerateElementCore()
    {
        var checkBox = new CheckBox
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsTabStop = false,
            Focusable = false,
        };
        // Two-way with immediate trigger so clicks commit at once; read-only columns
        // bind one-way (the source property may not have a setter at all).
        var binding = CreateCellBinding(ReadOnly ? BindingMode.OneWay : BindingMode.TwoWay);
        binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        checkBox.SetBinding(CheckBox.IsCheckedProperty, binding);
        return checkBox;
    }
}

/// <summary>Column picking a value from a list, edited via a combo box.</summary>
public class GridComboBoxColumn : GridColumnBase
{
    /// <summary>Identifies the <see cref="ItemsSource"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(GridComboBoxColumn), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="DisplayMemberPath"/> dependency property.</summary>
    public static readonly DependencyProperty DisplayMemberPathProperty = DependencyProperty.Register(
        nameof(DisplayMemberPath), typeof(string), typeof(GridComboBoxColumn), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="SelectedValuePath"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedValuePathProperty = DependencyProperty.Register(
        nameof(SelectedValuePath), typeof(string), typeof(GridComboBoxColumn), new PropertyMetadata(null));

    /// <summary>The list of items to choose from (bindable).</summary>
    public System.Collections.IEnumerable? ItemsSource
    {
        get => (System.Collections.IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Path to the display text within the list items.</summary>
    public string? DisplayMemberPath
    {
        get => (string?)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    /// <summary>Path to the value within the list items; when set, binds cell values via this path.</summary>
    public string? SelectedValuePath
    {
        get => (string?)GetValue(SelectedValuePathProperty);
        set => SetValue(SelectedValuePathProperty, value);
    }

    /// <summary>Creates the combo display element (shows the selected item's display text).</summary>
    protected override FrameworkElement GenerateElementCore()
    {
        var textBlock = new TextBlock
        {
            Margin = new Thickness(8, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        textBlock.SetBinding(
            TextBlock.TextProperty,
            new Binding(FieldName)
            {
                Mode = BindingMode.OneWay,
                Converter = new ComboBoxDisplayConverter(this),
            });
        return textBlock;
    }

    /// <summary>Creates the combo box editing element.</summary>
    protected override FrameworkElement GenerateEditingElementCore()
    {
        var comboBox = new ComboBox
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center,
            ItemsSource = ItemsSource,
            // null path must STAY null: coercing it to "" makes WPF look up a property
            // named "" on each item — enums/strings have none, so the editor rendered
            // EMPTY when editing began (the selected value never displayed).
            DisplayMemberPath = DisplayMemberPath,
        };

        if (string.IsNullOrEmpty(SelectedValuePath))
        {
            comboBox.SetBinding(ComboBox.SelectedItemProperty, CreateCellBinding(BindingMode.TwoWay));
        }
        else
        {
            var binding = CreateCellBinding(BindingMode.TwoWay);
            comboBox.SelectedValuePath = SelectedValuePath;
            comboBox.SetBinding(ComboBox.SelectedValueProperty, binding);
        }

        return comboBox;
    }

    internal override System.Windows.DependencyProperty GetEditingCommitProperty()
        => string.IsNullOrEmpty(SelectedValuePath) ? ComboBox.SelectedItemProperty : ComboBox.SelectedValueProperty;

    internal override void RebindEditingElement(FrameworkElement element, object dataItem)
    {
        if (element is ComboBox comboBox)
        {
            var binding = CreateEditingBinding(dataItem);
            if (string.IsNullOrEmpty(SelectedValuePath))
            {
                comboBox.SetBinding(ComboBox.SelectedItemProperty, binding);
            }
            else
            {
                comboBox.SelectedValuePath = SelectedValuePath;
                comboBox.SetBinding(ComboBox.SelectedValueProperty, binding);
            }
        }
    }

    private sealed class ComboBoxDisplayConverter : System.Windows.Data.IValueConverter
    {
        private readonly GridComboBoxColumn _column;

        public ComboBoxDisplayConverter(GridComboBoxColumn column) => _column = column;

        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (_column.ItemsSource is not { } items)
            {
                return value;
            }

            var selectedValuePath = _column.SelectedValuePath;
            foreach (var item in items)
            {
                if (item is null)
                {
                    continue;
                }

                var candidate = string.IsNullOrEmpty(selectedValuePath)
                    ? item
                    : Data.PropertyAccessor.GetOrCreate(item.GetType(), selectedValuePath!).GetValue(item);
                if (Equals(candidate, value))
                {
                    return string.IsNullOrEmpty(_column.DisplayMemberPath)
                        ? item
                        : Data.PropertyAccessor.GetOrCreate(item.GetType(), _column.DisplayMemberPath!).GetValue(item);
                }
            }

            return value;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }
}

/// <summary>Text column edited via the AutoComplete editor (suggestions from ItemsSource).</summary>
public class GridAutoCompleteColumn : GridColumnBase
{
    /// <summary>Identifies the <see cref="ItemsSource"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(GridAutoCompleteColumn), new PropertyMetadata(null));

    /// <summary>The suggestion source.</summary>
    public System.Collections.IEnumerable? ItemsSource
    {
        get => (System.Collections.IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Property path of the match text for non-string items.</summary>
    public string? ItemTextPath { get; set; }

    /// <summary>Creates the text display element.</summary>
    protected override FrameworkElement GenerateElementCore()
    {
        var textBlock = new TextBlock
        {
            Margin = new Thickness(8, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        textBlock.SetBinding(TextBlock.TextProperty, CreateCellBinding(BindingMode.OneWay));
        return textBlock;
    }

    /// <summary>Creates the editing element: the AutoComplete editor.</summary>
    protected override FrameworkElement GenerateEditingElementCore()
    {
        var editor = new VCWpfControls.Editors.AutoComplete
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            ItemTextPath = ItemTextPath,
            ItemsSource = ItemsSource ?? Array.Empty<string>(),
        };
        editor.SetBinding(VCWpfControls.Editors.AutoComplete.TextProperty, CreateCellBinding(BindingMode.TwoWay));
        return editor;
    }

    /// <inheritdoc />
    internal override System.Windows.DependencyProperty? GetEditingCommitProperty()
        => VCWpfControls.Editors.AutoComplete.TextProperty;

    /// <inheritdoc />
    internal override void RebindEditingElement(FrameworkElement element, object dataItem)
    {
        if (element is VCWpfControls.Editors.AutoComplete editor)
        {
            editor.SetBinding(VCWpfControls.Editors.AutoComplete.TextProperty, CreateEditingBinding(dataItem));
        }
    }
}

/// <summary>Column for date values, edited via a date picker.</summary>
public class GridDateColumn : GridColumnBase
{
    /// <summary>Creates the date display element with culture-aware formatting.</summary>
    protected override FrameworkElement GenerateElementCore()
    {
        var textBlock = new TextBlock
        {
            Margin = new Thickness(8, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        textBlock.SetBinding(
            TextBlock.TextProperty,
            new Binding(FieldName)
            {
                Mode = BindingMode.OneWay,
                StringFormat = string.IsNullOrEmpty(StringFormat) ? "d" : StringFormat,
            });
        return textBlock;
    }

    /// <summary>Creates the date editing element: a <see cref="DatePicker"/>.</summary>
    protected override FrameworkElement GenerateEditingElementCore()
    {
        var datePicker = new DatePicker
        {
            VerticalAlignment = VerticalAlignment.Center,
        };
        datePicker.SetBinding(DatePicker.SelectedDateProperty, CreateCellBinding(BindingMode.TwoWay));
        return datePicker;
    }

    internal override System.Windows.DependencyProperty GetEditingCommitProperty() => DatePicker.SelectedDateProperty;

    internal override void RebindEditingElement(FrameworkElement element, object dataItem)
    {
        if (element is DatePicker datePicker)
        {
            datePicker.SetBinding(DatePicker.SelectedDateProperty, CreateEditingBinding(dataItem));
        }
    }
}

/// <summary>Column fully defined by <see cref="GridColumnBase.CellTemplate"/> and <c>CellEditingTemplate</c>.</summary>
public class GridTemplateColumn : GridColumnBase
{
    /// <summary>Throws: a template column requires <see cref="GridColumnBase.CellTemplate"/>.</summary>
    protected override FrameworkElement GenerateElementCore()
        => throw new InvalidOperationException(
            $"{nameof(GridTemplateColumn)} requires '{nameof(CellTemplate)}' to be set.");
}
