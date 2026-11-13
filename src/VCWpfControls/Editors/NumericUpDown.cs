using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace VCWpfControls.Editors;

/// <summary>
/// A numeric editor with up/down spinner buttons: typed or pasted text is parsed with the
/// current culture, the value is clamped to <see cref="MinValue"/>/<see cref="MaxValue"/>,
/// arrows/PageUp/PageDown/mouse wheel and click-hold repeat buttons apply
/// <see cref="Increment"/>. Two-way bindable <see cref="Value"/>.
/// </summary>
public class NumericUpDown : Control
{
    /// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(decimal), typeof(NumericUpDown),
        new FrameworkPropertyMetadata(0m, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValuePropertyChanged, CoerceValue));

    /// <summary>Identifies the <see cref="MinValue"/> dependency property.</summary>
    public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
        nameof(MinValue), typeof(decimal), typeof(NumericUpDown),
        new PropertyMetadata(decimal.MinValue, OnBoundsChanged));

    /// <summary>Identifies the <see cref="MaxValue"/> dependency property.</summary>
    public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
        nameof(MaxValue), typeof(decimal), typeof(NumericUpDown),
        new PropertyMetadata(decimal.MaxValue, OnBoundsChanged));

    /// <summary>Identifies the <see cref="Increment"/> dependency property.</summary>
    public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register(
        nameof(Increment), typeof(decimal), typeof(NumericUpDown), new PropertyMetadata(1m));

    /// <summary>Identifies the <see cref="DecimalPlaces"/> dependency property.</summary>
    public static readonly DependencyProperty DecimalPlacesProperty = DependencyProperty.Register(
        nameof(DecimalPlaces), typeof(int), typeof(NumericUpDown),
        new PropertyMetadata(-1, OnDisplayPropertyChanged));

    /// <summary>Identifies the <see cref="FormatString"/> dependency property.</summary>
    public static readonly DependencyProperty FormatStringProperty = DependencyProperty.Register(
        nameof(FormatString), typeof(string), typeof(NumericUpDown),
        new PropertyMetadata(null, OnDisplayPropertyChanged));

    /// <summary>Identifies the <see cref="IsReadOnly"/> dependency property.</summary>
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly), typeof(bool), typeof(NumericUpDown), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="ValueText"/> read-only dependency property.</summary>
    private static readonly DependencyPropertyKey ValueTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ValueText), typeof(string), typeof(NumericUpDown), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the read-only <see cref="ValueText"/> dependency property.</summary>
    public static readonly DependencyProperty ValueTextProperty = ValueTextPropertyKey.DependencyProperty;

    /// <summary>Identifies the <see cref="ValueChanged"/> routed event.</summary>
    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged), RoutingStrategy.Direct,
        typeof(System.Windows.RoutedPropertyChangedEventHandler<decimal>), typeof(NumericUpDown));

    private const string PartTextBox = "PART_TextBox";
    private const string PartUpButton = "PART_UpButton";
    private const string PartDownButton = "PART_DownButton";

    private TextBox? _textBox;

    static NumericUpDown()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericUpDown),
            new FrameworkPropertyMetadata(typeof(NumericUpDown)));
        // The wrapper itself is NOT a tab stop: the inner TextBox handles the caret, and a
        // focusable wrapper adds a dead Tab press before every spinner (acceptance fix;
        // AutoComplete uses the same pattern).
        KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(NumericUpDown),
            new FrameworkPropertyMetadata(false));
        FocusableProperty.OverrideMetadata(typeof(NumericUpDown),
            new FrameworkPropertyMetadata(false));
    }

    /// <summary>Occurs after <see cref="Value"/> changed.</summary>
    public event System.Windows.RoutedPropertyChangedEventHandler<decimal> ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    /// <summary>The current value (two-way bindable, always within bounds).</summary>
    public decimal Value
    {
        get => (decimal)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Minimum allowed value.</summary>
    public decimal MinValue
    {
        get => (decimal)GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }

    /// <summary>Maximum allowed value.</summary>
    public decimal MaxValue
    {
        get => (decimal)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    /// <summary>Step applied by buttons, arrows and the mouse wheel.</summary>
    public decimal Increment
    {
        get => (decimal)GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    /// <summary>Fixed decimal places; -1 derives the scale from <see cref="Increment"/>.</summary>
    public int DecimalPlaces
    {
        get => (int)GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, value);
    }

    /// <summary>Optional numeric format string, e.g. <c>N2</c> or <c>C</c>.</summary>
    public string? FormatString
    {
        get => (string?)GetValue(FormatStringProperty);
        set => SetValue(FormatStringProperty, value);
    }

    /// <summary>When <see langword="true"/>, the value cannot be edited.</summary>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>Formatted value shown in the text box.</summary>
    public string ValueText => (string)GetValue(ValueTextProperty);

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_textBox is not null)
        {
            _textBox.KeyDown -= OnTextBoxKeyDown;
            _textBox.LostFocus -= OnTextBoxLostFocus;
        }

        _textBox = GetTemplateChild(PartTextBox) as TextBox;

        // Name the spin buttons for assistive technology (their template content is a
        // glyph, which reads as an empty name to screen readers and UIA clients).
        if (GetTemplateChild("PART_UpButton") is System.Windows.Controls.Primitives.RepeatButton spinUp)
        {
            System.Windows.Automation.AutomationProperties.SetName(spinUp, Localization.GridStrings.NumericIncrease);
        }

        if (GetTemplateChild("PART_DownButton") is System.Windows.Controls.Primitives.RepeatButton spinDown)
        {
            System.Windows.Automation.AutomationProperties.SetName(spinDown, Localization.GridStrings.NumericDecrease);
        }

        if (_textBox is not null)
        {
            _textBox.KeyDown += OnTextBoxKeyDown;
            _textBox.LostFocus += OnTextBoxLostFocus;
        }

        if (GetTemplateChild(PartUpButton) is RepeatButton up)
        {
            up.Click += (_, _) => ApplyChange(Increment);
        }

        if (GetTemplateChild(PartDownButton) is RepeatButton down)
        {
            down.Click += (_, _) => ApplyChange(-Increment);
        }

        UpdateValueText();
    }

    /// <inheritdoc />
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (IsReadOnly)
        {
            return;
        }

        ApplyChange(e.Delta > 0 ? Increment : -Increment);
        e.Handled = true;
    }

    /// <summary>Applies a delta and clamps it; ignores no-op changes.</summary>
    internal void ApplyChange(decimal delta)
    {
        if (IsReadOnly)
        {
            return;
        }

        var candidate = NumericSpinnerLogic.Clamp(Value + delta, MinValue, MaxValue);
        if (candidate != Value)
        {
            Value = candidate;
        }
    }

    private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (IsReadOnly)
        {
            return;
        }

        var handled = true;
        switch (e.Key)
        {
            case Key.Up:
                ApplyChange(Increment);
                break;
            case Key.Down:
                ApplyChange(-Increment);
                break;
            case Key.PageUp:
                ApplyChange(Increment * 10);
                break;
            case Key.PageDown:
                ApplyChange(-Increment * 10);
                break;
            case Key.Enter:
                CommitText();
                break;
            default:
                handled = false;
                break;
        }

        e.Handled = handled;
    }

    private void OnTextBoxLostFocus(object sender, RoutedEventArgs e) => CommitText();

    /// <summary>Parses the text box content; invalid input reverts to the current value.</summary>
    private void CommitText()
    {
        if (_textBox is null)
        {
            return;
        }

        var parsed = NumericSpinnerLogic.Parse(_textBox.Text);
        if (parsed is { } value)
        {
            Value = NumericSpinnerLogic.Clamp(value, MinValue, MaxValue);
        }
        else
        {
            UpdateValueText();
        }
    }

    private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NumericUpDown)d;
        control.UpdateValueText();
        control.RaiseEvent(new RoutedPropertyChangedEventArgs<decimal>((decimal)e.OldValue, (decimal)e.NewValue)
        {
            RoutedEvent = ValueChangedEvent,
        });
    }

    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        var control = (NumericUpDown)d;
        return NumericSpinnerLogic.Clamp((decimal)baseValue, control.MinValue, control.MaxValue);
    }

    private static void OnBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NumericUpDown)d;
        control.Value = control.Value; // re-run coercion
    }

    private static void OnDisplayPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((NumericUpDown)d).UpdateValueText();

    private void UpdateValueText()
    {
        var places = NumericSpinnerLogic.DeriveDecimalPlaces(Increment, DecimalPlaces);
        SetValue(ValueTextPropertyKey, NumericSpinnerLogic.Format(Value, places, FormatString));
        if (_textBox is not null)
        {
            _textBox.Text = ValueText;
        }
    }
}
