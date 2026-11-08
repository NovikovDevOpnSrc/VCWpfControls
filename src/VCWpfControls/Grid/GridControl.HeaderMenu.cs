using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Data;
using VCWpfControls.Grid.Primitives;
using VCWpfControls.Localization;

namespace VCWpfControls.Grid;

/// <summary>
/// The default header context menu of <see cref="GridControl"/>: sorting (with state marks),
/// column content fit, grouping, freezing, hiding and a column-visibility chooser. Localized
/// (EN/RU/ZH). Built in code so it always matches the current grid state.
/// </summary>
public partial class GridControl
{
    /// <summary>Builds the default context menu for a column header.</summary>
    internal ContextMenu BuildColumnHeaderMenu(GridColumnBase column)
    {
        var menu = new ContextMenu();

        var sortAscending = new MenuItem
        {
            Header = GridStrings.SortAscending,
            IsCheckable = true,
            IsChecked = column.SortDirection == GridSortDirection.Ascending,
        };
        sortAscending.Click += (_, _) => SortBy(column, GridSortDirection.Ascending);

        var sortDescending = new MenuItem
        {
            Header = GridStrings.SortDescending,
            IsCheckable = true,
            IsChecked = column.SortDirection == GridSortDirection.Descending,
        };
        sortDescending.Click += (_, _) => SortBy(column, GridSortDirection.Descending);

        var clearSorting = new MenuItem
        {
            Header = GridStrings.ClearSorting,
            IsEnabled = SortDescriptions.Count > 0,
        };
        clearSorting.Click += (_, _) => ClearSorting();

        menu.Items.Add(sortAscending);
        menu.Items.Add(sortDescending);
        menu.Items.Add(clearSorting);

        // Filter...: the CONDITION builder preseeded with this column (filter by
        // condition); the value-check list moved under "Filter by values...".
        var filter = new MenuItem
        {
            Header = GridStrings.FilterEllipsis,
            IsEnabled = column.CanUserFilter,
        };
        filter.Click += (_, _) => OpenConditionDialog(column);
        menu.Items.Add(filter);

        var filterValues = new MenuItem
        {
            Header = GridStrings.FilterByValues,
            IsEnabled = column.CanUserFilter,
        };
        filterValues.Click += (_, _) => OpenColumnFilterMenu(column);
        menu.Items.Add(filterValues);
        menu.Items.Add(new Separator());

        var bestFit = new MenuItem { Header = GridStrings.BestFit };
        bestFit.Click += (_, _) => BestFitColumn(column);
        menu.Items.Add(bestFit);

        var bestFitAll = new MenuItem { Header = GridStrings.BestFitAll };
        bestFitAll.Click += (_, _) => BestFitAllColumns();
        menu.Items.Add(bestFitAll);

        var fillAll = new MenuItem { Header = GridStrings.BestFitAllFill };
        fillAll.Click += (_, _) => FillColumnsToViewport();
        menu.Items.Add(fillAll);
        menu.Items.Add(new Separator());

        var groupField = column.EffectiveSortPath;
        var grouped = GroupDescriptions.Any(g => string.Equals(g.FieldName, groupField, StringComparison.OrdinalIgnoreCase));
        var groupBy = new MenuItem
        {
            Header = GridStrings.GroupByColumn,
            IsCheckable = true,
            IsChecked = grouped,
            IsEnabled = CanUserGroupColumns && !string.IsNullOrEmpty(groupField),
        };
        groupBy.Click += (_, _) =>
        {
            var existing = GroupDescriptions.FirstOrDefault(g => string.Equals(g.FieldName, groupField, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                GroupDescriptions.Remove(existing);
            }
            else if (!string.IsNullOrEmpty(groupField))
            {
                GroupDescriptions.Add(new GridGroupDescription(groupField));
            }
        };
        menu.Items.Add(groupBy);

        var visibleIndex = 0;
        for (var i = 0; i < VisibleColumns.Count; i++)
        {
            if (ReferenceEquals(VisibleColumns[i], column))
            {
                visibleIndex = i;
                break;
            }

            if (i == VisibleColumns.Count - 1)
            {
                visibleIndex = -1;
            }
        }

        var frozen = visibleIndex >= 0 && visibleIndex < FrozenColumnCount;
        var freeze = new MenuItem
        {
            Header = GridStrings.FreezeColumn,
            IsCheckable = true,
            IsChecked = frozen,
            IsEnabled = visibleIndex >= 0,
        };
        freeze.Click += (_, _) => FrozenColumnCount = frozen ? 0 : visibleIndex + 1;
        menu.Items.Add(freeze);

        var hide = new MenuItem
        {
            Header = GridStrings.HideColumn,
            IsEnabled = VisibleColumns.Count > 1, // keep at least one column visible
        };
        hide.Click += (_, _) => column.Visible = false;
        menu.Items.Add(hide);
        menu.Items.Add(new Separator());

        var chooser = new MenuItem { Header = GridStrings.ColumnsHeader };
        var chooserDialog = new MenuItem { Header = GridStrings.ColumnChooserTitle + "..." };
        chooserDialog.Click += (_, _) => OpenColumnChooser();
        chooser.Items.Add(chooserDialog);
        chooser.Items.Add(new Separator());
        foreach (var candidate in Columns)
        {
            var toggleColumn = candidate;
            var item = new MenuItem
            {
                Header = GridColumnBase.HeaderText(candidate.Header) ?? candidate.FieldName,
                IsCheckable = true,
                IsChecked = candidate.Visible,
            };
            item.Click += (_, _) => toggleColumn.Visible = !toggleColumn.Visible;
            chooser.Items.Add(item);
        }

        menu.Items.Add(chooser);

        var reset = new MenuItem
        {
            Header = GridStrings.ResetLayout,
            IsEnabled = _initialLayout is not null,
        };
        reset.Click += (_, _) => ResetLayout();
        menu.Items.Add(reset);
        return menu;
    }

    /// <summary>
    /// Opens a modal ConditionBuilder over the grid's item type; OK applies the tree as
    /// <see cref="CustomFilterPredicate"/> (through ConditionEvaluator), Clear removes the
    /// filter, Cancel keeps everything. When <paramref name="column"/> is given (the
    /// header-menu "Filter..." path) the builder starts with one condition on that
    /// column's field instead of an empty group.
    /// </summary>
    public void OpenConditionDialog(GridColumnBase? column = null)
    {
        if (ResolveSourceItemTypeForConditions() is not { } itemType)
        {
            return;
        }

        var schema = Conditions.ConditionSchema.FromType(itemType);
        var dialog = new Window
        {
            Title = GridStrings.FilterEllipsis,
            Owner = Window.GetWindow(this),
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 640,
            Height = 480,
            MinWidth = 480,
            MinHeight = 360,
        };

        var builder = new Conditions.ConditionBuilder
        {
            Schema = schema,
            Condition = _conditionTree ?? PreseededCondition(column, schema),
            Margin = new Thickness(12),
        };
        _conditionTree ??= builder.Condition;

        AttachConditionDialogHandlers(dialog, builder);
        _conditionDialogForTest = dialog;
        dialog.Closed += (_, _) => _conditionDialogForTest = null;
        if (ShowConditionDialogModally)
        {
            dialog.ShowDialog();
        }
        else
        {
            dialog.Show();
        }
    }

    /// <summary>
    /// The dialog's initial tree for a column-scoped "Filter...": one Equals condition on
    /// the column's field. Falls back to an empty group when the field is not in the schema
    /// (computed paths) or no column was given.
    /// </summary>
    private static Conditions.ConditionNode PreseededCondition(GridColumnBase? column, Conditions.ConditionSchema schema)
    {
        if (column is not null
            && !string.IsNullOrEmpty(column.EffectiveSortPath)
            && schema.Fields.Any(f => string.Equals(f.Name, column.EffectiveSortPath, StringComparison.OrdinalIgnoreCase)))
        {
            return new Conditions.ConditionGroup(Conditions.ConditionGroupOperator.And)
            {
                Children = { new Conditions.ConditionExpression(column.EffectiveSortPath, GridFilterOperator.Equals) },
            };
        }

        return new Conditions.ConditionGroup(Conditions.ConditionGroupOperator.And);
    }

    /// <summary>Whether the condition dialog runs modal (<c>ShowDialog</c>); tests turn this off.</summary>
    internal bool ShowConditionDialogModally { get; set; } = true;

    /// <summary>Test hook: the condition dialog without the modal loop.</summary>
    internal Window OpenConditionDialogForTest(GridColumnBase? column = null)
    {
        ShowConditionDialogModally = false;
        try
        {
            OpenConditionDialog(column);
        }
        finally
        {
            ShowConditionDialogModally = true;
        }

        return ConditionDialogForTest ?? throw new InvalidOperationException($"ResolvedItemType is null; source={PivotItemsSourceDebugText()}");
    }

    /// <summary>
    /// The item type for the condition dialog: the engine's resolved type, or inferred from
    /// the RAW source — a predicate that filters everything out leaves the engine typeless.
    /// </summary>
    private Type? ResolveSourceItemTypeForConditions()
    {
        if (DataEngine.ResolvedItemType is { } resolved)
        {
            return resolved;
        }

        if (_unfilteredSource is System.Collections.IEnumerable { } raw && TryGetItemType(raw, out var inferred))
        {
            return inferred;
        }

        return null;
    }

    private string PivotItemsSourceDebugText()
        => ItemsSource?.GetType().Name ?? "null";

    private void AttachConditionDialogHandlers(Window dialog, Conditions.ConditionBuilder builder)
    {
        var ok = new Button { Content = GridStrings.FilterMenuOk, MinWidth = 80, Margin = new Thickness(4), Padding = new Thickness(10, 4, 10, 4) };
        var cancel = new Button { Content = GridStrings.WizardCancel, MinWidth = 80, Margin = new Thickness(4), Padding = new Thickness(10, 4, 10, 4) };
        var clear = new Button { Content = GridStrings.FilterMenuClear, MinWidth = 80, Margin = new Thickness(4), Padding = new Thickness(10, 4, 10, 4) };

        ok.Click += (_, _) =>
        {
            _conditionTree = builder.Condition;
            CustomFilterPredicate = _conditionTree is null || _conditionTree.IsEmpty
                ? null
                : item => Conditions.ConditionEvaluator.Matches(item!, _conditionTree);
            dialog.Close();
        };
        clear.Click += (_, _) =>
        {
            _conditionTree = null;
            CustomFilterPredicate = null;
            dialog.Close();
        };
        cancel.Click += (_, _) => dialog.Close();

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(clear);
        buttons.Children.Add(cancel);
        buttons.Children.Add(ok);

        var root = new DockPanel();
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);
        root.Children.Add(builder);
        dialog.Content = root;
    }

