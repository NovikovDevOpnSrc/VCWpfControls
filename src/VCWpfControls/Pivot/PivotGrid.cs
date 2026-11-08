using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Localization;

namespace VCWpfControls.Pivot;

/// <summary>
/// One rendered pivot row: the composite axis values, the cell aggregates (in the same
/// order as the generated data columns) and the row totals per measure.
/// </summary>
public sealed class PivotRowItem
{
    /// <summary>Axis captions of the row fields.</summary>
    public IReadOnlyList<PivotAxisValue> Values { get; init; } = Array.Empty<PivotAxisValue>();

    /// <summary>Cell values in generated column order (column combination x measure).</summary>
    public object?[] Cells { get; init; } = Array.Empty<object?>();

    /// <summary>Row totals per measure.</summary>
    public object?[] Totals { get; init; } = Array.Empty<object?>();

    /// <summary>Whether this is the grand-total row rendered last.</summary>
    public bool IsTotal { get; init; }
}

/// <summary>Event arguments of <see cref="PivotGrid.DrillDownRequested"/>.</summary>
public sealed class PivotDrillDownEventArgs : EventArgs
{
    /// <summary>Creates the args.</summary>
    public PivotDrillDownEventArgs(PivotRowItem row, Grid.Columns.GridColumnBase column, IReadOnlyList<object> sourceRows, object?[] aggregates)
    {
        Row = row;
        Column = column;
        SourceRows = sourceRows;
        Aggregates = aggregates;
    }

    /// <summary>The clicked pivot row.</summary>
    public PivotRowItem Row { get; }

    /// <summary>The clicked data column.</summary>
    public Grid.Columns.GridColumnBase Column { get; }

    /// <summary>The source rows aggregated into the clicked cell.</summary>
    public IReadOnlyList<object> SourceRows { get; }

    /// <summary>The cell's aggregates (per measure).</summary>
    public object?[] Aggregates { get; }
}

/// <summary>
/// A pivot table control: computes <see cref="PivotEngine"/> results from
/// <see cref="PivotItemsSource"/> and renders them in a <see cref="GridControl"/> — one column
/// per column-field combination (x measure), leading columns per row field, a totals
/// column and a grand-total row. Themes, virtualization and the header UI come from the
/// underlying grid.
/// </summary>
public class PivotGrid : GridControl
{
    /// <summary>Identifies the <see cref="PivotItemsSource"/> dependency property.</summary>
    public static readonly DependencyProperty PivotItemsSourceProperty = DependencyProperty.Register(
        nameof(PivotItemsSource), typeof(IEnumerable), typeof(PivotGrid),
        new PropertyMetadata(null, OnConfigurationChanged));

    /// <summary>Identifies the <see cref="IncludeTotals"/> dependency property.</summary>
    public static readonly DependencyProperty IncludeTotalsProperty = DependencyProperty.Register(
        nameof(IncludeTotals), typeof(bool), typeof(PivotGrid),
        new PropertyMetadata(GridColumnBase.Boxed.BoolTrue, OnConfigurationChanged));

    /// <summary>Identifies the <see cref="Condition"/> dependency property.</summary>
    public static readonly DependencyProperty ConditionProperty = DependencyProperty.Register(
        nameof(Condition), typeof(VCWpfControls.Conditions.ConditionNode), typeof(PivotGrid),
        new PropertyMetadata(null, OnConfigurationChanged));

    /// <summary>
    /// A condition tree filtering the SOURCE ROWS before bucketing (bridge to the
    /// condition editor; evaluated with ConditionEvaluator). Null disables it.
    /// </summary>
    public VCWpfControls.Conditions.ConditionNode? Condition
    {
        get => (VCWpfControls.Conditions.ConditionNode?)GetValue(ConditionProperty);
        set => SetValue(ConditionProperty, value);
    }

    /// <summary>Identifies the <see cref="FieldFilter"/> dependency property.</summary>
    public static readonly DependencyProperty FieldFilterProperty = DependencyProperty.Register(
        nameof(FieldFilter), typeof(Func<string, object?, bool>), typeof(PivotGrid),
        new PropertyMetadata(null, OnConfigurationChanged));

    /// <summary>Creates the pivot grid.</summary>
    public PivotGrid()
    {
        AutoGenerateColumns = false;
        RowFields.CollectionChanged += (_, _) => Recompute();
        ColumnFields.CollectionChanged += (_, _) => Recompute();
        Measures.CollectionChanged += (_, _) => Recompute();
    }

    /// <summary>The source rows to aggregate (bind your collection here).</summary>
    public IEnumerable? PivotItemsSource
    {
        get => (IEnumerable?)GetValue(PivotItemsSourceProperty);
        set => SetValue(PivotItemsSourceProperty, value);
    }

    /// <summary>Fields whose values form the row axis (composite, in order).</summary>
    public ObservableCollection<string> RowFields { get; } = new();

    /// <summary>Fields whose values form the column axis.</summary>
    public ObservableCollection<string> ColumnFields { get; } = new();

    /// <summary>Aggregates shown in the cells (at least one required).</summary>
    public ObservableCollection<PivotMeasure> Measures { get; } = new();

    /// <summary>Whether to add the totals column and the grand-total row.</summary>
    public bool IncludeTotals
    {
        get => (bool)GetValue(IncludeTotalsProperty);
        set => SetValue(IncludeTotalsProperty, value);
    }

