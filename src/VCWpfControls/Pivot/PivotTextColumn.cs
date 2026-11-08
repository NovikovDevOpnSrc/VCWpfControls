using System.Windows;
using System.Windows.Data;
using VCWpfControls.Grid.Columns;

namespace VCWpfControls.Pivot;

/// <summary>
/// A text column bound to an arbitrary property path (e.g. <c>Cells[2]</c> or
/// <c>Values[0].Caption</c>) instead of the field name — used by <see cref="PivotGrid"/>
/// to render the computed pivot cells.
/// </summary>
public sealed class PivotTextColumn : GridTextColumn
{
    /// <summary>Identifies the <see cref="Path"/> dependency property.</summary>
    public static readonly DependencyProperty PathProperty = DependencyProperty.Register(
        nameof(Path), typeof(string), typeof(PivotTextColumn), new PropertyMetadata(string.Empty));

    /// <summary>The binding path of the cell value on <see cref="PivotRowItem"/>.</summary>
    public string Path
    {
        get => (string)GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    /// <inheritdoc />
    protected internal override Binding CreateCellBinding(BindingMode mode)
    {
        var binding = base.CreateCellBinding(mode);
        binding.Path = new PropertyPath(Path);
        return binding;
    }

    /// <summary>
    /// Resolves the DISPLAYED value from <see cref="Path"/> — the base would read
    /// <see cref="GridColumnBase.FieldName"/>, which on pivot columns names a whole
    /// collection, so automation peers and clipboard copy would carry
    /// <c>System.Object[]</c> instead of the cell text.
    /// </summary>
    public override object? GetCellValue(object item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item is not PivotRowItem row)
        {
            return base.GetCellValue(item);
        }

        if (TryIndex(Path, "Values[", out var valueIndex))
        {
            return valueIndex < row.Values.Count ? row.Values[valueIndex].Caption : null;
        }

        if (TryIndex(Path, "Cells[", out var cellIndex))
        {
            return cellIndex < row.Cells.Length ? row.Cells[cellIndex] : null;
        }

        if (TryIndex(Path, "Totals[", out var totalIndex))
        {
            return totalIndex < row.Totals.Length ? row.Totals[totalIndex] : null;
        }

        return null;
    }

    private static bool TryIndex(string path, string prefix, out int index)
    {
        index = -1;
        if (!path.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var start = prefix.Length;
        var end = path.IndexOf(']', start);
        return end > start && int.TryParse(path.AsSpan(start, end - start), out index);
    }
}
