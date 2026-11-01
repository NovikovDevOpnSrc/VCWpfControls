using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace VCWpfControls.Grid.Columns;

/// <summary>
/// Generates columns from an item type using reflection. Honors
/// <see cref="DisplayAttribute"/> (name, order), <see cref="ReadOnlyAttribute"/> and
/// <see cref="BrowsableAttribute"/>.
/// </summary>
public static class ColumnAutoGenerator
{
    /// <summary>Returns the columns generated for the given item type, in display order.</summary>
    public static IEnumerable<GridColumnBase> GenerateColumns(Type itemType)
    {
        ArgumentNullException.ThrowIfNull(itemType);

        var properties = itemType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead
                && !p.IsSpecialName
                && p.GetIndexParameters().Length == 0
                && IsBrowsable(p))
            .OrderBy(p => GetDisplayOrder(p) ?? int.MaxValue)
            .ThenBy(p => p.MetadataToken);

        foreach (var property in properties)
        {
            yield return CreateColumn(property);
        }
    }

    private static bool IsBrowsable(PropertyInfo property)
        => property.GetCustomAttribute<BrowsableAttribute>() is not { } attribute || attribute.Browsable;

    private static int? GetDisplayOrder(PropertyInfo property)
        => property.GetCustomAttribute<DisplayAttribute>() is { } display && display.GetOrder() is { } order
            ? order
            : null;

    private static GridColumnBase CreateColumn(PropertyInfo property)
    {
        var column = CreateTypedColumn(property.PropertyType);
        column.FieldName = property.Name;
        column.Header = ResolveHeader(property);
        column.ReadOnly = property.GetCustomAttribute<ReadOnlyAttribute>() is { } readOnly && readOnly.IsReadOnly;
        return column;
    }

    private static string ResolveHeader(PropertyInfo property)
    {
        if (property.GetCustomAttribute<DisplayAttribute>() is { } display)
        {
            var name = display.GetShortName() ?? display.GetName();
            if (!string.IsNullOrEmpty(name))
            {
                return name!;
            }
        }

        if (property.GetCustomAttribute<DisplayNameAttribute>() is { } displayName && !string.IsNullOrEmpty(displayName.DisplayName))
        {
            return displayName.DisplayName;
        }

        return property.Name;
    }

    private static GridColumnBase CreateTypedColumn(Type propertyType)
    {
        var type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (type == typeof(bool))
        {
            return new GridCheckBoxColumn();
        }

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly))
        {
            return new GridDateColumn();
        }

        if (type.IsEnum)
        {
            return new GridComboBoxColumn
            {
                ItemsSource = Enum.GetValues(type),
            };
        }

        if (IsNumeric(type))
        {
            return new GridNumericColumn();
        }

        return new GridTextColumn();
    }

    private static bool IsNumeric(Type type) => Type.GetTypeCode(type) is
        >= TypeCode.SByte and <= TypeCode.Decimal;
}
