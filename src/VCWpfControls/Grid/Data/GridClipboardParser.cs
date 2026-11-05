using System.Globalization;

namespace VCWpfControls.Grid.Data;

/// <summary>
/// Parsing and conversion helpers for clipboard exchange (UI-free, unit-testable):
/// TSV cells in / out, and culture-aware string-to-property-type conversion for pasting.
/// </summary>
public static class GridClipboard
{
    /// <summary>Splits clipboard text into TSV rows and cells (Excel format). Empty lines are dropped.</summary>
    public static List<string[]> ParseTsv(string text)
    {
        var rows = new List<string[]>();
        if (string.IsNullOrEmpty(text))
        {
            return rows;
        }

        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        if (normalized.EndsWith('\n'))
        {
            normalized = normalized[..^1];
        }

        foreach (var line in normalized.Split('\n'))
        {
            if (line.Length == 0)
            {
                continue;
            }

            rows.Add(line.Split('\t'));
        }

        return rows;
    }

    /// <summary>
    /// Converts a clipboard string to the target property type (culture-aware).
    /// Empty text becomes <see langword="null"/> for nullable types. Returns
    /// <see langword="false"/> when the text cannot be represented.
    /// </summary>
    public static bool TryConvert(string? text, Type targetType, out object? value)
    {
        var type = targetType;
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
        {
            type = underlying;
        }

        if (string.IsNullOrEmpty(text))
        {
            value = (!targetType.IsValueType || underlying is not null) ? null : Activator.CreateInstance(type);
            return true;
        }

        try
        {
            if (type == typeof(string))
            {
                value = text;
                return true;
            }

            if (type.IsEnum)
            {
                value = Enum.Parse(type, text, ignoreCase: true);
                return true;
            }

            if (type == typeof(DateTime))
            {
                value = DateTime.Parse(text, CultureInfo.CurrentCulture);
                return true;
            }

            if (type == typeof(TimeSpan))
            {
                value = TimeSpan.Parse(text, CultureInfo.CurrentCulture);
                return true;
            }

            if (type == typeof(Guid))
            {
                value = Guid.Parse(text);
                return true;
            }

            value = Convert.ChangeType(text, type, CultureInfo.CurrentCulture);
            return true;
        }
        catch (FormatException)
        {
            value = null;
            return false;
        }
        catch (InvalidCastException)
        {
            value = null;
            return false;
        }
        catch (OverflowException)
        {
            value = null;
            return false;
        }
        catch (ArgumentException)
        {
            value = null;
            return false;
        }
    }
}
