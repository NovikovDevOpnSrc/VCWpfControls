using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Automation;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Data;
using VCWpfControls.Grid.Primitives;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

public class IssueRegressionsTests
{
    private sealed class Instrument
    {
        public string? Code { get; set; }

        public decimal? Strike { get; set; }

        public string Class { get; set; } = "FX";
    }

    [StaFact]
    public void NullText_ShownInsteadOfNull()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn { FieldName = "Code", Header = "Code", NullText = "—" });
        grid.Columns.Add(new GridNumericColumn { FieldName = "Strike", Header = "Strike", NullText = "—" });
        grid.ItemsSource = new[]
        {
            new Instrument { Code = "SI-3.25", Strike = 3.25m },
            new Instrument { Code = null, Strike = null },
        };
        TestWindowHost.Pump();

        try
        {
            var row0 = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
            var row1 = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(1);
            var texts0 = row0.CellsPanel!.Children.OfType<TextBlock>().Select(t => t.Text).ToList();
            var texts1 = row1.CellsPanel!.Children.OfType<TextBlock>().Select(t => t.Text).ToList();

            Assert.Equal("SI-3.25", texts0[0]);
            Assert.Equal("3.25", texts0[1]);
            Assert.Equal("—", texts1[0]);   // string null -> NullText
            Assert.Equal("—", texts1[1]);   // numeric null -> NullText
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void NullText_Default_EmptyCell()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn { FieldName = "Code", Header = "Code" });
        grid.ItemsSource = new[] { new Instrument() };
        TestWindowHost.Pump();

        try
        {
            var row = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
            Assert.Equal(string.Empty, row.CellsPanel!.Children.OfType<TextBlock>().First().Text);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void GroupHeaderStringFormat_KeyCountColumnHeader()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn { FieldName = "Class", Header = "Базовый актив" });
        grid.ItemsSource = new[]
        {
            new Instrument { Code = "a" },
            new Instrument { Code = "b" },
        };
        grid.GroupDescriptions.Add(new GridGroupDescription("Class")
        {
            HeaderStringFormat = "{2}: {0} ({1} инстр.)",
        });
        TestWindowHost.Pump();

        try
        {
            Assert.Single(grid.Items.OfType<GridGroupRow>());
            var row = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
            var groupText = FindTextByName(row, "PART_GroupText");
            Assert.NotNull(groupText);
            Assert.Equal("Базовый актив: FX (2 инстр.)", groupText!.Text);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void GroupHeader_DefaultFormat_StillLocalized()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn { FieldName = "Class", Header = "Class" });
        grid.ItemsSource = new[] { new Instrument { Code = "a" } };
        grid.GroupDescriptions.Add(new GridGroupDescription("Class"));
        TestWindowHost.Pump();

        try
        {
            var row = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
            var groupText = FindTextByName(row, "PART_GroupText");
            Assert.NotNull(groupText);
            Assert.Contains("FX", groupText!.Text);
            Assert.Contains("1", groupText.Text);
        }
        finally
        {
            window.Close();
        }
    }

    // --- Issues round 2 (#6–#8): ElementStyle, UIA names, template content ---

    [StaFact]
    public void Issue8_ElementStyle_AppliedToGeneratedCellElements_AndLiveChange()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        var style = new Style(typeof(TextBlock))
        {
            Setters = { new Setter(TextBlock.ForegroundProperty, Brushes.Red) },
        };
        var column = new GridTextColumn { FieldName = "Code", Header = "Code", ElementStyle = style };
        grid.Columns.Add(column);
        grid.ItemsSource = new[] { new Instrument { Code = "SI-3.25" } };
        TestWindowHost.Pump();

        try
        {
            var row = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
            var cell = row.CellsPanel!.GetCellElement(column);
            Assert.NotNull(cell);
            Assert.Same(style, cell!.Style);

            // Live change re-styles existing (cached) elements; null reverts to the default style.
            var other = new Style(typeof(TextBlock))
            {
                Setters = { new Setter(TextBlock.FontStyleProperty, FontStyles.Italic) },
            };
            column.ElementStyle = other;
            Assert.Same(other, cell.Style);
            column.ElementStyle = null;
            Assert.Null(cell.Style);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Issue7_GroupRow_UiaName_ExposesGroupCaption()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn { FieldName = "Class", Header = "Class" });
        grid.ItemsSource = new[]
        {
            new Instrument { Code = "a" },
            new Instrument { Code = "b" },
        };
        grid.GroupDescriptions.Add(new GridGroupDescription("Class"));
        TestWindowHost.Pump();

        try
        {
            var row = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
            Assert.True(row.IsGroupRow);
            var caption = System.Windows.Automation.AutomationProperties.GetName(row);
            Assert.Contains("FX", caption);
            Assert.Contains("2", caption);
            Assert.Equal(caption, new GridRowAutomationPeer(row).GetName());
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Issue7_ColumnHeader_UiaName_UnwrapsTextBlockContent()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn { FieldName = "Code", Header = "Name" });
        var styled = new GridTextColumn { FieldName = "Strike", Header = new TextBlock { Text = "Last price" } };
        grid.Columns.Add(styled);
        grid.ItemsSource = new[] { new Instrument { Code = "a" } };
        TestWindowHost.Pump();

        try
        {
            var textHeader = grid.HeadersPresenter!.GetHeader(grid.Columns[0]);
            Assert.Equal("Name", new GridColumnHeaderAutomationPeer(textHeader!).GetName());

            var blockHeader = grid.HeadersPresenter.GetHeader(styled);
            Assert.Equal("Last price", new GridColumnHeaderAutomationPeer(blockHeader!).GetName());
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Issue6_TemplateColumn_CellPeer_ExposesTemplateContentToUia()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(Button));
        factory.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, "Favorite");
        template.VisualTree = factory;

        var column = new GridTemplateColumn { FieldName = "Code", Header = "Fav", CellTemplate = template };
        grid.Columns.Add(column);
        grid.ItemsSource = new[] { new Instrument { Code = "SI-3.25" } };
        TestWindowHost.Pump();

        try
        {
            var row = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
            var cellPeer = new GridCellAutomationPeer(new GridRowAutomationPeer(row), grid, column);
            var children = cellPeer.GetChildren();
            Assert.NotNull(children);

            var buttonPeer = children!.OfType<AutomationPeer>().FirstOrDefault(p => p is ButtonAutomationPeer);
            Assert.NotNull(buttonPeer);
            Assert.Equal("Favorite", buttonPeer!.GetName());
            Assert.NotNull(buttonPeer.GetPattern(PatternInterface.Invoke));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void EditingElementStyle_AppliedToGeneratedEditingElements()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        var style = new Style(typeof(TextBox))
        {
            Setters = { new Setter(TextBox.BackgroundProperty, Brushes.Yellow) },
        };
        grid.Columns.Add(new GridTextColumn
        {
            FieldName = "Code",
            Header = "Code",
            ElementStyle = new Style(typeof(TextBlock)),
            EditingElementStyle = style,
        });
        grid.ItemsSource = new[] { new Instrument { Code = "SI" } };
        TestWindowHost.Pump();

        try
        {
            var column = grid.Columns[0];
            Assert.Same(style, column.GenerateEditingElement()!.Style);
            // Display elements get ElementStyle (the empty TextBlock style), not the editing one.
            Assert.Same(column.ElementStyle, column.GenerateElement().Style);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Issue9_FocusVisual_ClearedFromOldRow_WhenCurrentItemMoves()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        var items = new[]
        {
            new Instrument { Code = "a" },
            new Instrument { Code = "b" },
            new Instrument { Code = "c" },
        };
        grid.Columns.Add(new GridTextColumn { FieldName = "Code", Header = "Code" });
        grid.ItemsSource = items;
        TestWindowHost.Pump();

        try
        {
            var rowA = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
            var rowB = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(1);
            var rowC = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(2);

            grid.CurrentItem = items[0];
            TestWindowHost.Pump();
            Assert.Equal(Visibility.Visible, rowA.CellsPanel!.FocusVisual.Visibility);
            Assert.Equal(Visibility.Collapsed, rowB.CellsPanel!.FocusVisual.Visibility);

            // Move across rows: the border must leave the old row (previously it stayed).
            grid.CurrentItem = items[1];
            TestWindowHost.Pump();
            Assert.Equal(Visibility.Collapsed, rowA.CellsPanel!.FocusVisual.Visibility);
            Assert.Equal(Visibility.Visible, rowB.CellsPanel!.FocusVisual.Visibility);

            grid.CurrentItem = items[2];
            TestWindowHost.Pump();
            Assert.Equal(Visibility.Collapsed, rowA.CellsPanel!.FocusVisual.Visibility);
            Assert.Equal(Visibility.Collapsed, rowB.CellsPanel!.FocusVisual.Visibility);
            Assert.Equal(Visibility.Visible, rowC.CellsPanel!.FocusVisual.Visibility);

            // Clearing the current item hides the border everywhere.
            grid.CurrentItem = null;
            TestWindowHost.Pump();
            Assert.Equal(Visibility.Collapsed, rowA.CellsPanel!.FocusVisual.Visibility);
            Assert.Equal(Visibility.Collapsed, rowB.CellsPanel!.FocusVisual.Visibility);
            Assert.Equal(Visibility.Collapsed, rowC.CellsPanel!.FocusVisual.Visibility);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Issue10_ColumnChooser_UnwrapsTextBlockHeader()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn { FieldName = "Code", Header = "Name" });
        var styled = new GridTextColumn { FieldName = "Strike", Header = new TextBlock { Text = "Last price" } };
        grid.Columns.Add(styled);
        grid.ItemsSource = new[] { new Instrument { Code = "a" } };
        TestWindowHost.Pump();

        try
        {
            var menu = grid.BuildColumnHeaderMenu(grid.Columns[0]);
            var chooser = menu.Items.OfType<MenuItem>().First(m => m.Items.Count > 0);
            // Skip the column-chooser dialog entry at the top; column toggles unwrap headers.
            var labels = chooser.Items.OfType<MenuItem>().Skip(1).Select(m => m.Header as string).ToList();
            Assert.Equal(new[] { "Name", "Last price" }, labels);
        }
        finally
        {
            window.Close();
        }
    }

    private static TextBlock? FindTextByName(Visual parent, string name)
    {
        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            if (System.Windows.Media.VisualTreeHelper.GetChild(parent, i) is Visual child)
            {
                if (child is TextBlock text && text.Name == name)
                {
                    return text;
                }

                var nested = FindTextByName(child, name);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }

        return null;
    }
}
