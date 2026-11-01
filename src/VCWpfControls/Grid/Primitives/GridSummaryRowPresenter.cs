using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Data;
using VCWpfControls.Localization;

namespace VCWpfControls.Grid.Primitives;

/// <summary>
/// The footer row showing aggregate summaries (see <c>GridControl.SummaryItems</c>),
/// one cell per visible column, aligned like the data cells.
/// </summary>
public sealed class GridSummaryRowPresenter : Panel
{
    private readonly Dictionary<GridColumnBase, TextBlock> _cellsByColumn = new();

    private static readonly DependencyPropertyKey HasContentPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasContent), typeof(bool), typeof(GridSummaryRowPresenter), new PropertyMetadata(false));

    /// <summary>Identifies the read-only <see cref="HasContent"/> dependency property.</summary>
    public static readonly DependencyProperty HasContentProperty = HasContentPropertyKey.DependencyProperty;

    /// <summary>The owning grid, attached when the control template is applied.</summary>
    internal GridControl? Owner { get; set; }

    /// <summary>Whether any summary cell currently has text (controls row visibility).</summary>
    public bool HasContent => (bool)GetValue(HasContentProperty);

    /// <summary>Builds or reuses one summary cell per visible column.</summary>
    internal void EnsureCells()
    {
        if (Owner is null)
        {
            return;
        }

        Children.Clear();
        foreach (var column in Owner.VisibleColumns)
        {
            if (!_cellsByColumn.TryGetValue(column, out var cell))
            {
                cell = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                };
                _cellsByColumn[column] = cell;
            }

            Children.Add(cell);
        }

        RefreshValues();
        InvalidateMeasure();
    }

    /// <summary>Recomputes and renders the summary texts.</summary>
    internal void RefreshValues()
    {
        if (Owner is null)
        {
            return;
        }

        SetValue(HasContentPropertyKey, false);
        var columns = Owner.VisibleColumns;
        var items = Owner.DataEngine.DataRows.Select(row => row.Item).ToList();

        for (var i = 0; i < columns.Count; i++)
        {
            if (i >= Children.Count || Children[i] is not TextBlock cell)
            {
                continue;
            }

            var text = ComputeCellText(columns[i], items);
            cell.Text = text;
            cell.TextAlignment = columns[i].HorizontalContentAlignment == HorizontalAlignment.Right
                ? TextAlignment.Right
                : TextAlignment.Left;
            if (!string.IsNullOrEmpty(text))
            {
                SetValue(HasContentPropertyKey, true);
            }
        }

        InvalidateMeasure();
    }

    private string ComputeCellText(GridColumnBase column, List<object> items)
    {
        var summaries = Owner!.SummaryItems;
        var found = false;
        var parts = new List<string>();
        foreach (var summary in summaries)
        {
            if (!string.Equals(summary.FieldName, column.FieldName, StringComparison.OrdinalIgnoreCase)
                && summary.Function != GridSummaryFunction.Count)
            {
                continue;
            }

            if (summary.Function == GridSummaryFunction.Count
                && !string.IsNullOrEmpty(summary.FieldName)
                && !string.Equals(summary.FieldName, column.FieldName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = GridSummaryCalculator.Calculate(
                summary.Function,
                summary.FieldName,
                items,
                (type, path) => Owner.DataEngine.GetAccessor(type, path));

            var formatted = FormatValue(value, summary.StringFormat ?? column.StringFormat);
            parts.Add(GridStrings.Summary(summary.Function, summary.FieldName, formatted));
            found = true;
        }

        return found ? string.Join("  |  ", parts) : string.Empty;
    }

    private static string FormatValue(object? value, string? format)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return string.IsNullOrEmpty(format)
            ? Convert.ToString(value, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty
            : string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:" + format + "}", value);
    }

    /// <summary>Measures summary cells with their column's slot width.</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Owner is null)
        {
            return default;
        }

        var columns = Owner.VisibleColumns;
        var height = 0d;
        for (var i = 0; i < columns.Count && i < Children.Count; i++)
        {
            var child = Children[i];
            var slot = Owner.Layout.GetSlot(columns[i]);
            child.Measure(new Size(slot?.Width ?? 0, availableSize.Height));
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(availableSize.Width, height + 6);
    }

    /// <summary>Arranges summary cells exactly like header cells.</summary>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Owner is null)
        {
            return finalSize;
        }

        var columns = Owner.VisibleColumns;
        var offset = Owner.HorizontalOffset;
        for (var i = 0; i < columns.Count && i < Children.Count; i++)
        {
            var child = Children[i];
            var slot = Owner.Layout.GetSlot(columns[i]);
            if (slot is null)
            {
                child.Arrange(new Rect(0, 0, 0, 0));
                continue;
            }

            var x = slot.IsFrozen ? slot.X : slot.X - offset;
            child.Arrange(new Rect(x, 0, slot.Width, finalSize.Height - 6));
        }

        return finalSize;
    }
}
