---
title: Tree list
description: Hierarchical data with columns, expand/collapse and per-level sorting.
---

# TreeListView

`VCWpfControls.TreeList.TreeListView` renders a hierarchy as a grid: roots come from
`ItemsSource`, children are read from `ChildMemberPath` (any `IEnumerable` property).
Everything from `GridControl` is inherited: manual/auto columns, virtualization, selection,
editing, row details, themes, keyboard.

## Quick start

```xml
<Window xmlns:tree="clr-namespace:VCWpfControls.TreeList;assembly=VCWpfControls"
        xmlns:columns="clr-namespace:VCWpfControls.Grid.Columns;assembly=VCWpfControls">
    <tree:TreeListView ItemsSource="{Binding Units}" ChildMemberPath="Children">
        <tree:TreeListView.Columns>
            <columns:GridTextColumn FieldName="Name" Header="Unit" Width="2*" />
            <columns:GridNumericColumn FieldName="Headcount" Header="Headcount" Width="110" />
        </tree:TreeListView.Columns>
    </tree:TreeListView>
</Window>
```

## Expansion

- Nodes start collapsed; set `InitiallyExpanded="True"` to expand the first level.
- API: `ToggleNode(item, expanded)`, `ExpandAll()`, `CollapseAll()`, `IsExpanded(item)`;
  the expander toggle in the first column does the same per node.
- Expansion state survives sorting and collection changes.

## Sorting

Header clicks and `SortBy`/`AddSortLevel` order the children **within each level**
(roots among roots, their children among siblings, DFS display order).

## Live updates

Changes in the roots collection or in any materialized children collection
(`ObservableCollection` recommended) rebuild the view automatically.

## Filtering

Set `FilterPredicate` (any `Predicate<object>`): a node is visible when it matches or any descendant matches; ancestors of matches are shown and temporarily expanded. Manual expansion state is restored when the predicate is cleared.

## Not yet (see ROADMAP)

Filtering of tree nodes, load-on-demand children, group panel/filter row do not apply.