    /// <summary>
    /// Dimension filter applied before bucketing: receives (fieldName, value) and returns
    /// false to exclude the source row. Affects every row/column field.
    /// </summary>
    public Func<string, object?, bool>? FieldFilter
    {
        get => (Func<string, object?, bool>?)GetValue(FieldFilterProperty);
        set => SetValue(FieldFilterProperty, value);
    }

    /// <summary>The source rows behind one cell (drill-down); empty for the grand-total row.</summary>
    public IReadOnlyList<object> GetDrillDownRows(int rowIndex, int columnIndex)
    {
        if (Result?.SourceRows is not { } rows
            || rowIndex < 0 || rowIndex >= rows.Length
            || columnIndex < 0 || columnIndex >= rows[rowIndex].Length)
        {
            return Array.Empty<object>();
        }

        return rows[rowIndex][columnIndex] ?? Array.Empty<object>();
    }

    /// <summary>Raised when the user double-clicks a data cell — carries the source rows behind it.</summary>
    public event EventHandler<PivotDrillDownEventArgs>? DrillDownRequested;

    /// <inheritdoc />
    protected override void OnMouseDoubleClick(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        RaiseDrillDownForCurrentCell();
    }

    /// <summary>
    /// Raises <see cref="DrillDownRequested"/> for the current data cell (double-click
    /// path; also callable programmatically).
    /// </summary>
    internal void RaiseDrillDownForCurrentCell()
    {
        // The current cell is already updated by the mouse-down pass.
        if (CurrentItem is PivotRowItem row && !row.IsTotal
            && CurrentColumn is PivotTextColumn column
            && _dataColumnIndexes.TryGetValue(column, out var cellIndex))
        {
            DrillDownRequested?.Invoke(this, new PivotDrillDownEventArgs(
                row,
                column,
                GetDrillDownRows(cellIndex.Row, cellIndex.Column),
                Result!.Cells[cellIndex.Row][cellIndex.Column]));
        }
    }

    private readonly Dictionary<Grid.Columns.GridColumnBase, (int Row, int Column)> _dataColumnIndexes = new();

    /// <summary>The last computed result (null before the first computation).</summary>
    public PivotResult? Result { get; private set; }

    private static void OnConfigurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PivotGrid)d).Recompute();

    /// <summary>Recomputes the pivot and rebuilds the columns and items.</summary>
    public void Recompute()
    {
        if (PivotItemsSource is null || Measures.Count == 0)
        {
            return;
        }

        var source = Condition is null or { IsEmpty: true }
            ? PivotItemsSource
            : VCWpfControls.Conditions.ConditionEvaluator.Where(PivotItemsSource!, Condition);
        Result = PivotEngine.Compute(source, RowFields, ColumnFields, Measures, IncludeTotals, FieldFilter, includeSourceRows: true);
        var result = Result;
        var measureCount = Measures.Count;

        Columns.Clear();
        _dataColumnIndexes.Clear();
        for (var i = 0; i < RowFields.Count; i++)
        {
            Columns.Add(new PivotTextColumn
            {
                FieldName = "Values",
                Header = RowFields[i],
                Width = new GridColumnWidth(160, GridUnitType.Pixel),
                Path = $"Values[{i}].Caption",
            });
        }

        int ColumnCellIndex(int combo, int measure) => combo * measureCount + measure;

        for (var combo = 0; combo < result.Columns.Count; combo++)
        {
            var caption = string.Join(
                " / ",
                result.Columns[combo].Select(v => v.Caption.Length == 0 ? "—" : v.Caption));
            for (var m = 0; m < measureCount; m++)
            {
                var header = measureCount == 1 ? caption : $"{caption} · {Measures[m]}";
                var dataColumn = new PivotTextColumn
                {
                    FieldName = "Cells",
                    Header = header,
                    Width = new GridColumnWidth(1, GridUnitType.Star),
                    Path = $"Cells[{ColumnCellIndex(combo, m)}]",
                };
                Columns.Add(dataColumn);
                _dataColumnIndexes[dataColumn] = (combo, m);
            }
        }

        if (IncludeTotals)
        {
            for (var m = 0; m < measureCount; m++)
            {
                Columns.Add(new PivotTextColumn
                {
                    FieldName = "Totals",
                    Header = measureCount == 1 ? GridStrings.PivotTotal : $"{GridStrings.PivotTotal} · {Measures[m]}",
                    Width = new GridColumnWidth(1, GridUnitType.Star),
                    Path = $"Totals[{m}]",
                });
            }
        }

        var rows = new List<PivotRowItem>();
        foreach (var (rowValues, r) in result.Rows.Select((values, index) => (values, index)))
        {
            var cells = new object?[result.Columns.Count * measureCount];
            for (var combo = 0; combo < result.Columns.Count; combo++)
            {
                for (var m = 0; m < measureCount; m++)
                {
                    cells[ColumnCellIndex(combo, m)] = result.Cells[r][combo][m];
                }
            }

            rows.Add(new PivotRowItem
            {
                Values = rowValues,
                Cells = cells,
                Totals = IncludeTotals ? result.RowTotals[r] : Array.Empty<object?>(),
            });
        }

        if (IncludeTotals && rows.Count > 0)
        {
            rows.Add(new PivotRowItem
            {
                Values = RowFields.Select(f => new PivotAxisValue(GridStrings.PivotTotal)).ToList(),
                Cells = result.ColumnTotals.SelectMany(t => t).ToArray(),
                Totals = result.GrandTotals.ToArray(),
                IsTotal = true,
            });
        }

        ItemsSource = rows;
    }
}
