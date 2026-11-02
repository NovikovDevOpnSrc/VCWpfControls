using System.Text;

namespace VCWpfControls.Editors;

/// <summary>The kind of an editable mask slot.</summary>
public enum MaskSlotKind
{
    /// <summary>Required digit.</summary>
    Digit = 0,

    /// <summary>Optional digit.</summary>
    DigitOptional = 1,

    /// <summary>Optional digit or sign.</summary>
    DigitOrSign = 2,

    /// <summary>Required letter.</summary>
    Letter = 3,

    /// <summary>Optional letter.</summary>
    LetterOptional = 4,

    /// <summary>Required letter or digit.</summary>
    LetterOrDigit = 5,

    /// <summary>Optional letter or digit.</summary>
    LetterOrDigitOptional = 6,

    /// <summary>Required printable character.</summary>
    Any = 7,

    /// <summary>Optional printable character.</summary>
    AnyOptional = 8,
}

/// <summary>
/// UI-free masked-input engine. Parses a mask into literal and editable slots and applies
/// character-level edits (insert, backspace, delete) while enforcing the slot rules.
/// Fully unit-testable without WPF.
/// </summary>
/// <remarks>
/// Mask language: <c>0</c> digit, <c>9</c> optional digit, <c>#</c> optional digit or sign,
/// <c>L</c> letter, <c>?</c> optional letter, <c>A</c> letter/digit, <c>a</c> optional
/// letter/digit, <c>C</c> any character, <c>c</c> optional any character. <c>\</c> escapes the
/// next character into a literal; every other character is a literal.
/// </remarks>
public sealed class InputMask
{
    private readonly List<(char Literal, MaskSlotKind? Slot)> _tokens;

    /// <summary>Creates the engine for the given mask.</summary>
    public InputMask(string mask)
    {
        ArgumentNullException.ThrowIfNull(mask);
        Mask = mask;
        _tokens = new List<(char, MaskSlotKind?)>();
        Length = 0;

        for (var i = 0; i < mask.Length; i++)
        {
            var ch = mask[i];
            if (ch == '\\' && i + 1 < mask.Length)
            {
                _tokens.Add((mask[++i], null));
            }
            else
            {
                var slot = ch switch
                {
                    '0' => MaskSlotKind.Digit,
                    '9' => MaskSlotKind.DigitOptional,
                    '#' => MaskSlotKind.DigitOrSign,
                    'L' => MaskSlotKind.Letter,
                    '?' => MaskSlotKind.LetterOptional,
                    'A' => MaskSlotKind.LetterOrDigit,
                    'a' => MaskSlotKind.LetterOrDigitOptional,
                    'C' => MaskSlotKind.Any,
                    'c' => MaskSlotKind.AnyOptional,
                    _ => (MaskSlotKind?)null,
                };
                _tokens.Add((ch, slot));
            }

            Length++;
        }

        _values = new char?[Length];
    }

    /// <summary>The original mask string.</summary>
    public string Mask { get; }

    /// <summary>Total number of characters in the display (literals + editable slots).</summary>
    public int Length { get; }

    /// <summary>Character shown for empty editable slots.</summary>
    public char PromptChar { get; set; } = '_';

    private readonly char?[] _values;

