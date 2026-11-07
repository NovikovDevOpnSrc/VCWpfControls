using System.Reflection;
using VCWpfControls.Grid.Data;

namespace VCWpfControls.PropertyGrid;

/// <summary>Editor control kind inferred from the property type.</summary>
public enum PropertyItemEditorKind
{
    /// <summary>Single-line text box.</summary>
    Text = 0,

    /// <summary>Multi-line text box.</summary>
    MultiLineText = 1,

    /// <summary>Check box.</summary>
    Boolean = 2,

    /// <summary>Combo box with the enum members.</summary>
    Enum = 3,

    /// <summary>Right-aligned numeric text box.</summary>
    Numeric = 4,

    /// <summary>Date picker.</summary>
    Date = 5,

    /// <summary>Read-only text (unsupported/collection types).</summary>
    ReadOnly = 6,
}

/// <summary>When a property edit is pushed to the object.</summary>
public enum PropertyGridCommitMode
{
    /// <summary>On Enter or when the editor loses focus (the default).</summary>
    OnCommit = 0,

    /// <summary>On every keystroke / toggle (no pending state).</summary>
    Immediate = 1,
}

/// <summary>
/// One editable property of <see cref="PropertyGrid.SelectedObject"/>: metadata resolved through
/// reflection plus get/set access to the value. UI-free.
/// </summary>
public sealed class PropertyItem
{
    /// <summary>Creates the item.</summary>
    public PropertyItem(object owner, PropertyInfo property)
    {
        Owner = owner;
        Property = property;
        Name = property.Name;
        PropertyType = property.PropertyType;
        IsReadOnly = !property.CanWrite || PropertyItemBuilder.IsMarkedReadOnly(property);
        EditorKind = PropertyItemBuilder.ResolveEditorKind(PropertyType, property);
    }

    /// <summary>The object the property belongs to.</summary>
    public object Owner { get; }

    /// <summary>The reflected property.</summary>
    public PropertyInfo Property { get; }

    /// <summary>Property name.</summary>
    public string Name { get; }

    /// <summary>Display name (from attributes) or the property name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Category (from attributes) or <see langword="null"/>.</summary>
    public string? Category { get; set; }

    /// <summary>Description tooltip text.</summary>
    public string? Description { get; set; }

    /// <summary>Sort order from attributes; <see cref="int.MaxValue"/> when unspecified.</summary>
    public int SortOrder { get; set; } = int.MaxValue;

    /// <summary>Static property type.</summary>
    public Type PropertyType { get; }

    /// <summary>Whether the value can be edited.</summary>
    public bool IsReadOnly { get; }

    /// <summary>The editor kind for this property.</summary>
    public PropertyItemEditorKind EditorKind { get; }

    /// <summary>Whether the property is a complex object expandable into nested items.</summary>
    public bool IsExpandable => !IsReadOnly
        && PropertyItemBuilder.IsExpandableType(PropertyType);

    /// <summary>Nested items of an expandable property (built lazily).</summary>
    public List<PropertyItem>? Children { get; set; }

    /// <summary>Reads the current value.</summary>
    public object? GetValue() => Property.GetValue(Owner);

    /// <summary>Writes the value; throws on setter failure.</summary>
    public void SetValue(object? value) => Property.SetValue(Owner, value);

    /// <summary>The type converter of the property type (base converter when none registered).</summary>
    public System.ComponentModel.TypeConverter Converter
        => _converter ??= System.ComponentModel.TypeDescriptor.GetConverter(PropertyType);

    private System.ComponentModel.TypeConverter? _converter;

    /// <summary>
    /// Whether the property type has a REAL TypeConverter registered (not the do-nothing
    /// base) that can convert from a string — the editor can accept typed text then.
    /// </summary>
    public bool HasStringConverter
        => Converter.GetType() != typeof(System.ComponentModel.TypeConverter)
        && Converter.CanConvertFrom(typeof(string));

    /// <summary>Reads the current value as display text.</summary>
    public string GetValueText()
    {
        var value = GetValue();
        if (value is null)
        {
            return string.Empty;
        }

        if (value is System.Collections.IEnumerable enumerable && value is not string)
        {
            var count = 0;
            foreach (var _ in enumerable)
            {
                count++;
            }

            return $"{value.GetType().Name} ({count} items)";
        }

        // A registered converter wins (Guid, CultureInfo, custom structs...); the base
        // converter would just call ToString, so it is not treated as one.
        if (HasStringConverter && Converter.CanConvertTo(typeof(string)))
        {
            try
            {
                return Converter.ConvertToString(null, System.Globalization.CultureInfo.CurrentCulture, value) ?? string.Empty;
            }
            catch (Exception exception) when (
                exception is FormatException or InvalidCastException or OverflowException or ArgumentException
                or System.ComponentModel.DataAnnotations.ValidationException)
            {
                // fall through to the default formatting
            }
        }

        return Convert.ToString(value, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;
    }
}
