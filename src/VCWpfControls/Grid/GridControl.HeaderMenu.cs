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
        menu.Items.Add(new Separator());

        var bestFit = new MenuItem { Header = GridStrings.BestFit };
        bestFit.Click += (_, _) => BestFitColumn(column);
        menu.Items.Add(bestFit);

        var bestFitAll = new MenuItem { Header = GridStrings.BestFitAll };
        bestFitAll.Click += (_, _) => BestFitAllColumns();
        menu.Items.Add(bestFitAll);
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
        foreach (var candidate in Columns)
        {
            var toggleColumn = candidate;
            var item = new MenuItem
            {
                Header = Convert.ToString(candidate.Header, System.Globalization.CultureInfo.CurrentCulture) ?? candidate.FieldName,
                IsCheckable = true,
                IsChecked = candidate.Visible,
            };
            item.Click += (_, _) => toggleColumn.Visible = !toggleColumn.Visible;
            chooser.Items.Add(item);
        }

        menu.Items.Add(chooser);
        return menu;
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
