using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace VCWpfControls.PropertyGrid;

/// <summary>
/// Builds <see cref="PropertyItem"/> lists from an object's type using reflection. Honors
/// <see cref="DisplayAttribute"/> (name/description/order/group), <see cref="CategoryAttribute"/>,
/// <see cref="DisplayNameAttribute"/>, <see cref="ReadOnlyAttribute"/>, <see cref="BrowsableAttribute"/>
/// and <see cref="DataTypeAttribute"/> (multiline text). UI-free and unit-testable.
/// </summary>
public static class PropertyItemBuilder
{
    /// <summary>Returns the property items of the object, ordered by category then display order.</summary>
    public static List<PropertyItem> Build(object target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return Build(target, target.GetType());
    }

    /// <summary>Returns the property items of the object, optionally declared-only.</summary>
    public static List<PropertyItem> Build(object target, bool declaredPropertiesOnly)
    {
        ArgumentNullException.ThrowIfNull(target);
        return Build(target, target.GetType(), declaredPropertiesOnly);
    }

    /// <summary>Returns the property items for the given type instantiated on the target.</summary>
    public static List<PropertyItem> Build(object target, Type type)
        => Build(target, type, declaredPropertiesOnly: false);

    /// <summary>Returns the property items for the given type instantiated on the target;
    /// with <paramref name="declaredPropertiesOnly"/> only the type's OWN declarations are
    /// listed (bind to a control and edit its API without the inherited noise).</summary>
    public static List<PropertyItem> Build(object target, Type type, bool declaredPropertiesOnly)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(type);

        var result = new List<PropertyItem>();
        var properties = declaredPropertiesOnly
            ? GetPropertiesDeclaredInOwnAssembly(type)
            : type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            if (!property.CanRead
                || property.IsSpecialName
                || property.GetIndexParameters().Length > 0
                || !IsBrowsable(property))
            {
                continue;
            }

            var item = new PropertyItem(target, property)
            {
                DisplayName = ResolveDisplayName(property),
                Category = ResolveCategory(property),
                Description = ResolveDescription(property),
                SortOrder = ResolveOrder(property),
            };
            result.Add(item);
        }

        return result
            .OrderBy(i => i.Category, StringComparer.CurrentCulture)
            .ThenBy(i => i.SortOrder)
            .ThenBy(i => i.DisplayName, StringComparer.CurrentCulture)
            .ToList();
    }

    /// <summary>
    /// The hierarchy walk behind <c>declaredPropertiesOnly</c>: every declaration from the
    /// concrete type up while the types live in the SAME assembly — editing a library
    /// control lists its whole API (TreeListView includes GridControl's dependency
    /// properties) while the inherited platform surface (ItemsControl, Control) stays out.
    /// </summary>
    private static IEnumerable<PropertyInfo> GetPropertiesDeclaredInOwnAssembly(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var current = type; current is not null && current != typeof(object) && current.Assembly == type.Assembly; current = current.BaseType)
        {
            foreach (var property in current.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (seen.Add(property.Name))
                {
                    yield return property; // the most-derived declaration wins
                }
            }
        }
    }

    /// <summary>Builds the nested items of an expandable property value.</summary>
    public static List<PropertyItem>? BuildChildren(PropertyItem item)
    {
        if (!item.IsExpandable)
        {
            return null;
        }

        var value = item.GetValue();
        if (value is null)
        {
            return new List<PropertyItem>();
        }

        return Build(value, value.GetType());
    }

    /// <summary>Whether the type is a complex object that can be expanded into nested properties.</summary>
    public static bool IsExpandableType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(string) || type == typeof(object) || type.IsEnum || type.IsArray || type.IsPrimitive)
        {
            return false;
        }

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly)
            || type == typeof(Guid) || type == typeof(TimeSpan))
        {
            return false;
        }

        // Remaining non-primitive value types and classes are expandable.
        return Type.GetTypeCode(type) is not (>= TypeCode.SByte and <= TypeCode.Decimal);
    }

    /// <summary>Maps a property type to the editor kind.</summary>
    public static PropertyItemEditorKind ResolveEditorKind(Type type, PropertyInfo property)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(bool))
        {
            return PropertyItemEditorKind.Boolean;
        }

        if (underlying.IsEnum)
        {
            return PropertyItemEditorKind.Enum;
        }

        if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset) || underlying == typeof(DateOnly))
        {
            return PropertyItemEditorKind.Date;
        }

        if (Type.GetTypeCode(underlying) is >= TypeCode.SByte and <= TypeCode.Decimal)
        {
            return PropertyItemEditorKind.Numeric;
        }

        if (underlying == typeof(string))
        {
            if (property.GetCustomAttribute<DataTypeAttribute>() is { } dataType
                && dataType.DataType == DataType.MultilineText)
            {
                return PropertyItemEditorKind.MultiLineText;
            }

            return PropertyItemEditorKind.Text;
        }

        // Types with a registered string converter (Guid, CultureInfo, custom structs...)
        // are editable as text: the converter parses the input back. NOTE: compute the
        // converter HERE - constructing a PropertyItem would re-enter this method
        // (its ctor calls ResolveEditorKind) and blow the stack.
        var converter = System.ComponentModel.TypeDescriptor.GetConverter(underlying);
        if (converter.GetType() != typeof(System.ComponentModel.TypeConverter)
            && converter.CanConvertFrom(typeof(string)))
        {
            return PropertyItemEditorKind.Text;
        }

        return PropertyItemEditorKind.ReadOnly;
    }

    /// <summary>Whether the property is marked <see cref="ReadOnlyAttribute"/>.</summary>
    public static bool IsMarkedReadOnly(PropertyInfo property)
        => property.GetCustomAttribute<ReadOnlyAttribute>() is { } readOnly && readOnly.IsReadOnly;

    private static bool IsBrowsable(PropertyInfo property)
        => property.GetCustomAttribute<BrowsableAttribute>() is not { } attribute || attribute.Browsable;

    private static string ResolveDisplayName(PropertyInfo property)
    {
        if (property.GetCustomAttribute<DisplayAttribute>() is { } display)
        {
            var name = display.GetName();
            if (!string.IsNullOrEmpty(name))
            {
                return name!;
            }
        }

        if (property.GetCustomAttribute<DisplayNameAttribute>() is { } displayName
            && !string.IsNullOrEmpty(displayName.DisplayName))
        {
            return displayName.DisplayName;
        }

        return property.Name;
    }

    private static string? ResolveCategory(PropertyInfo property)
    {
        if (property.GetCustomAttribute<DisplayAttribute>() is { } display
            && display.GetGroupName() is { } group)
        {
            return group;
        }

        return property.GetCustomAttribute<CategoryAttribute>()?.Category;
    }

    private static string? ResolveDescription(PropertyInfo property)
        => property.GetCustomAttribute<DisplayAttribute>()?.GetDescription()
            ?? property.GetCustomAttribute<DescriptionAttribute>()?.Description;

    private static int ResolveOrder(PropertyInfo property)
        => property.GetCustomAttribute<DisplayAttribute>() is { } display && display.GetOrder() is { } order
            ? order
            : int.MaxValue;
}
