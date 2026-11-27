using System.Reflection;
using Xunit;

namespace VCWpfControls.Core.Tests;

/// <summary>
/// Executable architecture rules (the deslop-please idea, C# edition): the layering that
/// AGENTS.md states in prose is enforced here by reflection, so a future "just one small
/// WPF type" commit in the engine fails CI instead of silently eroding the boundary.
///
/// Rules:
///  1. The data engine (`VCWpfControls.Grid.Data`) knows NO WPF — no System.Windows type
///     may appear in its bases, interfaces, members, parameters or attributes. This is
///     what lets the engine run (and stay testable) without STA.
///  2. The engine is self-contained otherwise: only itself and the string resources
///     (`VCWpfControls.Localization`) — no UI namespaces of the library.
///  3. `Localization` is WPF-free too (pure resx), so rule 1's allowlist stays sound.
///
/// Known sanctioned edges NOT covered here (documented, not enforced):
///  - `Grid.Primitives.GridCellsPanel` type-checks `TreeList.TreeListView` (indent seam);
///  - `GridControl.HeaderMenu` composes `Conditions.*` (the filter dialog) — and
///    Conditions reads Grid resource tokens, a namespace-level cycle to retire someday.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly Library = typeof(VCWpfControls.Grid.Data.GridDataManager).Assembly;

    private static IEnumerable<Type> TypesUnder(string ns)
        => Library.GetTypes().Where(t => t.Namespace?.StartsWith(ns, StringComparison.Ordinal) == true);

    /// <summary>Every type reachable from the given type's shape: bases, interfaces,
    /// fields, properties, methods, events, attributes — generics and arrays unwrapped.</summary>
    private static IEnumerable<Type> ReferencedTypes(Type type)
    {
        IEnumerable<Type> Unwrap(Type? t)
        {
            for (var current = t; current is not null;)
            {
                if (current.IsArray)
                {
                    current = current.GetElementType();
                    continue;
                }

                if (current.IsByRef)
                {
                    current = current.GetElementType();
                    continue;
                }

                if (current.IsGenericType && !current.IsGenericTypeDefinition)
                {
                    yield return current.GetGenericTypeDefinition();
                    foreach (var arg in current.GetGenericArguments())
                    {
                        foreach (var unwrapped in Unwrap(arg))
                        {
                            yield return unwrapped;
                        }
                    }

                    yield break;
                }

                if (current.IsGenericParameter)
                {
                    yield break;
                }

                yield return current;
                yield break;
            }
        }

        IEnumerable<Type> Shape()
        {
            foreach (var baseType in Unwrap(type.BaseType))
            {
                yield return baseType;
            }

            foreach (var implemented in type.GetInterfaces())
            {
                foreach (var unwrapped in Unwrap(implemented))
                {
                    yield return unwrapped;
                }
            }

            const BindingFlags All = BindingFlags.Instance | BindingFlags.Static
                | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            foreach (var field in type.GetFields(All))
            {
                foreach (var unwrapped in Unwrap(field.FieldType))
                {
                    yield return unwrapped;
                }

                yield return field.GetType(); // compiler-generated helper types stay in-namespace
            }

            foreach (var property in type.GetProperties(All))
            {
                foreach (var unwrapped in Unwrap(property.PropertyType))
                {
                    yield return unwrapped;
                }
            }

            foreach (var method in type.GetMethods(All))
            {
                foreach (var unwrapped in Unwrap(method.ReturnType))
                {
                    yield return unwrapped;
                }

                foreach (var parameter in method.GetParameters())
                {
                    foreach (var unwrapped in Unwrap(parameter.ParameterType))
                    {
                        yield return unwrapped;
                    }
                }
            }

            foreach (var ctor in type.GetConstructors(All))
            {
                foreach (var parameter in ctor.GetParameters())
                {
                    foreach (var unwrapped in Unwrap(parameter.ParameterType))
                    {
                        yield return unwrapped;
                    }
                }
            }

            foreach (var evt in type.GetEvents(All))
            {
                foreach (var unwrapped in Unwrap(evt.EventHandlerType))
                {
                    yield return unwrapped;
                }
            }

            foreach (var attr in type.GetCustomAttributesData())
            {
                yield return attr.AttributeType;
                foreach (var arg in attr.ConstructorArguments)
                {
                    if (arg.Value is Type typeArg)
                    {
                        yield return typeArg;
                    }
                }

                foreach (var named in attr.NamedArguments)
                {
                    if (named.TypedValue.Value is Type namedType)
                    {
                        yield return namedType;
                    }
                }
            }
        }

        foreach (var referenced in Shape())
        {
            if (referenced == type)
            {
                continue;
            }

            // Nested compiler-generated closures share the namespace — they are the type itself.
            if (referenced.IsNested && referenced.DeclaringType == type)
            {
                foreach (var closureMember in ReferencedTypes(referenced))
                {
                    yield return closureMember;
                }

                continue;
            }

            yield return referenced;
        }
    }

    private static void AssertNoReferences(IEnumerable<Type> subjects, Func<Type, bool> isForbidden, string ruleName)
    {
        var violations = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var type in subjects)
        {
            foreach (var referenced in ReferencedTypes(type))
            {
                if (isForbidden(referenced))
                {
                    violations.Add($"{type.FullName} -> {referenced.FullName}");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"{ruleName} violated:\n{string.Join("\n", violations.Take(20))}");
    }

    [Fact]
    public void DataEngine_KnowsNoWpf()
    {
        AssertNoReferences(
            TypesUnder("VCWpfControls.Grid.Data"),
            t => t.Namespace?.StartsWith("System.Windows", StringComparison.Ordinal) == true,
            "the data engine (Grid/Data) must not reference any System.Windows type (it runs without STA)");
    }

    [Fact]
    public void DataEngine_IsSelfContained()
    {
        AssertNoReferences(
            TypesUnder("VCWpfControls.Grid.Data"),
            t => t.Namespace?.StartsWith("VCWpfControls", StringComparison.Ordinal) == true
                && !t.Namespace.StartsWith("VCWpfControls.Grid.Data", StringComparison.Ordinal)
                && !t.Namespace.StartsWith("VCWpfControls.Localization", StringComparison.Ordinal),
            "the data engine may only use itself and the Localization strings");
    }

    [Fact]
    public void Localization_IsWpfFree()
    {
        AssertNoReferences(
            TypesUnder("VCWpfControls.Localization"),
            t => t.Namespace?.StartsWith("System.Windows", StringComparison.Ordinal) == true,
            "Localization must stay pure resx (it is the engine's only sanctioned dependency)");
    }
}
