using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
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
