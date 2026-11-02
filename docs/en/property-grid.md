---
title: Property grid
description: Edit the properties of any object with typed editors, categories and search.
---

# Property grid

`VCWpfControls.PropertyGrid.PropertyGrid` shows and edits the public properties of any object:
typed editors, category grouping, search, expandable nested objects and live updates through
`INotifyPropertyChanged`.

## Quick start

```xml
<Window xmlns:pg="clr-namespace:VCWpfControls.PropertyGrid;assembly=VCWpfControls">
    <pg:PropertyGrid SelectedObject="{Binding SelectedEmployee}" />
</Window>
```

A common pattern is pairing it with a `GridControl`:

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="380" />
    </Grid.ColumnDefinitions>
    <grid:GridControl x:Name="Grid" ItemsSource="{Binding Employees}" />
    <pg:PropertyGrid Grid.Column="1" SelectedObject="{Binding SelectedItem, ElementName=Grid}" />
</Grid>
```

## Features

- **Typed editors** resolved from property types: text box, multi-line text
  (`[DataType(DataType.MultilineText)]`), check box, enum combo box, right-aligned numeric box,
  date picker, read-only text for collections and unsupported types.
- **Metadata** via attributes: `[Display]` (`Name`, `Description`, `Order`, `GroupName`),
  `[Category]`, `[DisplayName]`, `[ReadOnly]`, `[Browsable(false)]`.
- **Categories** rendered as expanders (`ShowCategories="False"` for a flat list).
- **Search box** (`ShowSearchBox`) filtering properties by display or CLR name.
- **Expandable properties**: complex objects get a `+` toggle revealing nested properties
  (lazy, indented).
- **Live updates**: when the selected object implements `INotifyPropertyChanged`, edited or
  external changes refresh the corresponding editor.
- **Events**: `PropertyValueChanged` fires after each committed edit (the value is already
  written to the object).
- **Options**: `LabelWidth` controls the label column width.

## Editing semantics

- Text editors commit on **Enter** and on focus loss; invalid input (e.g. letters in a numeric
  field) is silently reverted to the current value.
- Invalid enum/combo selections cannot happen; check boxes and date pickers commit immediately.
- Read-only properties and collections are displayed but not editable.

## Theming and localization

The control uses the same `GridResources.*` resource keys as the grid (see
[Theming](theming.md)); the search hint and the "Misc" category name are localized
(EN/RU/ZH ship with the package).