    private Conditions.ConditionNode? _conditionTree;
    private Window? _conditionDialogForTest;

    /// <summary>The open condition dialog, for tests (null when closed).</summary>
    internal Window? ConditionDialogForTest => _conditionDialogForTest;

    /// <summary>Test hook: the dialog's builder.</summary>
    internal Conditions.ConditionBuilder? ConditionBuilderForTest
        => _conditionDialogForTest?.Content is DockPanel root && root.Children.Count > 0 && root.Children[1] is Conditions.ConditionBuilder builder
            ? builder
            : null;

    /// <summary>The active condition tree (set by the dialog), for tests and callers.</summary>
    internal Conditions.ConditionNode? ConditionTreeForTest => _conditionTree;

    /// <summary>Opens the column chooser dialog (visibility + reorder).</summary>
    public void OpenColumnChooser() => OpenColumnChooserForTest();

    /// <summary>Opens the chooser and returns it (test hook).</summary>
    internal Primitives.GridColumnChooserWindow OpenColumnChooserForTest()
    {
        var window = new Primitives.GridColumnChooserWindow(this)
        {
            Owner = Window.GetWindow(this),
        };
        window.Show();
        return window;
    }

    /// <summary>
    /// Restores the layout captured when the columns were first built: the column order,
    /// every column's width and visibility, and the frozen column count. The snapshot is
    /// taken once, after column generation settles.
    /// </summary>
    public void ResetLayout()
    {
        if (_initialLayout is not { } snapshot)
        {
            return;
        }

        _resettingLayout = true;
        try
        {
            FrozenColumnCount = _initialFrozenColumnCount;
            foreach (var (column, width, visible) in snapshot)
            {
                column.Width = width;
                column.Visible = visible;
            }

            // Reorder by removing everything and re-inserting in the snapshot order.
            foreach (var (column, _, _) in snapshot)
            {
                Columns.Remove(column);
            }

            var index = 0;
            foreach (var (column, _, _) in snapshot)
            {
                Columns.Insert(index++, column);
            }
        }
        finally
        {
            _resettingLayout = false;
            InvalidateLayout();
            RefreshVisibleColumns(rebuildCells: true);
        }
    }

    /// <summary>Opens the default header context menu below the given header.</summary>
    internal void OpenColumnHeaderMenu(GridColumnBase column, GridColumnHeader header)
    {
        var menu = BuildColumnHeaderMenu(column);
        menu.PlacementTarget = header;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }
}
