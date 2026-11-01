using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace VCWpfControls.Grid.Data;

/// <summary>
/// A cached, compiled accessor for a property path such as <c>"Name"</c> or <c>"Address.City"</c>.
/// Accessors are cached per (owner type, path) pair and are thread-safe.
/// </summary>
public sealed class PropertyAccessor
{
    private static readonly ConcurrentDictionary<KeyValuePair<Type, string>, PropertyAccessor> Cache = new();

    private readonly Func<object, object?>? _getter;

    private PropertyAccessor(Type ownerType, string path)
    {
        OwnerType = ownerType;
        Path = path;
        Segments = path.Split('.');

        // Resolve the whole chain; the accessor is valid only when every segment resolved.
        Type? type = ownerType;
        PropertyInfo? last = null;
        foreach (var segment in Segments)
        {
            if (type is null)
            {
                last = null;
                break;
            }

            last = GetProperty(type, segment);
            if (last is null)
            {
                break;
            }

            type = last.PropertyType;
        }

        IsValid = last is not null;
        ValueType = last?.PropertyType ?? typeof(object);
        _getter = last is null ? null : BuildGetter(ownerType, Segments);
    }

    /// <summary>The type the path was resolved against.</summary>
    public Type OwnerType { get; }

    /// <summary>The dotted property path.</summary>
    public string Path { get; }

    /// <summary>Individual path segments.</summary>
    public IReadOnlyList<string> Segments { get; }

    /// <summary><c>true</c> when every segment of the path resolved to a public property.</summary>
    public bool IsValid { get; }

    /// <summary>The static type of the final property, or <see cref="object"/> when the path is invalid.</summary>
    public Type ValueType { get; }

    /// <summary>
    /// Gets or creates an accessor for the given path on the given owner type.
    /// Paths that cannot be resolved yield an invalid accessor that returns <see langword="null"/>.
    /// </summary>
    public static PropertyAccessor GetOrCreate(Type ownerType, string path)
    {
        ArgumentNullException.ThrowIfNull(ownerType);
        ArgumentNullException.ThrowIfNull(path);
        return Cache.GetOrAdd(new KeyValuePair<Type, string>(ownerType, path), key => new PropertyAccessor(key.Key, key.Value));
    }

    /// <summary>
    /// Reads the value. Returns <see langword="null"/> when any intermediate segment is <see langword="null"/>,
    /// when the path is invalid, or when <paramref name="target"/> is <see langword="null"/>.
    /// </summary>
    public object? GetValue(object? target)
    {
        if (target is null || _getter is null)
        {
            return null;
        }

        try
        {
            return _getter(target);
        }
        catch (TargetInvocationException)
        {
            // A property getter threw; treat the value as unavailable.
            return null;
        }
    }

    private static Func<object, object?>? BuildGetter(Type ownerType, IReadOnlyList<string> segments)
    {
        // Builds: object item => { object cur = item; if (cur == null) return null;
        //   cur = ((T0)cur).P0; if (cur == null) return null; ...; return cur; }
        var parameter = Expression.Parameter(typeof(object), "item");
        var current = Expression.Variable(typeof(object), "cur");
        var returnLabel = Expression.Label(typeof(object));

        var statements = new List<Expression>
        {
            Expression.Assign(current, parameter),
            Expression.IfThen(
                Expression.Equal(current, Expression.Constant(null, typeof(object))),
                Expression.Return(returnLabel, Expression.Constant(null, typeof(object)))),
        };

        Type? type = ownerType;
        foreach (var segment in segments)
        {
            var pi = type is null ? null : GetProperty(type, segment);
            if (pi is null)
            {
                return null;
            }

            statements.Add(Expression.Assign(
                current,
                Expression.Convert(
                    Expression.Property(Expression.Convert(current, type!), pi),
                    typeof(object))));

            if (!pi.PropertyType.IsValueType)
            {
                statements.Add(Expression.IfThen(
                    Expression.Equal(current, Expression.Constant(null, typeof(object))),
                    Expression.Return(returnLabel, Expression.Constant(null, typeof(object)))));
            }

            type = pi.PropertyType;
        }

        statements.Add(Expression.Label(returnLabel, current));

        var body = Expression.Block(new[] { current }, statements);
        return Expression.Lambda<Func<object, object?>>(body, parameter).Compile();
    }

    private static PropertyInfo? GetProperty(Type type, string name)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        return type.GetProperty(name, flags)
            ?? type.GetProperty(name, flags | BindingFlags.IgnoreCase);
    }
}
