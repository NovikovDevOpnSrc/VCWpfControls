---
title: 树形列表
description: 带列的层级数据，支持展开/折叠与按级别排序。
---

# TreeListView

`VCWpfControls.TreeList.TreeListView` 将层级数据渲染为表格：根节点来自 `ItemsSource`，
子节点通过 `ChildMemberPath`（任意 `IEnumerable` 属性）读取。继承 `GridControl` 的全部能力：
手动/自动列、虚拟化、选择、编辑、行详情、主题、键盘。

## 快速开始

```xml
<Window xmlns:tree="clr-namespace:VCWpfControls.TreeList;assembly=VCWpfControls"
        xmlns:columns="clr-namespace:VCWpfControls.Grid.Columns;assembly=VCWpfControls">
    <tree:TreeListView ItemsSource="{Binding Units}" ChildMemberPath="Children">
        <tree:TreeListView.Columns>
            <columns:GridTextColumn FieldName="Name" Header="部门" Width="2*" />
            <columns:GridNumericColumn FieldName="Headcount" Header="人数" Width="110" />
        </tree:TreeListView.Columns>
    </tree:TreeListView>
</Window>
```

## 展开/折叠

- 节点默认折叠；`InitiallyExpanded="True"` 展开第一层。
- API：`ToggleNode(item, expanded)`、`ExpandAll()`、`CollapseAll()`、`IsExpanded(item)`；
  首列中的箭头对单个节点执行相同操作。
- 展开状态在排序和集合变更后保留。

## 排序

点击表头或调用 `SortBy`/`AddSortLevel` 会在**每一级内部**对子节点排序（根节点之间、
兄弟节点之间；显示为 DFS 顺序）。

## 实时更新

根集合或任何已加载的子集合（建议使用 `ObservableCollection`）发生变化时会自动重建视图。

## 暂不支持（见 ROADMAP）

分组面板/筛选行不适用。
