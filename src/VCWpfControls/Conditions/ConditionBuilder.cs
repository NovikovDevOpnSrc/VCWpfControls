using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Grid.Data;
using VCWpfControls.Localization;

namespace VCWpfControls.Conditions;

/// <summary>
/// A visual editor for <see cref="ConditionNode"/> trees (stage B of the condition
/// editor): recursive And/Or groups with typed value editors, a text tab parsing the
/// <see cref="ConditionParser"/> language with positioned error reporting, and a live
/// preview. Built in code (like the filter menu/chooser) so it themes through the
/// standard controls.
/// </summary>
public class ConditionBuilder : ContentControl
{
    private static class Boxes
    {
        public static readonly object False = false;
        public static readonly object True = true;
    }

    /// <summary>Identifies the <see cref="Schema"/> dependency property.</summary>
    public static readonly DependencyProperty SchemaProperty = DependencyProperty.Register(
        nameof(Schema), typeof(ConditionSchema), typeof(ConditionBuilder),
        new PropertyMetadata(null, OnStructureChanged));

    /// <summary>Identifies the <see cref="Condition"/> dependency property.</summary>
    public static readonly DependencyProperty ConditionProperty = DependencyProperty.Register(
        nameof(Condition), typeof(ConditionNode), typeof(ConditionBuilder),
        new PropertyMetadata(null, OnStructureChanged));

    /// <summary>Identifies the <see cref="IsTextMode"/> dependency property.</summary>
    public static readonly DependencyProperty IsTextModeProperty = DependencyProperty.Register(
        nameof(IsTextMode), typeof(bool), typeof(ConditionBuilder),
        new PropertyMetadata(Boxes.False, OnStructureChanged));

    /// <summary>Identifies the <see cref="PreviewText"/> dependency property.</summary>
    public static readonly DependencyProperty PreviewTextProperty = DependencyProperty.Register(
        nameof(PreviewText), typeof(string), typeof(ConditionBuilder), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="ErrorText"/> dependency property.</summary>
    public static readonly DependencyProperty ErrorTextProperty = DependencyProperty.Register(
        nameof(ErrorText), typeof(string), typeof(ConditionBuilder), new PropertyMetadata(string.Empty));

    /// <summary>Creates the builder.</summary>
    public ConditionBuilder()
    {
        Condition = new ConditionGroup(ConditionGroupOperator.And);
    }

    /// <summary>Raised after the user edited the tree or the text parsed successfully.</summary>
    public event EventHandler? ConditionChanged;

    /// <summary>The filterable fields; defaults to an empty schema.</summary>
    public ConditionSchema? Schema
    {
        get => (ConditionSchema?)GetValue(SchemaProperty);
        set => SetValue(SchemaProperty, value);
    }

    /// <summary>The edited condition tree (auto-created And group; two-way bindable).</summary>
    public ConditionNode? Condition
    {
        get => (ConditionNode?)GetValue(ConditionProperty);
        set => SetValue(ConditionProperty, value);
    }

    /// <summary>Whether the text tab (instead of the visual tree) is shown.</summary>
    public bool IsTextMode
    {
        get => (bool)GetValue(IsTextModeProperty);
        set => SetValue(IsTextModeProperty, value);
    }

    /// <summary>The live preview (canonical text form of the tree).</summary>
    public string PreviewText
    {
        get => (string)GetValue(PreviewTextProperty);
        private set => SetValue(PreviewTextProperty, value);
    }

    /// <summary>The last text-parse error (empty when the text is valid).</summary>
    public string ErrorText
    {
        get => (string)GetValue(ErrorTextProperty);
        private set => SetValue(ErrorTextProperty, value);
    }

    private ConditionSchema EffectiveSchema => Schema ?? new ConditionSchema();

    private static void OnStructureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((ConditionBuilder)d).Rebuild();

    /// <summary>Rebuilds the editor surface (visual tree or text box) from the model.</summary>
    public void Rebuild()
    {
        var root = new DockPanel();

        var builderTab = new RadioButton
        {
            Content = GridStrings.ConditionBuilderTab,
            IsChecked = !IsTextMode,
            Margin = new Thickness(0, 0, 12, 0),
            GroupName = "ConditionMode",
        };
        builderTab.Checked += (_, _) =>
        {
            if (IsTextMode)
            {
                SetCurrentValue(IsTextModeProperty, Boxes.False);
            }
        };

        var textTab = new RadioButton
        {
            Content = GridStrings.ConditionTextTab,
            IsChecked = IsTextMode,
            GroupName = "ConditionMode",
        };
        textTab.Checked += (_, _) =>
        {
            if (!IsTextMode)
            {
                SetCurrentValue(IsTextModeProperty, Boxes.True);
            }
        };

        var tabs = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
        tabs.Children.Add(builderTab);
        tabs.Children.Add(textTab);

        var preview = new TextBlock
        {
            Text = GridStrings.ConditionPreview + ": " + ConditionParser.Format(Condition),
            Opacity = 0.7,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 0),
        };
        SetPreview(preview);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(0) };
        if (IsTextMode)
        {
            scroll.Content = BuildTextEditor();
        }
        else
        {
            scroll.Content = Condition is ConditionGroup group
                ? BuildGroupView(group, 0)
                : BuildSingleNodeView();
        }