    /// <summary>Whether every required slot is filled.</summary>
    public bool IsComplete
    {
        get
        {
            for (var i = 0; i < Length; i++)
            {
                if (IsRequired(_tokens[i].Slot) && _values[i] is null)
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>Whether the given display position is an editable slot.</summary>
    public bool IsEditable(int position)
        => position >= 0 && position < Length && _tokens[position].Slot is not null;

    /// <summary>Returns the display text: literals, filled values and prompts.</summary>
    public string ToDisplayString()
    {
        var builder = new StringBuilder(Length);
        for (var i = 0; i < Length; i++)
        {
            if (_tokens[i].Slot is null)
            {
                builder.Append(_tokens[i].Literal);
            }
            else
            {
                builder.Append(_values[i] ?? PromptChar);
            }
        }

        return builder.ToString();
    }

    /// <summary>Returns the value: filled editable characters only, by default.</summary>
    public string ToValueString(bool includeLiterals = false)
    {
        var builder = new StringBuilder(Length);
        for (var i = 0; i < Length; i++)
        {
            var isLiteral = _tokens[i].Slot is null;
            if (isLiteral && !includeLiterals)
            {
                continue;
            }

            if (isLiteral)
            {
                builder.Append(_tokens[i].Literal);
            }
            else if (_values[i] is { } value)
            {
                builder.Append(value);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Tries to type <paramref name="input"/> at <paramref name="position"/>; returns the new
    /// caret position, or <c>-1</c> when the character is rejected.
    /// </summary>
    public int Insert(string input, int position)
    {
        ArgumentNullException.ThrowIfNull(input);
        var caret = position;
        foreach (var ch in input)
        {
            caret = InsertChar(ch, caret);
            if (caret < 0)
            {
                return -1;
            }
        }

        return caret;
    }

    /// <summary>
    /// Types one character into the first editable slot at/after the caret. The character is
    /// rejected when that slot's rule does not accept it (editable slots are never skipped).
    /// Returns the new caret position (after the filled slot), or <c>-1</c> on rejection.
    /// </summary>
    public int InsertChar(char ch, int position)
    {
        if (char.IsControl(ch))
        {
            return -1;
        }

        for (var i = Math.Max(0, position); i < Length; i++)
        {
            if (_tokens[i].Slot is not { } kind)
            {
                continue; // literal: move to the next slot
            }

            if (!Accepts(kind, ch))
            {
                return -1;
            }

            _values[i] = ch;
            return NextEditableAfter(i + 1);
        }

        return -1;
    }

    /// <summary>Clears the closest editable slot before the caret; returns the new caret.</summary>
    public int Backspace(int position)
    {
        for (var i = Math.Min(position, Length - 1); i >= 0; i--)
        {
            if (_tokens[i].Slot is not null)
            {
                _values[i] = null;
                return i;
            }
        }

        return Math.Max(0, Math.Min(position, Length));
    }

    /// <summary>Clears the editable slot at the caret (or the next one); caret unchanged.</summary>
    public int Delete(int position)
    {
        for (var i = Math.Max(0, position); i < Length; i++)
        {
            if (_tokens[i].Slot is not null)
            {
                _values[i] = null;
                return position;
            }
        }

        return position;
    }

    /// <summary>Replaces all editable values from a raw string (no mask validation).</summary>
    public void SetValue(string? value)
    {
        for (var i = 0; i < Length; i++)
        {
            _values[i] = null;
        }

        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        var slot = 0;
        foreach (var ch in value)
        {
            while (slot < Length && _tokens[slot].Slot is null)
            {
                slot++;
            }

            if (slot >= Length)
            {
                break;
            }

            _values[slot++] = ch;
        }
    }

    /// <summary>First editable slot at or after the position, or the length when none.</summary>
    public int NextEditableAfter(int position)
    {
        for (var i = Math.Max(0, position); i < Length; i++)
        {
            if (_tokens[i].Slot is not null)
            {
                return i;
            }
        }

        return Length;
    }

    private static bool IsRequired(MaskSlotKind? kind)
        => kind is MaskSlotKind.Digit or MaskSlotKind.Letter or MaskSlotKind.LetterOrDigit or MaskSlotKind.Any;

    private static bool Accepts(MaskSlotKind kind, char ch)
        => kind switch
        {
            MaskSlotKind.Digit => char.IsDigit(ch),
            MaskSlotKind.DigitOptional => char.IsDigit(ch) || ch == ' ',
            MaskSlotKind.DigitOrSign => char.IsDigit(ch) || ch == '+' || ch == '-' || ch == ' ',
            MaskSlotKind.Letter => char.IsLetter(ch),
            MaskSlotKind.LetterOptional => char.IsLetter(ch) || ch == ' ',
            MaskSlotKind.LetterOrDigit => char.IsLetterOrDigit(ch),
            MaskSlotKind.LetterOrDigitOptional => char.IsLetterOrDigit(ch) || ch == ' ',
            MaskSlotKind.Any => !char.IsControl(ch),
            MaskSlotKind.AnyOptional => !char.IsControl(ch),
            _ => false,
        };
}
