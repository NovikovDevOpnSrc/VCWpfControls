using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace VCWpfControls.Control.Tests.Infrastructure;

/// <summary>
/// Detects SILENT binding failures in a realized visual tree — the browser-verification
/// idea adapted: a web skill reads the console, we walk the tree and ask every local
/// <see cref="BindingExpression"/> for its <see cref="BindingExpression.Status"/>. This
/// bug class is real here: the Department editor once rendered empty because a path
/// missed the <c>Data.</c> prefix — nothing crashed and nothing was logged.
///
/// NOTE: the classic global channel (<c>PresentationTraceSources.DataBindingSource</c>)
/// is DEAD on .NET Core WPF — verified empirically (canonical listener/Refresh/level
/// ordering + per-binding <c>TraceLevel=High</c> + a working-binding control: zero
/// events). The supported per-expression status API is the only reliable instrument.
/// </summary>
public static class BindingErrorScanner
{
    /// <summary>
    /// Collects the failed bindings under <paramref name="root"/>: "owner type: property
    /// -> status". Path and update-source errors are reported; <c>Inactive</c> (a null
    /// DataContext waiting for data, e.g. an empty grid's placeholder) is not a failure.
    /// </summary>
    public static IReadOnlyList<string> FailuresUnder(DependencyObject root)
    {
        var failures = new List<string>();
        Walk(root, failures);
        return failures;
    }

    private static void Walk(DependencyObject node, List<string> failures)
    {
        var enumerator = node.GetLocalValueEnumerator();
        while (enumerator.MoveNext())
        {
            if (enumerator.Current.Value is BindingExpression { Status: BindingStatus.PathError or BindingStatus.UpdateSourceError } expression)
            {
                failures.Add($"{node.GetType().Name}: {enumerator.Current.Property.Name} -> {expression.Status} ({expression.ParentBinding.Path.Path})");
            }
        }

        var children = VisualTreeHelper.GetChildrenCount(node);
        for (var i = 0; i < children; i++)
        {
            Walk(VisualTreeHelper.GetChild(node, i), failures);
        }
    }
}