        DockPanel.SetDock(tabs, Dock.Top);
        DockPanel.SetDock(preview, Dock.Bottom);
        root.Children.Add(tabs);
        root.Children.Add(preview);
        root.Children.Add(scroll);
        Content = root;
        PreviewText = ConditionParser.Format(Condition);
    }

    private UIElement BuildSingleNodeView()
    {
        // A bare expression at the root: show it inside an implicit And group row set.
        var host = new ConditionGroup(ConditionGroupOperator.And) { Children = { Condition! } };
        var panel = new StackPanel();
        panel.Children.Add(BuildGroupView(host, 0));
        return panel;
    }

    private UIElement BuildTextEditor()
    {
        var box = new TextBox
        {
            Text = ConditionParser.Format(Condition),
            AcceptsReturn = false,
            Margin = new Thickness(0, 2, 0, 4),
        };
        var error = new TextBlock { Foreground = System.Windows.Media.Brushes.Firebrick, TextWrapping = TextWrapping.Wrap };
        box.LostFocus += (_, _) => ApplyText(box.Text, error);
        box.KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                ApplyText(box.Text, error);
                e.Handled = true;
            }
        };
        var panel = new StackPanel();
        panel.Children.Add(box);
        panel.Children.Add(error);
        return panel;
    }

    private void ApplyText(string text, TextBlock error)
    {
        try
        {
            Condition = ConditionParser.Parse(text);
            ErrorText = string.Empty;
            error.Text = string.Empty;
            ConditionChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (ConditionParseException exception)
        {
            ErrorText = exception.Message;
            error.Text = exception.Message;
        }
    }

    private Border BuildGroupView(ConditionGroup group, int depth)
    {
        var panel = new StackPanel { Margin = new Thickness(depth * 16, 0, 0, 0) };

        var andOr = new ComboBox { Width = 120, Margin = new Thickness(0, 0, 8, 4) };
        andOr.Items.Add(new ComboBoxItem { Content = GridStrings.ConditionAll, Tag = ConditionGroupOperator.And });
        andOr.Items.Add(new ComboBoxItem { Content = GridStrings.ConditionAny, Tag = ConditionGroupOperator.Or });
        andOr.SelectedIndex = group.OperatorKind == ConditionGroupOperator.And ? 0 : 1;
        andOr.SelectionChanged += (_, _) =>
        {
            if (andOr.SelectedItem is ComboBoxItem { Tag: ConditionGroupOperator kind })
            {
                group.OperatorKind = kind;
                RaiseChanged();
            }
        };

        var addCondition = new Button { Content = GridStrings.ConditionAdd, Margin = new Thickness(0, 0, 8, 4), Padding = new Thickness(8, 2, 8, 2) };
        addCondition.Click += (_, _) =>
        {
            var field = EffectiveSchema.Fields.FirstOrDefault();
            group.Children.Add(new ConditionExpression(
                field?.Name ?? string.Empty,
                GridFilterOperator.Equals,
                field is null ? null : DefaultValueOf(field)));
            Rebuild();
            RaiseChanged();
        };

        var addGroup = new Button { Content = GridStrings.ConditionAddGroup, Margin = new Thickness(0, 0, 8, 4), Padding = new Thickness(8, 2, 8, 2) };
        addGroup.Click += (_, _) =>
        {
            group.Children.Add(new ConditionGroup(ConditionGroupOperator.And));
            Rebuild();
            RaiseChanged();
        };

        var header = new StackPanel { Orientation = Orientation.Horizontal };
        header.Children.Add(andOr);
        header.Children.Add(addCondition);
        header.Children.Add(addGroup);
        panel.Children.Add(header);

        foreach (var child in group.Children)
        {
            if (child is ConditionGroup nested)
            {
                panel.Children.Add(BuildGroupView(nested, depth + 1));
            }
            else if (child is ConditionExpression expression)
            {
                panel.Children.Add(BuildExpressionRow(group, expression, depth + 1));
            }
        }

        return new Border
        {
            BorderBrush = System.Windows.Media.Brushes.LightGray,
            BorderThickness = new Thickness(depth == 0 ? 0 : 1, 0, 0, 0),
            Padding = new Thickness(depth == 0 ? 0 : 8, 0, 0, 4),
            Child = panel,
        };
    }

    private UIElement BuildExpressionRow(ConditionGroup parent, ConditionExpression expression, int depth)
    {
        var schema = EffectiveSchema;
        var field = schema.Find(expression.FieldName) ?? schema.Fields.FirstOrDefault();

        var fieldCombo = new ComboBox { Width = 130, Margin = new Thickness(0, 0, 6, 4), DisplayMemberPath = nameof(ConditionField.DisplayName) };
        fieldCombo.ItemsSource = schema.Fields;
        if (field is not null)
        {
            fieldCombo.SelectedItem = field;
        }

        var operatorCombo = new ComboBox { Width = 120, Margin = new Thickness(0, 0, 6, 4) };
        var valueHost = new StackPanel { Orientation = Orientation.Horizontal };
        var refreshing = false;

        void RefreshRow()
        {
            if (refreshing)
            {
                return;
            }

            refreshing = true;
            try
            {
                var current = fieldCombo.SelectedItem as ConditionField ?? field;
                if (current is not null && !string.Equals(expression.FieldName, current.Name, StringComparison.Ordinal))
                {
                    expression.FieldName = current.Name;
                    expression.Value = DefaultValueOf(current);
                    expression.Value2 = null;
                    if (current.ValueType != typeof(string))
                    {
                        expression.OperatorKind = GridFilterOperator.Equals;
                    }
                }

                operatorCombo.Items.Clear();
                foreach (var kind in OperatorsFor(current))
                {
                    operatorCombo.Items.Add(new ComboBoxItem { Content = OperatorCaption(kind), Tag = kind });
                }

                operatorCombo.SelectedIndex = Math.Max(0, operatorCombo.Items.OfType<ComboBoxItem>().ToList().FindIndex(i => Equals(i.Tag, expression.OperatorKind)));

                valueHost.Children.Clear();
                foreach (var editor in BuildValueEditors(expression, current))
                {
                    valueHost.Children.Add(editor);
                }
            }
            finally
            {
                refreshing = false;
            }
        }

        fieldCombo.SelectionChanged += (_, _) => { RefreshRow(); RaiseChanged(); };
        operatorCombo.SelectionChanged += (_, _) =>
        {
            if (operatorCombo.SelectedItem is ComboBoxItem { Tag: GridFilterOperator kind })
            {
                expression.OperatorKind = kind;
                RefreshRow();
                RaiseChanged();
            }
        };

        RefreshRow();

        var remove = new Button { Content = "✕", Margin = new Thickness(6, 0, 0, 4), Padding = new Thickness(6, 1, 6, 1), ToolTip = GridStrings.ConditionRemove };
        remove.Click += (_, _) =>
        {
            parent.Children.Remove(expression);
            Rebuild();
            RaiseChanged();
        };

        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(depth * 16, 0, 0, 0) };
        row.Children.Add(fieldCombo);
        row.Children.Add(operatorCombo);
        row.Children.Add(valueHost);
        row.Children.Add(remove);
        return row;
    }

    private IEnumerable<FrameworkElement> BuildValueEditors(ConditionExpression expression, ConditionField? field)
    {
        if (expression.OperatorKind is GridFilterOperator.IsNull or GridFilterOperator.IsNotNull)
        {
            yield break;
        }

        var valueType = field is null ? typeof(string) : (Nullable.GetUnderlyingType(field.ValueType) ?? field.ValueType);
        if (expression.OperatorKind is GridFilterOperator.Between or GridFilterOperator.NotBetween)
        {
            yield return BuildSingleEditor(expression, valueType, isLower: true);
            yield return BuildSingleEditor(expression, valueType, isLower: false);
            yield break;
        }

        if (expression.OperatorKind is GridFilterOperator.In or GridFilterOperator.NotIn)
        {
            var list = (expression.Value as System.Collections.IEnumerable)?
                .Cast<object?>()
                .Select(v => v is null ? string.Empty : Convert.ToString(v, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty)
                .ToList() ?? new List<string>();
            var box = new TextBox { Width = 180, Margin = new Thickness(0, 0, 6, 4), Text = string.Join(", ", list) };
            box.LostFocus += (_, _) =>
            {
                var parts = box.Text
                    .Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
                    .Select(part => GridClipboard.TryConvert(part, valueType, out var parsed) ? parsed : part)
                    .ToList();
                expression.Value = parts;
                RaiseChanged();
            };
            yield return box;
            yield break;
        }

        yield return BuildSingleEditor(expression, valueType, isLower: true);
    }

    private FrameworkElement BuildSingleEditor(ConditionExpression expression, Type valueType, bool isLower)
    {
        if (valueType == typeof(bool))
        {
            var checkBox = new CheckBox { Margin = new Thickness(0, 2, 6, 4), IsChecked = expressionValue() is true };
            checkBox.Checked += (_, _) => setExpressionValue(true);
            checkBox.Unchecked += (_, _) => setExpressionValue(false);
            return checkBox;
        }

        if (valueType == typeof(DateTime))
        {
            var picker = new DatePicker { Margin = new Thickness(0, 0, 6, 4), SelectedDate = expressionValue() as DateTime? };
            picker.SelectedDateChanged += (_, _) => setExpressionValue(picker.SelectedDate);
            return picker;
        }

        if (valueType.IsEnum)
        {
            var combo = new ComboBox { Width = 130, Margin = new Thickness(0, 0, 6, 4), ItemsSource = Enum.GetValues(valueType) };
            combo.SelectedItem = expressionValue();
            combo.SelectionChanged += (_, _) => setExpressionValue(combo.SelectedItem);
            return combo;
        }

        var text = expressionValue() is { } value && value is not string
            ? Convert.ToString(value, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty
            : expressionValue() as string ?? string.Empty;
        var textBox = new TextBox { Width = 130, Margin = new Thickness(0, 0, 6, 4), Text = text };
        textBox.LostFocus += (_, _) => CommitText(textBox, expression, valueType, isLower);
        textBox.KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                CommitText(textBox, expression, valueType, isLower);
                e.Handled = true;
            }
        };
        return textBox;

        object? expressionValue() => isLower ? expression.Value : expression.Value2;
        void setExpressionValue(object? parsed)
        {
            if (isLower)
            {
                expression.Value = parsed;
            }
            else
            {
                expression.Value2 = parsed;
            }

            RaiseChanged();
        }
    }

    private void CommitText(TextBox box, ConditionExpression expression, Type valueType, bool isLower)
    {
        // Empty text means null; other texts parse through the shared clipboard converter.
        object? parsed = string.IsNullOrEmpty(box.Text)
            ? null
            : GridClipboard.TryConvert(box.Text.Trim(), valueType, out var converted)
                ? converted
                : box.Text;
        if (isLower)
        {
            expression.Value = parsed;
        }
        else
        {
            expression.Value2 = parsed;
        }

        RaiseChanged();
    }

    private static object? DefaultValueOf(ConditionField field)
    {
        var type = Nullable.GetUnderlyingType(field.ValueType) ?? field.ValueType;
        if (type == typeof(bool) || type.IsEnum || Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.Decimal)
        {
            return Activator.CreateInstance(type);
        }

        return type == typeof(DateTime) ? DateTime.Today : string.Empty;
    }

    private static IEnumerable<GridFilterOperator> OperatorsFor(ConditionField? field)
    {
        if (field is null)
        {
            yield return GridFilterOperator.Equals;
            yield break;
        }

        var type = Nullable.GetUnderlyingType(field.ValueType) ?? field.ValueType;
        if (type == typeof(string))
        {
            yield return GridFilterOperator.Equals;
            yield return GridFilterOperator.NotEquals;
            yield return GridFilterOperator.Contains;
            yield return GridFilterOperator.NotContains;
            yield return GridFilterOperator.StartsWith;
            yield return GridFilterOperator.EndsWith;
            yield return GridFilterOperator.In;
            yield return GridFilterOperator.IsNull;
            yield return GridFilterOperator.IsNotNull;
            yield break;
        }

        if (type == typeof(bool) || type.IsEnum)
        {
            yield return GridFilterOperator.Equals;
            yield return GridFilterOperator.NotEquals;
            yield return GridFilterOperator.IsNull;
            yield break;
        }

        // Numbers and dates.
        yield return GridFilterOperator.Equals;
        yield return GridFilterOperator.NotEquals;
        yield return GridFilterOperator.GreaterThan;
        yield return GridFilterOperator.GreaterThanOrEqual;
        yield return GridFilterOperator.LessThan;
        yield return GridFilterOperator.LessThanOrEqual;
        yield return GridFilterOperator.Between;
        yield return GridFilterOperator.In;
        yield return GridFilterOperator.IsNull;
        yield return GridFilterOperator.IsNotNull;
    }

    private static string OperatorCaption(GridFilterOperator kind)
        => kind switch
        {
            GridFilterOperator.Equals => "=",
            GridFilterOperator.NotEquals => "!=",
            GridFilterOperator.Contains => "contains",
            GridFilterOperator.NotContains => "not contains",
            GridFilterOperator.StartsWith => "starts with",
            GridFilterOperator.EndsWith => "ends with",
            GridFilterOperator.GreaterThan => ">",
            GridFilterOperator.GreaterThanOrEqual => ">=",
            GridFilterOperator.LessThan => "<",
            GridFilterOperator.LessThanOrEqual => "<=",
            GridFilterOperator.Between => "between",
            GridFilterOperator.NotBetween => "not between",
            GridFilterOperator.In => "in",
            GridFilterOperator.NotIn => "not in",
            GridFilterOperator.IsNull => "is null",
            GridFilterOperator.IsNotNull => "is not null",
            _ => kind.ToString(),
        };

    private void SetPreview(TextBlock preview)
        => preview.Text = GridStrings.ConditionPreview + ": " + ConditionParser.Format(Condition);

    private void RaiseChanged()
    {
        PreviewText = ConditionParser.Format(Condition);
        ConditionChanged?.Invoke(this, EventArgs.Empty);
    }
}
