using System.Globalization;
using System.Resources;

namespace VCWpfControls.Localization;

/// <summary>
/// Access to the grid's built-in localized strings (English, Russian and Chinese ship with the
/// package; add more by dropping additional <c>Strings.&lt;culture&gt;.resx</c> resources into
/// your application and registering the culture).
/// </summary>
public static class GridStrings
{
    private static readonly ResourceManager Manager = new("VCWpfControls.Localization.Strings", typeof(GridStrings).Assembly);

    /// <summary>Formats the group header row: group key and item count.</summary>
    public static string GroupRowItemCount(object? key, int count)
        => string.Format(CultureInfo.CurrentCulture, Get("GroupRowItemCount"), key ?? string.Empty, count);

    /// <summary>Watermark-free hint shown in empty filter cells.</summary>
    public static string FilterRowHint => Get("FilterRowHint");

    /// <summary>Context menu: sort ascending.</summary>
    public static string SortAscending => Get("SortAscending");

    /// <summary>Context menu: sort descending.</summary>
    public static string SortDescending => Get("SortDescending");

    /// <summary>Context menu: clear sorting.</summary>
    public static string ClearSorting => Get("ClearSorting");

    /// <summary>Context menu: hide the column.</summary>
    public static string HideColumn => Get("HideColumn");

    /// <summary>Context menu: columns section header.</summary>
    public static string ColumnsHeader => Get("ColumnsHeader");

    /// <summary>Hint shown in the empty group panel.</summary>
    public static string GroupPanelHint => Get("GroupPanelHint");

    /// <summary>Filter menu: select all values entry.</summary>
    public static string FilterMenuSelectAll => Get("FilterMenuSelectAll");

    /// <summary>Filter menu: clear button.</summary>
    public static string FilterMenuClear => Get("FilterMenuClear");

    /// <summary>Filter menu: apply button.</summary>
    public static string FilterMenuOk => Get("FilterMenuOk");

    /// <summary>Header menu: fit this column to its content.</summary>
    public static string BestFit => Get("BestFit");

    /// <summary>Header menu: stretch all columns to fill the control width.</summary>
    public static string BestFitAllFill => Get("BestFitAllFill");

    /// <summary>Shown centered when the grid has no rows.</summary>
    public static string NoData => Get("NoData");

    /// <summary>The condition builder "Add condition" caption.</summary>
    public static string ConditionAdd => Get("ConditionAdd");

    /// <summary>The condition builder "Add group" caption.</summary>
    public static string ConditionAddGroup => Get("ConditionAddGroup");

    /// <summary>The condition builder "Remove" caption.</summary>
    public static string ConditionRemove => Get("ConditionRemove");

    /// <summary>The And (all) group caption.</summary>
    public static string ConditionAll => Get("ConditionAll");

    /// <summary>The Or (any) group caption.</summary>
    public static string ConditionAny => Get("ConditionAny");

    /// <summary>The "Builder" tab caption.</summary>
    public static string ConditionBuilderTab => Get("ConditionBuilderTab");

    /// <summary>The "Text" tab caption.</summary>
    public static string ConditionTextTab => Get("ConditionTextTab");

    /// <summary>The "Preview" caption.</summary>
    public static string ConditionPreview => Get("ConditionPreview");

    /// <summary>The pivot totals caption.</summary>
    public static string PivotTotal => Get("PivotTotal");

    /// <summary>The default subject of a scheduler appointment created by a drag.</summary>
    public static string SchedulerNewAppointment => Get("SchedulerNewAppointment");

    /// <summary>The wizard "Next" button caption.</summary>
    public static string WizardNext => Get("WizardNext");

    /// <summary>The wizard "Finish" button caption.</summary>
    public static string WizardFinish => Get("WizardFinish");

    /// <summary>The wizard "Back" button caption.</summary>
    public static string WizardBack => Get("WizardBack");

    /// <summary>The wizard "Cancel" button caption.</summary>
    public static string WizardCancel => Get("WizardCancel");

    /// <summary>The "From" label of the range filter.</summary>
    public static string FilterFrom => Get("FilterFrom");

    /// <summary>The "To" label of the range filter.</summary>
    public static string FilterTo => Get("FilterTo");

    /// <summary>The "Filter..." header menu entry (opens the condition builder).</summary>
    public static string FilterEllipsis => Get("FilterEllipsis");

    /// <summary>The "Filter by values..." header menu entry (the value-check list).</summary>
    public static string FilterByValues => Get("FilterByValues");

    /// <summary>The column chooser dialog title.</summary>
    public static string ColumnChooserTitle => Get("ColumnChooserTitle");

    /// <summary>The visibility column header of the chooser dialog.</summary>
    public static string ColumnChooserShowColumn => Get("ColumnChooserShowColumn");

    /// <summary>The "Move up" button of the chooser dialog.</summary>
    public static string MoveUp => Get("MoveUp");

    /// <summary>The "Move down" button of the chooser dialog.</summary>
    public static string MoveDown => Get("MoveDown");

    /// <summary>The "Reset layout" header menu entry.</summary>
    public static string ResetLayout => Get("ResetLayout");

    /// <summary>The "Close" button caption.</summary>
    public static string Close => Get("Close");

    /// <summary>Header menu: fit all columns.</summary>
    public static string BestFitAll => Get("BestFitAll");

    /// <summary>Header menu: freeze columns up to this one.</summary>
    public static string FreezeColumn => Get("FreezeColumn");

    /// <summary>Header menu: group by this column.</summary>
    public static string GroupByColumn => Get("GroupByColumn");

    /// <summary>Fallback category name for uncategorized properties.</summary>
    public static string CategoryMisc => Get("CategoryMisc");

    /// <summary>Property grid search box hint.</summary>
    public static string PropertyGridSearchHint => Get("PropertyGridSearchHint");

    /// <summary>Context menu: export completed message.</summary>
    public static string ExportCompleted => Get("ExportCompleted");

    /// <summary>Formats a summary cell for the given function, field and value.</summary>
    public static string Summary(GridSummaryFunction function, string fieldName, object? value)
        => string.Format(
            CultureInfo.CurrentCulture,
            Get(function switch
            {
                GridSummaryFunction.Sum => "SummarySum",
                GridSummaryFunction.Min => "SummaryMin",
                GridSummaryFunction.Max => "SummaryMax",
                GridSummaryFunction.Avg => "SummaryAvg",
                _ => "SummaryCount",
            }),
            fieldName,
            value);

    private static string Get(string key) => Manager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}

/// <summary>Aggregate functions available for grid summaries.</summary>
public enum GridSummaryFunction
{
    /// <summary>Sum of numeric values.</summary>
    Sum = 0,

    /// <summary>Minimum value.</summary>
    Min = 1,

    /// <summary>Maximum value.</summary>
    Max = 2,

    /// <summary>Average of numeric values.</summary>
    Avg = 3,

    /// <summary>Number of rows.</summary>
    Count = 4,
}
