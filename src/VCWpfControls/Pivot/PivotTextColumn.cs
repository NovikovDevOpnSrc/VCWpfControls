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
}
