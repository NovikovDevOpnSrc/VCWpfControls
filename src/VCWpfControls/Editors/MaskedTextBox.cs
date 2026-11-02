using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VCWpfControls.Editors;

/// <summary>
/// A text box with an input mask (see <see cref="InputMask"/> for the mask language).
/// Literals are displayed automatically, typing skips to the next matching slot,
/// backspace/delete clear editable slots. <see cref="Value"/> contains the typed characters
/// (optionally with literals via <see cref="IncludeLiterals"/>).
/// </summary>
public class MaskedTextBox : TextBox
{
    /// <summary>Identifies the <see cref="Mask"/> dependency property.</summary>
    public static readonly DependencyProperty MaskProperty = DependencyProperty.Register(
        nameof(Mask), typeof(string), typeof(MaskedTextBox),
        new PropertyMetadata(string.Empty, OnMaskChanged));

    /// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(MaskedTextBox),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    /// <summary>Identifies the <see cref="IncludeLiterals"/> dependency property.</summary>
    public static readonly DependencyProperty IncludeLiteralsProperty = DependencyProperty.Register(
        nameof(IncludeLiterals), typeof(bool), typeof(MaskedTextBox),
        new PropertyMetadata(false, OnIncludeLiteralsChanged));

    /// <summary>Identifies the <see cref="PromptChar"/> dependency property.</summary>
    public static readonly DependencyProperty PromptCharProperty = DependencyProperty.Register(
        nameof(PromptChar), typeof(char), typeof(MaskedTextBox),
        new PropertyMetadata('_', OnPromptChanged));

    private InputMask? _mask;
    private bool _updatingText;

    static MaskedTextBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MaskedTextBox),
            new FrameworkPropertyMetadata(typeof(MaskedTextBox)));
    }

    /// <summary>Creates the masked text box.</summary>
    public MaskedTextBox()
    {
        DataObject.AddPastingHandler(this, OnPaste);
    }

    /// <summary>The input mask, e.g. <c>"+7 (000) 000-00-00"</c>.</summary>
    public string Mask
    {
        get => (string)GetValue(MaskProperty);
        set => SetValue(MaskProperty, value);
    }

    /// <summary>The typed characters (mask-validated), with or without literals.</summary>
    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Whether <see cref="Value"/> includes literal characters.</summary>
    public bool IncludeLiterals
    {
        get => (bool)GetValue(IncludeLiteralsProperty);
        set => SetValue(IncludeLiteralsProperty, value);
    }

    /// <summary>Character shown for empty editable slots.</summary>
    public char PromptChar
    {
        get => (char)GetValue(PromptCharProperty);
        set => SetValue(PromptCharProperty, value);
    }

    /// <summary>Whether all required slots are filled.</summary>
    public bool IsMaskComplete => _mask?.IsComplete ?? false;

    private static void OnMaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (MaskedTextBox)d;
        control._mask = string.IsNullOrEmpty(control.Mask) ? null : new InputMask(control.Mask) { PromptChar = control.PromptChar };
        control.PushValueToText(control.Value);
        control.UpdateCaret();
    }

    private static void OnPromptChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (MaskedTextBox)d;
        if (control._mask is not null)
        {
            control._mask.PromptChar = control.PromptChar;
            control.PushValueToText(control.Value);
        }
    }

    private static void OnIncludeLiteralsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (MaskedTextBox)d;
        control.SyncValueFromMask();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (MaskedTextBox)d;
        control.PushValueToText((string)e.NewValue);
    }

    /// <inheritdoc />
    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        if (_mask is null || IsReadOnly)
        {
            base.OnPreviewTextInput(e);
            return;
        }

        var newCaret = _mask.Insert(e.Text, CaretIndex);
        if (newCaret >= 0)
        {
            PushMaskToText(newCaret);
        }

        e.Handled = true;
        base.OnPreviewTextInput(e);
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (_mask is null || IsReadOnly)
        {
            base.OnPreviewKeyDown(e);
            return;
        }

        switch (e.Key)
        {
            case Key.Back:
                PushMaskToText(_mask.Backspace(CaretIndex));
                e.Handled = true;
                break;

            case Key.Delete:
                PushMaskToText(_mask.Delete(CaretIndex));
                e.Handled = true;
                break;

            case Key.Left:
            case Key.Right:
            case Key.Home:
            case Key.End:
            case Key.Tab:
                // Navigation is handled by the TextBox itself.
                break;

            case Key.Space:
                if (_mask.InsertChar(' ', CaretIndex) is var caret && caret >= 0)
                {
                    PushMaskToText(caret);
                }

                e.Handled = true;
                break;

            default:
                if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    // Paste handled through the pasting event.
                    break;
                }

                if (Keyboard.Modifiers != ModifierKeys.None && Keyboard.Modifiers != ModifierKeys.Shift)
                {
                    break;
                }

                e.Handled = false;
                break;
        }

        base.OnPreviewKeyDown(e);
    }

    private void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (_mask is not null && !IsReadOnly && e.DataObject.GetData(DataFormats.Text) is string text)
        {
            var newCaret = _mask.Insert(text, CaretIndex);
            if (newCaret >= 0)
            {
                PushMaskToText(newCaret);
            }

            e.CancelCommand();
            return;
        }

        // No base call: OnPaste is an attached handler, not a virtual method.
    }

    /// <summary>Writes the engine state into the text box and the <see cref="Value"/> property.</summary>
    private void PushMaskToText(int caret)
    {
        PushValueToText(null);
        UpdateCaret(caret);
        SyncValueFromMask();
    }

    private void PushValueToText(string? value)
    {
        if (_mask is null)
        {
            return;
        }

        if (value is not null)
        {
            _mask.SetValue(value);
        }

        _updatingText = true;
        try
        {
            Text = _mask.ToDisplayString();
        }
        finally
        {
            _updatingText = false;
        }
    }

    private void SyncValueFromMask()
    {
        if (_mask is not null)
        {
            SetValue(ValueProperty, _mask.ToValueString(IncludeLiterals));
        }
    }

    private void UpdateCaret(int? caret = null)
    {
        if (_mask is null)
        {
            return;
        }

        var position = caret ?? CaretIndex;
        if (_mask.IsEditable(position) || position >= _mask.Length)
        {
            CaretIndex = Math.Min(position, _mask.Length);
            return;
        }

        CaretIndex = _mask.NextEditableAfter(position);
    }

    /// <inheritdoc />
    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        if (!_updatingText && _mask is not null)
        {
            // External text change (binding, programmatic Text set): re-apply the mask.
            _updatingText = true;
            try
            {
                _mask.SetValue(Text);
                Text = _mask.ToDisplayString();
            }
            finally
            {
                _updatingText = false;
            }

            SyncValueFromMask();
        }

        base.OnTextChanged(e);
    }
}
