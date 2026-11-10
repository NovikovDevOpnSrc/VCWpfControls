using VCWpfControls.Grid.Data;
using VCWpfControls.Localization;

namespace VCWpfControls.Conditions;

/// <summary>How the children of a <see cref="ConditionGroup"/> combine.</summary>
public enum ConditionGroupOperator
{
    /// <summary>All children must match.</summary>
    And = 0,

    /// <summary>At least one child must match.</summary>
    Or = 1,
}

/// <summary>One node of a condition tree: a group or a single expression.</summary>
public abstract class ConditionNode
{
    /// <summary>Whether the node is vacuously true/false (no children in a group).</summary>
    public abstract bool IsEmpty { get; }
}

/// <summary>A logical group of nested conditions combined with And/Or.</summary>
public sealed class ConditionGroup : ConditionNode
{
    /// <summary>Creates a group.</summary>
    public ConditionGroup(ConditionGroupOperator operatorKind = ConditionGroupOperator.And)
    {
        OperatorKind = operatorKind;
    }

    /// <summary>How the children combine.</summary>
    public ConditionGroupOperator OperatorKind { get; set; }

    /// <summary>The nested conditions (expressions and further groups).</summary>
    public List<ConditionNode> Children { get; } = new();

    /// <inheritdoc />
    public override bool IsEmpty => Children.Count == 0;
}

/// <summary>
/// One field condition: field + operator + value(s). Operators and their semantics are the
/// grid engine's <see cref="GridFilterOperator"/> — evaluation delegates to
/// <see cref="GridFilterDescription.Matches(object, PropertyAccessor)"/> so the condition
/// tree and the grid view can never disagree.
/// </summary>
public sealed class ConditionExpression : ConditionNode
{
    /// <summary>Creates the expression.</summary>
    public ConditionExpression(string fieldName, GridFilterOperator operatorKind, object? value = null, object? value2 = null)
    {
        _fieldName = fieldName;
        OperatorKind = operatorKind;
        Value = value;
        Value2 = value2;
    }

    /// <summary>The filtered field name.</summary>
    public string FieldName
    {
        get => _fieldName;
        set
        {
            _fieldName = value;
            _accessorFieldName = null; // invalidate the per-type accessor cache
        }
    }

    private string _fieldName;

    /// <summary>
    /// Per-item-type accessor cache: dictionary lookups per evaluation dominated the
    /// condition hot path (see benchmarks) — a ConditionalWeakTable read is free.
    /// Weak: collected item types take their cache entries with them.
    /// </summary>
    internal System.Runtime.CompilerServices.ConditionalWeakTable<Type, Grid.Data.PropertyAccessor>? AccessorCache;

    internal string? _accessorFieldName;

    /// <summary>The operator.</summary>
    public GridFilterOperator OperatorKind { get; set; }

    /// <summary>The value (a list for <see cref="GridFilterOperator.In"/>; the lower bound for Between).</summary>
    public object? Value { get; set; }

    /// <summary>The second value (the upper bound for Between/NotBetween).</summary>
    public object? Value2 { get; set; }

    /// <inheritdoc />
    public override bool IsEmpty => false;
}

/// <summary>Metadata of one filterable field for the condition editor UI.</summary>
public sealed class ConditionField
{
    /// <summary>Creates the field metadata.</summary>
    public ConditionField(string name, Type valueType, string? displayName = null)
    {
        Name = name;
        ValueType = valueType;
        DisplayName = displayName ?? name;
    }

    /// <summary>The field name used in expressions.</summary>
    public string Name { get; }

    /// <summary>The static value type (drives operators and the value editor).</summary>
    public Type ValueType { get; }

    /// <summary>The display name.</summary>
    public string DisplayName { get; }

    /// <summary>Optional suggested values (enum members, reference lists).</summary>
    public System.Collections.IEnumerable? Suggestions { get; set; }
}

/// <summary>The set of fields a condition tree filters over.</summary>
public sealed class ConditionSchema
{
    /// <summary>Creates an empty schema.</summary>
    public ConditionSchema()
    {
    }

    /// <summary>Creates a schema with the given fields.</summary>
    public ConditionSchema(IEnumerable<ConditionField> fields)
    {
        Fields.AddRange(fields);
    }

    /// <summary>The filterable fields.</summary>
    public List<ConditionField> Fields { get; } = new();

    /// <summary>The field with the given name (case-insensitive), or null.</summary>
    public ConditionField? Find(string name)
        => Fields.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Builds a schema from the public readable instance properties of a type:
    /// booleans, enums, dates, numbers and strings are included; display names honor
    /// <c>[Display(Name = ...)]</c>.
    /// </summary>
    public static ConditionSchema FromType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var fields = new List<ConditionField>();
        foreach (var property in type.GetProperties())
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var valueType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var supported = valueType == typeof(string)
                || valueType == typeof(bool)
                || valueType == typeof(DateTime)
                || valueType.IsEnum
                || Type.GetTypeCode(valueType) is >= TypeCode.SByte and <= TypeCode.Decimal;
            if (!supported)
            {
                continue;
            }

            var display = property.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false)
                .OfType<System.ComponentModel.DataAnnotations.DisplayAttribute>()
                .FirstOrDefault()?.GetName();
            var field = new ConditionField(property.Name, property.PropertyType, display);
            if (valueType.IsEnum)
            {
                field.Suggestions = Enum.GetValues(valueType);
            }

            fields.Add(field);
        }

        return new ConditionSchema(fields);
    }
}
